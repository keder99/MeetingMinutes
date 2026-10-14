using System;
using System.Web;
using System.Web.Security;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public class Global : HttpApplication
    {
        // از روی کوکی ورود، AppPrincipal (شامل شناسه، نقش و نام نمایشی) می‌سازد تا
        // User.IsInRole("Admin") و ((AppPrincipal)User).UserId در همه‌ی صفحات کار کند.
        // توجه: غیرفعال‌شدن کاربر تا پایان اعتبار کوکی (۶۰ دقیقه) اثر نمی‌کند؛ در صورت نیاز
        // در مراحل بعد یک بررسی سبک IsActive اضافه می‌شود.
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var identity = Context.User != null ? Context.User.Identity as FormsIdentity : null;
            if (identity == null || !identity.IsAuthenticated) return;

            var parts = (identity.Ticket.UserData ?? string.Empty).Split(new[] { '|' }, 3);
            int userId;
            if (parts.Length < 3 || !int.TryParse(parts[0], out userId)) return;

            Context.User = new AppPrincipal(identity, userId, parts[1], parts[2]);
        }
    }
}
