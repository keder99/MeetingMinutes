using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Web;

namespace MeetingMinutes.Infrastructure
{
    public class ProfileInput
    {
        public string Prefix;
        public string FirstName;
        public string LastName;
        public string Position;
        public string Email;
        public string Mobile;
    }

    /// <summary>
    /// ویرایش خودِ کاربر روی اطلاعات فردی‌اش. برخلاف PeopleService (که برای دبیر/مدیر است)،
    /// اینجا فقط فیلدهای توصیفیِ بی‌خطر قابل تغییرند؛ واحد سازمانی، عضویت در هیأت و وضعیت فعال/غیرفعال
    /// دست‌نخورده می‌مانند و فقط از صفحه‌ی «افراد» توسط دبیر/مدیر قابل تغییرند.
    /// </summary>
    public static class ProfileService
    {
        private static readonly Regex EmailRx = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex MobileRx = new Regex(@"^\+?[0-9]{7,15}$", RegexOptions.Compiled);

        public class ProfileData
        {
            public string Username;
            public string RoleTitle;
            public int? PersonId;
            public string Prefix, FirstName, LastName, Position, Email, Mobile ,UnitTitle;
            public int PhotoVersion;
        }

        public static ProfileData Get(int userId)
        {
            var dt = Db.Query(
                @"SELECT u.Username, r.Title AS RoleTitle, u.PersonId,
                         p.Prefix, p.FirstName, p.LastName, p.Position, p.Email, p.Mobile, p.PhotoVersion, o.Title AS UnitTitle
                  FROM dbo.Users u
                  JOIN dbo.Roles r ON r.RoleId = u.RoleId
                  LEFT JOIN dbo.People p ON p.PersonId = u.PersonId
                  LEFT JOIN dbo.OrgUnits o ON o.OrgUnitId = p.OrgUnitId
                  WHERE u.UserId = @id", Db.P("@id", userId));
            if (dt.Rows.Count == 0) return null;
            var r0 = dt.Rows[0];
            return new ProfileData
            {
                Username = (string)r0["Username"],
                RoleTitle = (string)r0["RoleTitle"],
                PersonId = r0["PersonId"] is DBNull ? (int?)null : (int)r0["PersonId"],
                Prefix = Str(r0["Prefix"]), FirstName = Str(r0["FirstName"]), LastName = Str(r0["LastName"]),
                Position = Str(r0["Position"]), Email = Str(r0["Email"]), Mobile = Str(r0["Mobile"]),
                PhotoVersion= (r0["PhotoVersion"]) is DBNull ? 0 : (int)r0["PhotoVersion"],
                UnitTitle = Str(r0["UnitTitle"])
            };
        }

        private static string Str(object v)
        {
            return v is DBNull || v == null ? string.Empty : (string)v;
        }

        public static string AvatarPath(int personId)
        {
            return Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/Avatars"), personId + ".jpg");
        }

        /// <summary>برای هدر سایت: شناسه‌ی فرد و نسخه‌ی عکس.</summary>
        public static void GetAvatar(int userId, out int? personId, out int version)
        {
            personId = null; version = 0;
            var dt = Db.Query(
                @"SELECT u.PersonId, p.PhotoVersion FROM dbo.Users u
          LEFT JOIN dbo.People p ON p.PersonId = u.PersonId WHERE u.UserId = @id", Db.P("@id", userId));
            if (dt.Rows.Count == 0 || dt.Rows[0]["PersonId"] is DBNull) return;
            personId = (int)dt.Rows[0]["PersonId"];
            version = dt.Rows[0]["PhotoVersion"] is DBNull ? 0 : (int)dt.Rows[0]["PhotoVersion"];
        }

        public static bool SavePhoto(int userId, HttpPostedFile file, out string error)
        {
            error = null;
            var pid = Db.Scalar("SELECT PersonId FROM dbo.Users WHERE UserId = @u", Db.P("@u", userId));
            if (pid == null || pid is DBNull) { error = "ابتدا اطلاعات فردی را ذخیره کنید."; return false; }
            if (file.ContentLength > 2 * 1024 * 1024) { error = "حجم عکس حداکثر ۲ مگابایت است."; return false; }

            try
            {
                byte[] head = new byte[8];
                file.InputStream.Read(head, 0, 8);
                file.InputStream.Position = 0;
                bool isJpeg = head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF;
                bool isPng = head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47;
                if (!isJpeg && !isPng) { error = "فقط فایل JPG یا PNG مجاز است."; return false; }
                using (var src = Image.FromStream(file.InputStream))
                {
                    if (!src.RawFormat.Equals(ImageFormat.Jpeg) && !src.RawFormat.Equals(ImageFormat.Png))
                    { error = "فقط فایل JPG یا PNG مجاز است."; return false; }

                    const int S = 256;
                    int side = Math.Min(src.Width, src.Height);
                    var srcRect = new Rectangle((src.Width - side) / 2, (src.Height - side) / 2, side, side);

                    using (var bmp = new Bitmap(S, S))
                    {
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.Clear(Color.White);
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(src, new Rectangle(0, 0, S, S), srcRect, GraphicsUnit.Pixel);
                        }
                        var path = AvatarPath((int)pid);
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                        var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                        using (var ep = new EncoderParameters(1))
                        {
                            ep.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                            bmp.Save(path, codec, ep);
                        }
                    }
                }
            }
            catch (ArgumentException) { error = "فایل انتخابی عکس معتبر نیست."; return false; }
            catch (OutOfMemoryException) { error = "فایل انتخابی عکس معتبر نیست."; return false; }

            Db.Execute("UPDATE dbo.People SET PhotoVersion = PhotoVersion + 1 WHERE PersonId = @id", Db.P("@id", pid));
            Audit.Write(userId, "Update", "Person", (int)pid, "تغییر عکس پروفایل");
            return true;
        }

        public static void RemovePhoto(int userId)
        {
            var pid = Db.Scalar("SELECT PersonId FROM dbo.Users WHERE UserId = @u", Db.P("@u", userId));
            if (pid == null || pid is DBNull) return;
            var path = AvatarPath((int)pid);
            if (File.Exists(path)) File.Delete(path);
            Db.Execute("UPDATE dbo.People SET PhotoVersion = 0 WHERE PersonId = @id", Db.P("@id", pid));
            Audit.Write(userId, "Update", "Person", (int)pid, "حذف عکس پروفایل");   // این خط اضافه شود
        }

        /// <summary>نام نمایشیِ به‌روز (برای صدور دوباره‌ی کوکی ورود بعد از ذخیره‌ی پروفایل).</summary>
        /// <summary>
        /// آیا حساب کاربری هنوز نیاز به تکمیل دارد؟ سه حالت: هنوز به هیچ فردی وصل نیست، یا ایمیل خالی
        /// است، یا تلفن همراه خالی است. این سه فیلد تا وقتی پر نشوند، هر بار دوباره هشدار داده می‌شود.
        /// </summary>
        public static bool IsIncomplete(int userId)
        {
            var dt = Db.Query(
                @"SELECT u.PersonId, p.Email, p.Mobile FROM dbo.Users u
                  LEFT JOIN dbo.People p ON p.PersonId = u.PersonId WHERE u.UserId = @id", Db.P("@id", userId));
            if (dt.Rows.Count == 0) return false;
            var r = dt.Rows[0];
            if (r["PersonId"] is DBNull) return true;
            if (r["Mobile"] is DBNull || string.IsNullOrWhiteSpace((string)r["Mobile"])) return true;
            return false;
        }

        public static string DisplayName(int userId)
        {
            var v = Db.Scalar(
                @"SELECT COALESCE(p.FullName, u.Username) FROM dbo.Users u
                  LEFT JOIN dbo.People p ON p.PersonId = u.PersonId WHERE u.UserId = @id", Db.P("@id", userId));
            return v == null || v is DBNull ? string.Empty : (string)v;
        }

        public static bool Save(int userId, ProfileInput input, out string error)
        {
            error = null;
            var last = TextNormalizer.Clean(input.LastName);
            var first = TextNormalizer.CleanOrNull(input.FirstName);
            var prefix = TextNormalizer.CleanOrNull(input.Prefix);
            var position = TextNormalizer.CleanOrNull(input.Position);
            var email = TextNormalizer.CleanOrNull(input.Email);
            var mobile = TextNormalizer.CleanOrNull(input.Mobile);

            if (last.Length == 0) { error = "نام خانوادگی را وارد کنید."; return false; }
            if (last.Length > 80) { error = "نام خانوادگی حداکثر ۸۰ نویسه است."; return false; }
            if (first != null && first.Length > 80) { error = "نام حداکثر ۸۰ نویسه است."; return false; }
            if (position != null && position.Length > 150) { error = "سمت حداکثر ۱۵۰ نویسه است."; return false; }
            if (email != null && !EmailRx.IsMatch(email)) { error = "نشانی ایمیل معتبر نیست."; return false; }
            if (mobile != null)
            {
                mobile = Regex.Replace(TextNormalizer.Normalize(mobile), @"[\s\-]", string.Empty);
                if (!MobileRx.IsMatch(mobile)) { error = "شماره‌ی تلفن همراه معتبر نیست."; return false; }
            }

            var fullName = string.Join(" ", new[] { prefix, first, last }.Where(s => !string.IsNullOrEmpty(s)));
            var search = TextNormalizer.Normalize((first ?? string.Empty) + " " + last);

            var personId = Db.Scalar("SELECT PersonId FROM dbo.Users WHERE UserId = @u", Db.P("@u", userId));
            bool hasPerson = personId != null && !(personId is DBNull);

            // اگر نام با فرد دیگری (یا نام مستعار او) یکسان شد، از ساخت رکورد تکراری جلوگیری می‌شود
            var owner = Db.Query(
                @"SELECT TOP 1 FullName FROM dbo.People WHERE SearchName = @s AND (@ex IS NULL OR PersonId <> @ex)
                  UNION ALL
                  SELECT TOP 1 p.FullName FROM dbo.PersonAliases a JOIN dbo.People p ON p.PersonId = a.PersonId
                  WHERE a.AliasName = @s AND (@ex IS NULL OR p.PersonId <> @ex)",
                Db.P("@s", search), Db.P("@ex", hasPerson ? personId : null));
            if (owner.Rows.Count > 0)
            {
                error = "این نام قبلاً برای «" + owner.Rows[0][0] + "» در فهرست افراد ثبت شده است. اگر همان شما هستید، از مدیر یا دبیر بخواهید حساب کاربری‌تان را به آن فرد وصل کند.";
                return false;
            }

            if (hasPerson)
            {
                Db.Execute(
                    @"UPDATE dbo.People
                      SET Prefix = @pre, FirstName = @fn, LastName = @ln, FullName = @full, SearchName = @s,
                          Position = @pos, Email = @em, Mobile = @mob
                      WHERE PersonId = @id",
                    Db.P("@pre", prefix), Db.P("@fn", first), Db.P("@ln", last), Db.P("@full", fullName), Db.P("@s", search),
                    Db.P("@pos", position), Db.P("@em", email), Db.P("@mob", mobile), Db.P("@id", personId));
            }
            else
            {
                var newId = (int)Db.Scalar(
                    @"INSERT dbo.People (Prefix, FirstName, LastName, FullName, SearchName, Position, IsBoardMember, IsActive, Email, Mobile)
                      OUTPUT INSERTED.PersonId
                      VALUES (@pre, @fn, @ln, @full, @s, @pos, 0, 1, @em, @mob)",
                    Db.P("@pre", prefix), Db.P("@fn", first), Db.P("@ln", last), Db.P("@full", fullName), Db.P("@s", search),
                    Db.P("@pos", position), Db.P("@em", email), Db.P("@mob", mobile));
                Db.Execute("UPDATE dbo.Users SET PersonId = @p WHERE UserId = @u", Db.P("@p", newId), Db.P("@u", userId));
            }

            Audit.Write(userId, "Update", "Person", hasPerson ? (int)personId : (int?)null, "به‌روزرسانی پروفایل توسط خود کاربر");
            return true;
        }
    }
}
