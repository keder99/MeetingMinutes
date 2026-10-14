using System;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    // صفحه‌ی ایستا (معرفی شرکت و توسعه‌دهنده)؛ هیچ داده‌ای از دیتابیس نمی‌خواند.
    // فقط نیازمند ورود است (مثل بقیه‌ی سایت)، بدون محدودیت نقش خاص.
    public partial class AboutCompany : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
