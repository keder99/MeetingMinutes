using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Security;

namespace MeetingMinutes.Infrastructure
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public int UserId { get; set; }
        public string RoleCode { get; set; }
        public string DisplayName { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public static class AuthService
    {
        public const string GenericError = "نام کاربری یا رمز عبور صحیح نیست.";

        // برای برابر کردن زمان پاسخ وقتی نام کاربری وجود ندارد
        private static readonly byte[] DummySalt = new byte[16];
        private static readonly byte[] DummyHash = new byte[32];

        private static int Setting(string key, int fallback)
        {
            int n;
            return int.TryParse(ConfigurationManager.AppSettings[key], out n) ? n : fallback;
        }

        private static LoginResult Fail(string message)
        {
            return new LoginResult { Success = false, Error = message };
        }

        public static bool HasAnyUser()
        {
            return (int)Db.Scalar("SELECT COUNT(*) FROM dbo.Users") > 0;
        }

        public static LoginResult Login(string username, string password, string ip)
        {
            username = (username ?? string.Empty).Trim();
            if (username.Length == 0 || string.IsNullOrEmpty(password)) return Fail(GenericError);

            var dt = Db.Query(
                @"SELECT u.UserId, u.PasswordHash, u.PasswordSalt, u.HashIterations, u.IsActive, u.MustChangePassword,
                         CAST(CASE WHEN u.LockoutUntil > SYSDATETIME() THEN 1 ELSE 0 END AS bit) AS IsLocked,
                         r.Code AS RoleCode, COALESCE(p.FullName, u.Username) AS DisplayName
                  FROM dbo.Users u
                  JOIN dbo.Roles r ON r.RoleId = u.RoleId
                  LEFT JOIN dbo.People p ON p.PersonId = u.PersonId
                  WHERE u.Username = @u", Db.P("@u", username));

            if (dt.Rows.Count == 0)
            {
                PasswordHasher.Verify(password, DummyHash, DummySalt, PasswordHasher.DefaultIterations);
                Audit.Write(null, "LoginFailed", "User", null, "نام کاربری ناشناخته: " + username + " از " + ip);
                return Fail(GenericError);
            }

            var row = dt.Rows[0];
            int userId = (int)row["UserId"];

            if (!(bool)row["IsActive"])
            {
                Audit.Write(userId, "LoginFailed", "User", userId, "حساب غیرفعال از " + ip);
                return Fail(GenericError);
            }
            if ((bool)row["IsLocked"])
            {
                Audit.Write(userId, "LoginFailed", "User", userId, "حساب قفل است، از " + ip);
                return Fail("به‌دلیل تلاش‌های ناموفق متعدد، حساب موقتاً قفل شده است. چند دقیقه بعد دوباره تلاش کنید.");
            }

            bool ok = PasswordHasher.Verify(password, (byte[])row["PasswordHash"],
                                            (byte[])row["PasswordSalt"], (int)row["HashIterations"]);
            if (!ok)
            {
                Db.Execute(
                    @"UPDATE dbo.Users
                      SET FailedLoginCount = CASE WHEN FailedLoginCount + 1 >= @max THEN 0 ELSE FailedLoginCount + 1 END,
                          LockoutUntil     = CASE WHEN FailedLoginCount + 1 >= @max
                                                  THEN DATEADD(MINUTE, @mins, SYSDATETIME()) ELSE LockoutUntil END
                      WHERE UserId = @id",
                    Db.P("@max", Setting("LockoutMaxAttempts", 5)),
                    Db.P("@mins", Setting("LockoutMinutes", 15)),
                    Db.P("@id", userId));
                Audit.Write(userId, "LoginFailed", "User", userId, "رمز اشتباه از " + ip);
                return Fail(GenericError);
            }

            Db.Execute("UPDATE dbo.Users SET FailedLoginCount = 0, LockoutUntil = NULL, LastLoginAt = SYSDATETIME() WHERE UserId = @id",
                       Db.P("@id", userId));
            Audit.Write(userId, "Login", "User", userId, "ورود موفق از " + ip);

            return new LoginResult
            {
                Success = true,
                UserId = userId,
                RoleCode = (string)row["RoleCode"],
                DisplayName = (string)row["DisplayName"],
                MustChangePassword = (bool)row["MustChangePassword"]
            };
        }

        /// <summary>صدور کوکی Forms Authentication؛ شناسه، نقش و نام نمایشی در UserData ذخیره می‌شود.</summary>
        public static void SignIn(HttpContext ctx, LoginResult r, string username)
        {
            var userData = string.Join("|", r.UserId, r.RoleCode, r.DisplayName);
            var now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(1, username, now, now.Add(FormsAuthentication.Timeout),
                                                       false, userData, FormsAuthentication.FormsCookiePath);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath,
                SameSite = SameSiteMode.Lax
            };
            ctx.Response.Cookies.Add(cookie);
        }

        /// <summary>فقط وقتی هیچ کاربری وجود ندارد مدیر اولیه را می‌سازد؛ در غیر این صورت false.</summary>
        public static bool CreateFirstAdmin(string username, string fullName, string password)
        {
            byte[] hash, salt;
            int iterations;
            PasswordHasher.Create(password, out hash, out salt, out iterations);

            int userId;
            using (var con = Db.Open())
            using (var tx = con.BeginTransaction(IsolationLevel.Serializable))
            {
                using (var chk = new SqlCommand("SELECT COUNT(*) FROM dbo.Users", con, tx))
                {
                    if ((int)chk.ExecuteScalar() > 0) return false;
                }

                object personId = DBNull.Value;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    using (var cmd = new SqlCommand(
                        @"INSERT dbo.People (LastName, FullName, SearchName, IsBoardMember)
                          OUTPUT INSERTED.PersonId VALUES (@n, @n, @s, 0)", con, tx))
                    {
                        cmd.Parameters.Add(Db.P("@n", fullName));
                        cmd.Parameters.Add(Db.P("@s", TextNormalizer.Normalize(fullName)));
                        personId = cmd.ExecuteScalar();
                    }
                }

                using (var cmd = new SqlCommand(
                    @"INSERT dbo.Users (Username, PasswordHash, PasswordSalt, HashIterations, RoleId, PersonId, MustChangePassword)
                      OUTPUT INSERTED.UserId VALUES (@u, @h, @s, @i, 1, @p, 0)", con, tx))
                {
                    cmd.Parameters.Add(Db.P("@u", username));
                    cmd.Parameters.Add(Db.P("@h", hash));
                    cmd.Parameters.Add(Db.P("@s", salt));
                    cmd.Parameters.Add(Db.P("@i", iterations));
                    cmd.Parameters.Add(Db.P("@p", personId));
                    userId = (int)cmd.ExecuteScalar();
                }
                tx.Commit();
            }

            Audit.Write(userId, "Create", "User", userId, "ایجاد مدیر اولیه از صفحه‌ی Setup");
            return true;
        }
    }
}
