using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MeetingMinutes.Infrastructure
{
    /// <summary>
    /// تبدیل تاریخ میلادی/شمسی. در دیتابیس میلادی نگه می‌داریم و شمسی فقط برای نمایش و ورودی است.
    /// الگوریتم عیناً همان الگوریتم Scripts/jalali.js است تا تقویم مرورگر و سرور همیشه هم‌نتیجه باشند
    /// (به PersianCalendar خودِ .NET وابسته نیستیم چون رفتارش بین نسخه‌ها فرق دارد).
    /// </summary>
    public static class PersianDate
    {
        public static void ToJalaliParts(DateTime d, out int jy, out int jm, out int jd)
        {
            GregorianToJalali(d.Year, d.Month, d.Day, out jy, out jm, out jd);
        }

        /// <summary>yyyy/MM/dd با ارقام لاتین (فرمت ذخیره در MeetingDateFa).</summary>
        public static string ToJalali(DateTime d)
        {
            int jy, jm, jd;
            GregorianToJalali(d.Year, d.Month, d.Day, out jy, out jm, out jd);
            return jy.ToString("0000") + "/" + jm.ToString("00") + "/" + jd.ToString("00");
        }

        /// <summary>برای نمایش در صفحه، با ارقام فارسی.</summary>
        public static string ToDisplay(DateTime d, bool withTime)
        {
            var s = ToJalali(d);
            if (withTime) s += " " + d.ToString("HH:mm", CultureInfo.InvariantCulture);
            return TextNormalizer.ToPersianDigits(s);
        }

        /// <summary>ورودی مثل 1404/07/05 یا ۱۴۰۴-۷-۵ را به تاریخ میلادی تبدیل می‌کند.</summary>
        public static bool TryParse(string text, out DateTime date)
        {
            date = default(DateTime);
            var m = Regex.Match(TextNormalizer.Normalize(text), @"^(\d{4})[/\-](\d{1,2})[/\-](\d{1,2})$");
            if (!m.Success) return false;

            int jy = int.Parse(m.Groups[1].Value), jm = int.Parse(m.Groups[2].Value), jd = int.Parse(m.Groups[3].Value);
            if (jy < 1300 || jy > 1600 || jm < 1 || jm > 12 || jd < 1 || jd > DaysInMonth(jy, jm)) return false;

            int gy, gm, gd;
            JalaliToGregorian(jy, jm, jd, out gy, out gm, out gd);
            date = new DateTime(gy, gm, gd);
            return true;
        }

        public static int DaysInMonth(int jy, int jm)
        {
            int gy1, gm1, gd1, gy2, gm2, gd2;
            JalaliToGregorian(jy, jm, 1, out gy1, out gm1, out gd1);
            int ny = jy, nm = jm + 1;
            if (nm > 12) { nm = 1; ny = jy + 1; }
            JalaliToGregorian(ny, nm, 1, out gy2, out gm2, out gd2);
            return (int)(new DateTime(gy2, gm2, gd2) - new DateTime(gy1, gm1, gd1)).TotalDays;
        }

        private static void JalaliToGregorian(int jy, int jm, int jd, out int gy, out int gm, out int gd)
        {
            gy = (jy <= 979) ? 621 : 1600;
            jy -= (jy <= 979) ? 0 : 979;
            int days = (365 * jy) + ((jy / 33) * 8) + (((jy % 33) + 3) / 4) + 78 + jd
                       + ((jm < 7) ? (jm - 1) * 31 : ((jm - 7) * 30) + 186);
            gy += 400 * (days / 146097); days %= 146097;
            if (days > 36524)
            {
                gy += 100 * (--days / 36524); days %= 36524;
                if (days >= 365) days++;
            }
            gy += 4 * (days / 1461); days %= 1461;
            if (days > 365) { gy += (days - 1) / 365; days = (days - 1) % 365; }
            gd = days + 1;
            int[] sal = { 0, 31, ((gy % 4 == 0 && gy % 100 != 0) || (gy % 400 == 0)) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            for (gm = 1; gm <= 12; gm++)
            {
                if (gd <= sal[gm]) break;
                gd -= sal[gm];
            }
        }

        private static void GregorianToJalali(int gy, int gm, int gd, out int jy, out int jm, out int jd)
        {
            int[] gdm = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
            jy = (gy <= 1600) ? 0 : 979;
            gy -= (gy <= 1600) ? 621 : 1600;
            int gy2 = (gm > 2) ? (gy + 1) : gy;
            int days = (365 * gy) + ((gy2 + 3) / 4) - ((gy2 + 99) / 100) + ((gy2 + 399) / 400) - 80 + gd + gdm[gm - 1];
            jy += 33 * (days / 12053); days %= 12053;
            jy += 4 * (days / 1461); days %= 1461;
            if (days > 365) { jy += (days - 1) / 365; days = (days - 1) % 365; }
            jm = (days < 186) ? 1 + (days / 31) : 7 + ((days - 186) / 30);
            jd = 1 + ((days < 186) ? (days % 31) : ((days - 186) % 30));
        }
    }
}
