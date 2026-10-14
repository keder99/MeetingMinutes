using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace MeetingMinutes.Infrastructure
{
    public static class UserService
    {
        private static readonly Regex UsernameRx = new Regex(@"^[A-Za-z0-9_.@-]{3,50}$", RegexOptions.Compiled);

        private static int Count(string sql, params SqlParameter[] ps)
        {
            return (int)Db.Scalar(sql, ps);
        }
        public static DataTable List()
        {
            return Db.Query(
                @"SELECT u.UserId, u.Username, u.IsActive, u.LastLoginAt, u.MustChangePassword,
                 r.Title AS RoleTitle, p.PersonId, p.PhotoVersion, p.FullName, p.Email, p.Mobile,
                 CAST(CASE WHEN u.LockoutUntil > SYSDATETIME() THEN 1 ELSE 0 END AS bit) AS IsLocked
          FROM dbo.Users u
          JOIN dbo.Roles r ON r.RoleId = u.RoleId
          LEFT JOIN dbo.People p ON p.PersonId = u.PersonId
          ORDER BY u.IsActive DESC, u.Username");
        }

        public static DataRow Get(int userId)
        {
            var dt = Db.Query(
                @"SELECT UserId, Username, RoleId, PersonId, IsActive, MustChangePassword
                  FROM dbo.Users WHERE UserId = @id", Db.P("@id", userId));
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        public static DataTable Roles()
        {
            return Db.Query("SELECT RoleId, Title FROM dbo.Roles ORDER BY RoleId");
        }

        /// <summary>افراد فعالی که هنوز حساب کاربری ندارند (به‌علاوه‌ی فرد جاری هنگام ویرایش).</summary>
        public static DataTable AvailablePeople(int? includePersonId)
        {
            return Db.Query(
                @"SELECT p.PersonId, p.FullName FROM dbo.People p
                  WHERE (p.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.PersonId = p.PersonId))
                     OR p.PersonId = @inc
                  ORDER BY p.LastName, p.FirstName", Db.P("@inc", includePersonId));
        }

        public static bool Create(string username, int? personId, int roleId, string password, int actingUserId, out string error)
        {
            error = null;
            username = (username ?? string.Empty).Trim();
            if (!UsernameRx.IsMatch(username)) { error = "نام کاربری باید ۳ تا ۵۰ نویسه و فقط شامل حروف انگلیسی، عدد و . _ @ - باشد."; return false; }
            if (!PasswordHasher.TryValidate(username, password, out error)) return false;
            if (Count("SELECT COUNT(*) FROM dbo.Roles WHERE RoleId = @r", Db.P("@r", roleId)) == 0) { error = "نقش انتخاب‌شده نامعتبر است."; return false; }
            if (Count("SELECT COUNT(*) FROM dbo.Users WHERE Username = @u", Db.P("@u", username)) > 0) { error = "این نام کاربری قبلاً ثبت شده است."; return false; }
            if (personId.HasValue && Count("SELECT COUNT(*) FROM dbo.Users WHERE PersonId = @p", Db.P("@p", personId.Value)) > 0)
            { error = "برای این فرد قبلاً حساب کاربری ساخته شده است."; return false; }

            byte[] hash, salt;
            int iterations;
            PasswordHasher.Create(password, out hash, out salt, out iterations);

            var id = (int)Db.Scalar(
                @"INSERT dbo.Users (Username, PasswordHash, PasswordSalt, HashIterations, RoleId, PersonId, MustChangePassword)
                  OUTPUT INSERTED.UserId VALUES (@u, @h, @s, @i, @r, @p, 1)",
                Db.P("@u", username), Db.P("@h", hash), Db.P("@s", salt), Db.P("@i", iterations),
                Db.P("@r", roleId), Db.P("@p", personId));
            Audit.Write(actingUserId, "Create", "User", id, "ایجاد کاربر " + username);
            return true;
        }

        public static bool Update(int userId, int? personId, int roleId, bool isActive, int actingUserId, out string error)
        {
            error = null;
            var cur = Get(userId);
            if (cur == null) { error = "کاربر یافت نشد."; return false; }

            if (userId == actingUserId && (roleId != Convert.ToInt32(cur["RoleId"]) || !isActive))
            { error = "نقش یا وضعیت حساب خودتان را نمی‌توانید تغییر دهید."; return false; }

            if (Count("SELECT COUNT(*) FROM dbo.Roles WHERE RoleId = @r", Db.P("@r", roleId)) == 0) { error = "نقش انتخاب‌شده نامعتبر است."; return false; }
            if (personId.HasValue &&
                Count("SELECT COUNT(*) FROM dbo.Users WHERE PersonId = @p AND UserId <> @id", Db.P("@p", personId.Value), Db.P("@id", userId)) > 0)
            { error = "برای این فرد حساب کاربری دیگری وجود دارد."; return false; }

            Db.Execute("UPDATE dbo.Users SET PersonId = @p, RoleId = @r, IsActive = @a WHERE UserId = @id",
                       Db.P("@p", personId), Db.P("@r", roleId), Db.P("@a", isActive), Db.P("@id", userId));
            Audit.Write(actingUserId, "Update", "User", userId, "ویرایش کاربر " + cur["Username"]);
            return true;
        }

        /// <summary>رمز موقت جدید می‌گذارد؛ کاربر در اولین ورود مجبور به تغییر آن است.</summary>
        public static bool ResetPassword(int userId, string newPassword, int actingUserId, out string error)
        {
            error = null;
            if (userId == actingUserId) { error = "برای تغییر رمز خودتان از «تغییر رمز عبور» (آیکن کلید در نوار بالا) استفاده کنید."; return false; }
            var cur = Get(userId);
            if (cur == null) { error = "کاربر یافت نشد."; return false; }
            var username = (string)cur["Username"];
            if (!PasswordHasher.TryValidate(username, newPassword, out error)) return false;

            byte[] hash, salt;
            int iterations;
            PasswordHasher.Create(newPassword, out hash, out salt, out iterations);
            Db.Execute(
                @"UPDATE dbo.Users
                  SET PasswordHash = @h, PasswordSalt = @s, HashIterations = @i, MustChangePassword = 1,
                      FailedLoginCount = 0, LockoutUntil = NULL
                  WHERE UserId = @id",
                Db.P("@h", hash), Db.P("@s", salt), Db.P("@i", iterations), Db.P("@id", userId));
            Audit.Write(actingUserId, "ResetPassword", "User", userId, "ریست رمز کاربر " + username);
            return true;
        }

        public static void Unlock(int userId, int actingUserId)
        {
            Db.Execute("UPDATE dbo.Users SET FailedLoginCount = 0, LockoutUntil = NULL WHERE UserId = @id", Db.P("@id", userId));
            Audit.Write(actingUserId, "Unlock", "User", userId, "باز کردن قفل حساب");
        }

        // ---------- حذف ----------
        /// <summary>حذف واقعیِ یک کاربر فقط وقتی مجاز است که هیچ‌جای دیگر دیتابیس به او ارجاع نداده باشد
        /// (جز خواندنِ اعلان‌ها، که چون فقط یک رکوردِ کم‌اهمیتِ «دیده‌شد» است، خودکار پاک می‌شود).
        /// برای حسابی که سابقه دارد، به‌جای حذف باید غیرفعالش کرد.</summary>
        public static bool CanDelete(int userId, int actingUserId, out string reason)
        {
            reason = null;
            if (userId == actingUserId) { reason = "نمی‌توانید حساب کاربریِ خودتان را حذف کنید."; return false; }

            var cur = Get(userId);
            if (cur == null) { reason = "کاربر یافت نشد."; return false; }

            if ((string)Db.Query("SELECT r.Code FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId WHERE u.UserId = @id", Db.P("@id", userId)).Rows[0][0] == RoleCodes.Admin)
            {
                int otherActiveAdmins = Count(
                    @"SELECT COUNT(*) FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
                      WHERE r.Code = @admin AND u.IsActive = 1 AND u.UserId <> @id",
                    Db.P("@admin", RoleCodes.Admin), Db.P("@id", userId));
                if (otherActiveAdmins == 0) { reason = "این تنها مدیر فعال سامانه است و قابل حذف نیست."; return false; }
            }

            var reasons = new List<string>();
            if (Count("SELECT COUNT(*) FROM dbo.Meetings WHERE CreatedBy = @id OR UpdatedBy = @id", Db.P("@id", userId)) > 0)
                reasons.Add("در ثبت یا ویرایش جلسه‌ای نقش داشته");
            if (Count("SELECT COUNT(*) FROM dbo.ActionItems WHERE UpdatedBy = @id", Db.P("@id", userId)) > 0)
                reasons.Add("اقدامی را ویرایش کرده");
            if (Count("SELECT COUNT(*) FROM dbo.ActionFollowUps WHERE CreatedBy = @id", Db.P("@id", userId)) > 0)
                reasons.Add("پیگیریِ اقدامی را ثبت کرده");
            if (Count("SELECT COUNT(*) FROM dbo.Attachments WHERE UploadedBy = @id", Db.P("@id", userId)) > 0)
                reasons.Add("ضمیمه‌ای بارگذاری کرده");
            if (Count("SELECT COUNT(*) FROM dbo.Notifications WHERE CreatedBy = @id", Db.P("@id", userId)) > 0)
                reasons.Add("اعلانی ساخته");

            if (reasons.Count == 0) return true;
            reason = "این کاربر قابل حذف نیست، چون " + string.Join("، ", reasons) + ". به‌جای حذف، غیرفعالش کنید.";
            return false;
        }

        public static bool Delete(int userId, int actingUserId, out string error)
        {
            if (!CanDelete(userId, actingUserId, out error)) return false;
            var username = (string)Db.Query("SELECT Username FROM dbo.Users WHERE UserId = @id", Db.P("@id", userId)).Rows[0][0];

            using (var con = Db.Open())
            using (var tx = con.BeginTransaction())
            {
                using (var c = new SqlCommand("DELETE dbo.NotificationDismissals WHERE UserId = @id", con, tx))
                { c.Parameters.Add(Db.P("@id", userId)); c.ExecuteNonQuery(); }

                using (var c = new SqlCommand("DELETE dbo.Users WHERE UserId = @id", con, tx))
                { c.Parameters.Add(Db.P("@id", userId)); c.ExecuteNonQuery(); }
                tx.Commit();
            }

            Audit.Write(actingUserId, "Delete", "User", userId, "حذف کاربر " + username);
            return true;
        }

        public static bool ChangeOwnPassword(int userId, string current, string newPassword, out string error)
        {
            error = null;
            var dt = Db.Query(
                "SELECT Username, PasswordHash, PasswordSalt, HashIterations FROM dbo.Users WHERE UserId = @id", Db.P("@id", userId));
            if (dt.Rows.Count == 0) { error = "کاربر یافت نشد."; return false; }
            var row = dt.Rows[0];
            var hash = (byte[])row["PasswordHash"];
            var salt = (byte[])row["PasswordSalt"];
            var iter = (int)row["HashIterations"];

            if (!PasswordHasher.Verify(current ?? string.Empty, hash, salt, iter)) { error = "رمز عبور فعلی صحیح نیست."; return false; }
            if (!PasswordHasher.TryValidate((string)row["Username"], newPassword, out error)) return false;
            if (PasswordHasher.Verify(newPassword, hash, salt, iter)) { error = "رمز جدید باید با رمز فعلی متفاوت باشد."; return false; }

            byte[] nh, ns;
            int ni;
            PasswordHasher.Create(newPassword, out nh, out ns, out ni);
            Db.Execute(
                @"UPDATE dbo.Users SET PasswordHash = @h, PasswordSalt = @s, HashIterations = @i, MustChangePassword = 0
                  WHERE UserId = @id",
                Db.P("@h", nh), Db.P("@s", ns), Db.P("@i", ni), Db.P("@id", userId));
            Audit.Write(userId, "ChangePassword", "User", userId, "تغییر رمز عبور توسط خود کاربر");
            return true;
        }
    }
}
