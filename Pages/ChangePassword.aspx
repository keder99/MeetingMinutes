<%@ Page Title="تغییر رمز عبور" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="MeetingMinutes.ChangePassword" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>تغییر رمز عبور</h1>
            <p>رمز عبور حساب خودتان را تغییر دهید</p>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel runat="server" DefaultButton="btnSave" CssClass="panel settings-panel">
        <div class="panel-body">
            <div class="field">
                <asp:Label runat="server" AssociatedControlID="txtCurrent" Text="رمز عبور فعلی" />
                <div class="password-field">
                    <asp:TextBox ID="txtCurrent" runat="server" TextMode="Password" CssClass="ltr" MaxLength="100" autocomplete="current-password" />
                    <button type="button" class="pw-toggle" tabindex="-1" aria-label="نمایش رمز عبور"><i class="ti ti-eye"></i></button>
                </div>
            </div>
            <div class="field">
                <asp:Label runat="server" AssociatedControlID="txtNew" Text="رمز عبور جدید (حداقل ۸ نویسه)" />
                <div class="password-field">
                    <asp:TextBox ID="txtNew" runat="server" TextMode="Password" CssClass="ltr" MaxLength="100" autocomplete="new-password" />
                    <button type="button" class="pw-toggle" tabindex="-1" aria-label="نمایش رمز عبور"><i class="ti ti-eye"></i></button>
                </div>
            </div>
            <div class="field">
                <asp:Label runat="server" AssociatedControlID="txtConfirm" Text="تکرار رمز عبور جدید" />
                <div class="password-field">
                    <asp:TextBox ID="txtConfirm" runat="server" TextMode="Password" CssClass="ltr" MaxLength="100" autocomplete="new-password" />
                    <button type="button" class="pw-toggle" tabindex="-1" aria-label="نمایش رمز عبور"><i class="ti ti-eye"></i></button>
                </div>
            </div>
            <div class="actions-row">
                <asp:Button ID="btnSave" runat="server" Text="تغییر رمز" CssClass="btn btn-primary" OnClick="btnSave_Click" />
            </div>
        </div>
    </asp:Panel>
    <script>
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
</asp:Content>
