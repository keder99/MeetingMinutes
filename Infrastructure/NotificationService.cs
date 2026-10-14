using System;
using System.Data;

namespace MeetingMinutes.Infrastructure
{
    /// <summary>
    /// اعلان‌های نوار بالا. دو دسته‌اند:
    /// ۱) اعلان‌های دستیِ مدیر (این کلاس) — قابل زمان‌بندی و حذف. با باز کردن زنگوله در نوار بالا،
    ///    همه‌ی اعلان‌های فعال برای همان کاربر «خوانده‌شده» ثبت می‌شوند (فقط از شمارشِ نخوانده کنار
    ///    می‌روند)، ولی خودِ پیام تا وقتی مدیر حذفش نکند، در فهرست باقی می‌ماند.
    ///    (جدول NotificationDismissals همان جدولِ «خوانده‌شده‌ها» است؛ نامش از قبل همین مانده.)
    /// ۲) هشدار «تکمیل پروفایل» که خودکار و بر اساس خالی‌بودن فرد/ایمیل/تلفنِ کاربر ساخته می‌شود
    ///    (ProfileService.IsIncomplete) و در دیتابیس ذخیره نمی‌شود؛ چون باید تا رفع نقص، هر بار دوباره
    ///    نشان داده شود، نه در این کلاس بلکه مستقیم در Site.Master.cs مدیریت می‌شود.
    /// </summary>
    public static class NotificationService
    {
        /// <summary>همه‌ی اعلان‌های فعال (خوانده‌شده و نخوانده)، برای نمایش کامل در پنل اعلان‌ها.</summary>
        public static DataTable ActiveForUser(int userId)
        {
            return Db.Query(
                @"SELECT n.NotificationId, n.Title, n.Body, n.CreatedAt,
                         CAST(CASE WHEN d.NotificationId IS NULL THEN 0 ELSE 1 END AS bit) AS IsRead
                  FROM dbo.Notifications n
                  LEFT JOIN dbo.NotificationDismissals d ON d.NotificationId = n.NotificationId AND d.UserId = @u
                  WHERE (n.StartsAt IS NULL OR n.StartsAt <= SYSDATETIME())
                    AND (n.EndsAt IS NULL OR n.EndsAt >= SYSDATETIME())
                  ORDER BY n.CreatedAt DESC", Db.P("@u", userId));
        }

        /// <summary>فقط برای شمارنده‌ی روی زنگوله؛ تعداد اعلان‌های فعالی که این کاربر هنوز ندیده است.</summary>
        public static int CountUnread(int userId)
        {
            return (int)Db.Scalar(
                @"SELECT COUNT(*) FROM dbo.Notifications n
                  WHERE (n.StartsAt IS NULL OR n.StartsAt <= SYSDATETIME())
                    AND (n.EndsAt IS NULL OR n.EndsAt >= SYSDATETIME())
                    AND NOT EXISTS (SELECT 1 FROM dbo.NotificationDismissals d
                                    WHERE d.NotificationId = n.NotificationId AND d.UserId = @u)",
                Db.P("@u", userId));
        }

        /// <summary>با باز شدن زنگوله (کلیک کاربر) صدا زده می‌شود: همه‌ی اعلان‌های فعالِ نخوانده‌ی
        /// همین کاربر یک‌جا خوانده‌شده ثبت می‌شوند. خودِ پیام‌ها حذف نمی‌شوند.</summary>
        public static void MarkAllRead(int userId)
        {
            Db.Execute(
                @"INSERT dbo.NotificationDismissals (NotificationId, UserId)
                  SELECT n.NotificationId, @u FROM dbo.Notifications n
                  WHERE (n.StartsAt IS NULL OR n.StartsAt <= SYSDATETIME())
                    AND (n.EndsAt IS NULL OR n.EndsAt >= SYSDATETIME())
                    AND NOT EXISTS (SELECT 1 FROM dbo.NotificationDismissals d
                                    WHERE d.NotificationId = n.NotificationId AND d.UserId = @u)",
                Db.P("@u", userId));
        }

        /// <summary>فهرست کامل برای صفحه‌ی مدیریت (شامل زمان‌بندی‌شده‌ها و منقضی‌شده‌ها).</summary>
        public static DataTable AdminList()
        {
            return Db.Query(
                @"SELECT n.NotificationId, n.Title, n.Body, n.StartsAt, n.EndsAt, n.CreatedAt,
                         COALESCE(p.FullName, u.Username) AS CreatedByName,
                         (SELECT COUNT(*) FROM dbo.NotificationDismissals d WHERE d.NotificationId = n.NotificationId) AS DismissCount,
                         CASE WHEN n.StartsAt IS NOT NULL AND n.StartsAt > SYSDATETIME() THEN 0
                              WHEN n.EndsAt IS NOT NULL AND n.EndsAt < SYSDATETIME() THEN 2
                              ELSE 1 END AS StateCode   -- 0=زمان‌بندی‌شده، 1=فعال، 2=منقضی‌شده
                  FROM dbo.Notifications n
                  LEFT JOIN dbo.Users u ON u.UserId = n.CreatedBy
                  LEFT JOIN dbo.People p ON p.PersonId = u.PersonId
                  ORDER BY n.CreatedAt DESC");
        }

        public static DataRow Get(int id)
        {
            var dt = Db.Query(
                "SELECT NotificationId, Title, Body, StartsAt, EndsAt FROM dbo.Notifications WHERE NotificationId = @id",
                Db.P("@id", id));
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        public static bool Save(int? id, string title, string body, DateTime? startsAt, DateTime? endsAt, int userId, out int savedId, out string error)
        {
            error = null;
            savedId = id ?? 0;
            title = TextNormalizer.CleanOrNull(title);
            body = TextNormalizer.Clean(body);

            if (body.Length == 0) { error = "متن پیام را وارد کنید."; return false; }
            if (body.Length > 1000) { error = "متن پیام حداکثر ۱۰۰۰ نویسه است."; return false; }
            if (title != null && title.Length > 200) { error = "عنوان حداکثر ۲۰۰ نویسه است."; return false; }
            if (startsAt.HasValue && endsAt.HasValue && startsAt.Value > endsAt.Value)
            { error = "تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد."; return false; }

            if (id.HasValue)
            {
                int n = Db.Execute(
                    "UPDATE dbo.Notifications SET Title = @t, Body = @b, StartsAt = @s, EndsAt = @e WHERE NotificationId = @id",
                    Db.P("@t", title), Db.P("@b", body), Db.P("@s", startsAt), Db.P("@e", endsAt), Db.P("@id", id.Value));
                if (n == 0) { error = "اعلان یافت نشد."; return false; }
            }
            else
            {
                savedId = (int)Db.Scalar(
                    @"INSERT dbo.Notifications (Title, Body, StartsAt, EndsAt, CreatedBy)
                      OUTPUT INSERTED.NotificationId VALUES (@t, @b, @s, @e, @u)",
                    Db.P("@t", title), Db.P("@b", body), Db.P("@s", startsAt), Db.P("@e", endsAt), Db.P("@u", userId));
            }

            Audit.Write(userId, id.HasValue ? "Update" : "Create", "Notification", savedId, title ?? Shorten(body));
            return true;
        }

        public static void Delete(int id, int userId)
        {
            Db.Execute("DELETE dbo.Notifications WHERE NotificationId = @id", Db.P("@id", id));
            Audit.Write(userId, "Delete", "Notification", id, "حذف اعلان");
        }

        private static string Shorten(string s)
        {
            return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
        }
    }
}
