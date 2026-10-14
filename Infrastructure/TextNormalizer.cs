using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MeetingMinutes.Infrastructure
{
    /// <summary>
    /// یکسان‌سازی متن فارسی: حروف عربی (ي ك) به فارسی، ارقام فارسی/عربی به لاتین (برای کلید جستجو).
    /// </summary>
    public static class TextNormalizer
    {
        /// <summary>برای متن قابل‌نمایش (مثل نام و متن مصوبه): فقط ي/ك را اصلاح می‌کند.</summary>
        public static string Clean(string s)
        {
            return Transform(s, false);
        }

        /// <summary>مثل Clean، ولی رشته‌ی خالی را null برمی‌گرداند (برای ستون‌های اختیاری).</summary>
        public static string CleanOrNull(string s)
        {
            var c = Clean(s);
            return c.Length == 0 ? null : c;
        }

        /// <summary>برای ستون‌های جستجو (SearchName و ...): حذف نیم‌فاصله، اعراب، کشیده و تبدیل ارقام.</summary>
        public static string Normalize(string s)
        {
            return Transform(s, true);
        }

        private static string Transform(string s, bool forSearch)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s.Trim())
            {
                if (ch == '\u064A' || ch == '\u0649') { sb.Append('\u06CC'); continue; }   // ي ى -> ی
                if (ch == '\u0643') { sb.Append('\u06A9'); continue; }                    // ك -> ک
                if (!forSearch) { sb.Append(ch); continue; }

                if (ch == '\u200C' || ch == '\u200D' || ch == '\u0640') continue;          // نیم‌فاصله، کشیده
                if (ch >= '\u064B' && ch <= '\u065F') continue;                           // اعراب
                if (ch >= '\u06F0' && ch <= '\u06F9') { sb.Append((char)('0' + (ch - '\u06F0'))); continue; }
                if (ch >= '\u0660' && ch <= '\u0669') { sb.Append((char)('0' + (ch - '\u0660'))); continue; }
                sb.Append(ch);
            }
            var result = sb.ToString();
            return forSearch ? Regex.Replace(result, @"\s+", " ").Trim() : result;
        }

        /// <summary>عدد یا رشته را با ارقام فارسی برمی‌گرداند (فقط برای نمایش).</summary>
        public static string ToPersianDigits(object value)
        {
            if (value == null) return string.Empty;
            var s = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
                sb.Append(ch >= '0' && ch <= '9' ? (char)('\u06F0' + (ch - '0')) : ch);
            return sb.ToString();
        }
    }
}
