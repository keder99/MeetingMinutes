<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="MeetingMinutes.Login" %>
<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width,initial-scale=1" />
    <title>ورود — سامانه مدیریت جلسات</title>
    <link runat="server" rel="stylesheet" href="~/Content/assets/font/Vazirmatn-font-face.css" />
    <link runat="server" rel="stylesheet" href="~/Content/assets/tabler-icons/tabler-icons.min.css" />
    <link runat="server" rel="stylesheet" href="~/Content/Site.css" />
</head>
<body>
<form id="form1" runat="server" class="login-wrap">
    <div class="login-card">
        <div class="login-brand"><i class="ti ti-clipboard-text"></i><span>صورت‌جلسه</span></div>
        <h1>ورود به سامانه</h1>
        <p class="sub">سامانه مدیریت جلسات و صورت‌جلسه</p>

        <asp:Literal ID="litMessage" runat="server" />

        <asp:Panel ID="pnlSetup" runat="server" Visible="false">
            <div class="alert alert-info">
                هنوز هیچ کاربری ساخته نشده است.
                <a href="Setup.aspx">ساخت مدیر اولیه</a>
            </div>
        </asp:Panel>

        <div class="field">
            <label for="txtUsername">نام کاربری</label>
            <asp:TextBox ID="txtUsername" runat="server" CssClass="ltr" MaxLength="50" autocomplete="username" />
        </div>
        <div class="field">
            <label for="txtPassword">رمز عبور</label>
            <div class="password-field">
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="ltr" MaxLength="100" autocomplete="current-password" />
                <button type="button" class="pw-toggle" tabindex="-1" aria-label="نمایش رمز عبور"><i class="ti ti-eye"></i></button>
            </div>
        </div>
        <asp:Button ID="btnLogin" runat="server" Text="ورود" CssClass="btn btn-primary btn-block" OnClick="btnLogin_Click" />
    </div>
    <script>
        document.addEventListener("DOMContentLoaded", function () {

            const username = document.getElementById("<%= txtUsername.ClientID %>");
        const password = document.getElementById("<%= txtPassword.ClientID %>");

        username.focus();

        username.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                password.focus();
            }
        });

        password.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                document.getElementById("<%= btnLogin.ClientID %>").click();
            }
        });

        });
        (function () {
            document.querySelectorAll('.pw-toggle').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var input = btn.previousElementSibling;
                    var showing = input.type === 'text';
                    input.type = showing ? 'password' : 'text';
                    btn.innerHTML = showing ? '<i class="ti ti-eye"></i>' : '<i class="ti ti-eye-off"></i>';
                    btn.setAttribute('aria-label', showing ? 'نمایش رمز عبور' : 'پنهان‌کردن رمز عبور');
                });
            });
        })();
    </script>
</form>
</body>
</html>
