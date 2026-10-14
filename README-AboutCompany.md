# صفحه‌ی «درباره‌ی ما»

## فایل‌های جدید
- Pages/AboutCompany.aspx
- Pages/AboutCompany.aspx.cs
- Pages/AboutCompany.aspx.designer.cs

(مسیر `Pages/` را مطابق همان ساختاری گذاشتم که خودتان الان دارید — `MasterPageFile="~/Pages/Site.Master"` و `Inherits="MeetingMinutes.AboutCompany"`، namespace تخت، مثل بقیه‌ی صفحات فعلی‌تان.)

## نصب
1. سه فایل بالا را داخل `Pages/` بگذارید و Include In Project کنید.
2. محتوای `about-company.css-snippet.txt` را به انتهای `Content/Site.css` خودتان اضافه کنید.
3. در `Site.Master`، بخش `sidebar-footer` را این‌طور عوض کنید تا به صفحه‌ی جدید لینک شود:

```html
<a runat="server" href="~/Pages/AboutCompany.aspx" class="sidebar-footer">
    <img src="<%= ResolveUrl("~/Content/meshkat-logo.png") %>" alt="لوگو شرکت" />
    <p style="display:inline;">
        <b>گروه نرم‌افزاری مشکات</b>
    </p>
</a>
```
فقط تگِ بیرونی از `<div class="sidebar-footer">` به `<a runat="server" href="~/Pages/AboutCompany.aspx" class="sidebar-footer">` عوض شده؛ محتوای داخلش دست‌نخورده است.

## یک نکته‌ی مهم
چون `Site.css` فعلیِ شما را ندارم (می‌بینم خودتان کلاس‌های جدید مثل `.noner`، `.sidebar-footer`، `.sidebar-nav` اضافه کرده‌اید)، نتوانستم مطمئن شوم تبدیل `div.sidebar-footer` به `a.sidebar-footer` دقیقاً همان ظاهر قبلی را حفظ می‌کند. برای همین یک override کوچک (`a.sidebar-footer{display:flex;...}`) هم در همان اسنیپت گذاشتم که چیدمان را برمی‌گرداند؛ اگر فاصله‌ها (gap) یا اندازه‌ی لوگو کمی فرق کرد، همان یک خط را با مقدار دلخواهتان عوض کنید.

## محتوا
همه‌ی متن‌ها نمونه‌ی پیش‌فرض‌اند و داخل کروشه یا با فونت ایتالیک مشخص شده‌اند تا راحت پیدایشان کنید؛ مستقیم در `AboutCompany.aspx` جایگزینشان کنید:
- نام/توضیح شرکت، آدرس، تلفن، ایمیل، وب‌سایت
- نام برنامه‌نویس، توضیح کوتاه، ایمیل، آیدی تلگرام، تلفن

## دسترسی
مثل بقیه‌ی سایت، فقط نیازمند ورود است؛ محدودیت نقش خاصی ندارد (همه‌ی نقش‌ها می‌بینند).
