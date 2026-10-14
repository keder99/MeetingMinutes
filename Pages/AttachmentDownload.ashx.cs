using System;
using System.IO;
using System.Text;
using System.Web;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    /// <summary>
    /// دانلود امن فایل ضمیمه. فایل‌های واقعی زیر App_Data هستند و IIS مستقیماً آن‌ها را سرو نمی‌کند؛
    /// فقط از همین مسیر —با همان قواعد دسترسیِ صفحه‌ی جلسه— قابل دریافت‌اند.
    /// این آدرس هم مثل بقیه‌ی سایت نیازمند ورود است (قاعده‌ی &lt;deny users="?"/&gt; در Web.config).
    /// </summary>
    public class AttachmentDownload : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            int id;
            if (!int.TryParse(context.Request.QueryString["id"], out id)) { Fail(context, 400, "درخواست نامعتبر است."); return; }

            var row = AttachmentService.GetForDownload(id);
            if (row == null || (bool)row["MeetingDeleted"]) { Fail(context, 404, "فایل یافت نشد."); return; }

            var user = context.User as AppPrincipal;
            if (user == null) { Fail(context, 401, "برای دریافت این فایل باید وارد شوید."); return; }

            // پیش‌نویس فقط برای دبیر/مدیر، مثل قاعده‌ی صفحه‌ی جزئیات جلسه
            int statusId = Convert.ToInt32(row["MeetingStatusId"]);
            if (statusId == MeetingStatusIds.Draft && !user.CanEditMinutes)
            { Fail(context, 403, "دسترسی به این فایل مجاز نیست."); return; }

            var fullPath = context.Server.MapPath("~/" + (string)row["StoragePath"]);
            if (!File.Exists(fullPath)) { Fail(context, 404, "فایل روی سرور یافت نشد."); return; }

            var fileName = (string)row["FileName"];
            var contentType = row["ContentType"] is DBNull ? "application/octet-stream" : (string)row["ContentType"];

            context.Response.Clear();
            context.Response.ContentType = contentType;
            // هم‌سازگار با مرورگرهای قدیمی (filename ساده‌شده) و هم درست برای نام فارسی (filename*=UTF-8'')
            var asciiName = System.Text.RegularExpressions.Regex.Replace(fileName, "[^\x20-\x7E]", "_");
            context.Response.AddHeader("Content-Disposition",
                "attachment; filename=\"" + asciiName.Replace("\"", "'") + "\"; filename*=UTF-8''" + Uri.EscapeDataString(fileName));
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            context.Response.TransmitFile(fullPath);
        }

        private static void Fail(HttpContext context, int statusCode, string message)
        {
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.Write(message);
        }
    }
}
