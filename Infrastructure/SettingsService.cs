using System;
using System.Collections.Generic;
using System.Data;

namespace MeetingMinutes.Infrastructure
{
    /// <summary>خواندن و ذخیره‌ی تنظیمات عمومی (جدول AppSettings). فقط مدیر به این صفحه دسترسی دارد.</summary>
    public static class SettingsService
    {
        public class Settings
        {
            public string OrgName;
            public string NumberPrefix;
            public bool OverdueReminder;
        }

        private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
        {
            { "OrgName", "" },
            { "NumberPrefix", "{YYYY}/" },
            { "OverdueReminder", "1" }
        };

        public static Settings Get()
        {
            var dt = Db.Query("SELECT SettingKey, SettingValue FROM dbo.AppSettings");
            var map = new Dictionary<string, string>(Defaults);
            foreach (DataRow r in dt.Rows)
            {
                var key = (string)r["SettingKey"];
                if (map.ContainsKey(key) && !(r["SettingValue"] is DBNull))
                    map[key] = (string)r["SettingValue"];
            }
            return new Settings
            {
                OrgName = map["OrgName"],
                NumberPrefix = map["NumberPrefix"],
                OverdueReminder = map["OverdueReminder"] == "1"
            };
        }

        /// <summary>فقط نمایش «N مورد معوق» و برجسته‌سازی ردیف‌های معوق را کنترل می‌کند؛
        /// خودِ محاسبه‌ی معوق‌بودن (بر اساس مهلت گذشته) همیشه انجام می‌شود.</summary>
        public static bool OverdueReminderEnabled()
        {
            var v = Db.Scalar("SELECT SettingValue FROM dbo.AppSettings WHERE SettingKey = 'OverdueReminder'");
            return v == null || v is DBNull || (string)v == "1";
        }

        public static string OrgName()
        {
            var v = Db.Scalar("SELECT SettingValue FROM dbo.AppSettings WHERE SettingKey = 'OrgName'");
            return v == null || v is DBNull ? string.Empty : (string)v;
        }

        public static bool Save(string orgName, string numberPrefix, bool overdueReminder, int userId, out string error)
        {
            error = null;
            orgName = TextNormalizer.CleanOrNull(orgName) ?? string.Empty;
            numberPrefix = TextNormalizer.Clean(numberPrefix);

            if (orgName.Length > 200) { error = "نام سازمان حداکثر ۲۰۰ نویسه است."; return false; }
            if (numberPrefix.Length == 0) { error = "پیشوند شماره‌ی جلسات نمی‌تواند خالی باشد."; return false; }
            if (numberPrefix.Length > 50) { error = "پیشوند شماره‌ی جلسات حداکثر ۵۰ نویسه است."; return false; }
            if (numberPrefix.IndexOf("{YYYY}", StringComparison.Ordinal) < 0)
            { error = "پیشوند باید شامل {YYYY} باشد تا سال شمسی جلسه در شماره جای بگیرد (مثال: {YYYY}/)."; return false; }

            Set("OrgName", orgName);
            Set("NumberPrefix", numberPrefix);
            Set("OverdueReminder", overdueReminder ? "1" : "0");
            Audit.Write(userId, "Update", "Settings", null, "ذخیره‌ی تنظیمات عمومی");
            return true;
        }

        private static void Set(string key, string value)
        {
            int n = Db.Execute("UPDATE dbo.AppSettings SET SettingValue = @v WHERE SettingKey = @k", Db.P("@v", value), Db.P("@k", key));
            if (n == 0)
                Db.Execute("INSERT dbo.AppSettings (SettingKey, SettingValue) VALUES (@k, @v)", Db.P("@k", key), Db.P("@v", value));
        }
    }
}
