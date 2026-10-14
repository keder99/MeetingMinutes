using System.Web;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    /// <summary>
    /// با باز شدن زنگوله‌ی اعلان‌ها در نوار بالا، از جاوااسکریپت (fetch) صدا زده می‌شود تا اعلان‌های
    /// دستیِ مدیر برای همین کاربر «خوانده‌شده» ثبت شوند. خودِ پیام‌ها پاک نمی‌شوند و در فهرست کاربر
    /// باقی می‌مانند؛ فقط از شمارشِ نخوانده کنار می‌روند. مثل بقیه‌ی سایت، ورود اجباری است
    /// (قاعده‌ی &lt;deny users="?"/&gt; در Web.config).
    /// </summary>
    public class MarkNotificationsRead : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            var user = context.User as AppPrincipal;
            if (user != null) NotificationService.MarkAllRead(user.UserId);

            context.Response.ContentType = "text/plain";
            context.Response.Write("ok");
        }
    }
}
