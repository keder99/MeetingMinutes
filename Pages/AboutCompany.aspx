<%@ Page Title="درباره‌ی ما" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="AboutCompany.aspx.cs" Inherits="MeetingMinutes.AboutCompany" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h4>معرفی شرکت سازنده و تیم توسعه‌</h4>
        </div>
    </div>

    <section class="about-card mb">
        <aside class="about-brand">
            <div class="about-logo-wrap">
                <img src="<%= ResolveUrl("~/Content/last.png") %>" alt="لوگوی گروه نرم‌افزاری مشکات" class="about-logo" width="285px" height="auto"/>
                 <h2 class="about-title" style="color:#023154;font-size:15px;">meshkat software development</h2>
            </div>
        </aside>

        <div class="about-content">
            <h3 class="about-heading">درباره‌ی ما</h3>
            <p class="about-text">
                گروه نرم‌افزاری مشکات در ما یک تیم توسعه نرم‌افزار پیشرو هستیم که با تسلط بر تمامی تکنولوژی‌ها، پلتفرم‌ها و زبان‌های برنامه‌نویسی، 
                توانایی پیاده‌سازی هرگونه راهکار دیجیتال را بدون هیچ‌گونه محدودیتی داریم. تمرکز ما بر ارائه سیستم‌های مقیاس‌پذیر، 
                امن و دقیق است که پیچیده‌ترین نیازهای نرم‌افزاری سازمان‌ها را با بالاترین استانداردهای مهندسی برآورده می‌سازد.
            </p>

            <ul class="about-contact comp">
                <li class="about-contact-item">
                    <i class="ti ti-brand-telegram"></i>
                    <div><small>ID telegram</small><span dir="ltr">@B_MESHKAT_SD</span></div>
                </li>
                <li class="about-contact-item">
                    <i class="ti ti-phone"></i>
                    <div><small>تلفن دفتر</small><span dir="ltr">0935 845 2811</span></div>
                </li>
                <li class="about-contact-item">
                    <i class="ti ti-mail"></i>
                    <div><small>ایمیل</small><span dir="ltr">sales@meshkatsoft.com</span></div>
                </li>
                <li class="about-contact-item">
                    <i class="ti ti-world"></i>
                    <div><small>وب‌سایت</small><span dir="ltr">meshkatsoft.ir</span></div>
                </li>
            </ul>
        </div>
    </section>

    <div class="panel">
        <div class="panel-head" style="justify-content: right;"><h2>تیم توسعه</h2><em style="margin:0px 10px">#</em><h2 style="font-size:16px">Seyed Mojtaba</h2></div>
        <div class="panel-body">
            <div class="dev-card">
                <div class="grow">
                    <p class="about-text">
                        از طریق راه های زیر با ما در ارتباط باشید.
                    </p>
                    <div class="about-contact">
                        <div class="about-contact-item"><i class="ti ti-mail"></i><span dir="ltr">smojtaba.rs@gmail.com</span></div>
                        <div class="about-contact-item"><i class="ti ti-brand-telegram"></i><span dir="ltr">@b_smojtaba</span></div>
                        <div class="about-contact-item"><i class="ti ti-phone"></i><span dir="ltr">0901 714 8807</span></div>
                        <div class="about-contact-item"><i class="ti "><img style="width: 20px;" src="../Content/eitaa.svg" /></i><span dir="ltr">@b_smojtaba</span></div>

                    </div>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
