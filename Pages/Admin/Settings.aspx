<%@ Page Title="تنظیمات" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="MeetingMinutes.Settings" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>تنظیمات</h1>
            <p>پیکربندی عمومی سامانه مدیریت جلسات</p>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel runat="server" DefaultButton="btnSave" CssClass="panel settings-panel">
        <div class="panel-body">
            <div class="field">
                <asp:Label runat="server" AssociatedControlID="txtOrgName" Text="نام سازمان" />
                <asp:TextBox ID="txtOrgName" runat="server" MaxLength="200" />
                <div class="hint">در نوار بالای سامانه، کنار عنوان «صورت‌جلسه» نمایش داده می‌شود.</div>
            </div>

            <div class="field" style="margin-top:14px">
                <asp:Label runat="server" AssociatedControlID="txtPrefix" Text="پیشوند شماره‌ی جلسات" />
                <asp:TextBox ID="txtPrefix" runat="server" CssClass="ltr" MaxLength="50" />
                <div class="hint">
                    باید شامل <code dir="ltr">{YYYY}</code> باشد (سال شمسی جلسه). نمونه با مقدار فعلی:
                    <b dir="ltr"><asp:Literal ID="litSample" runat="server" /></b>
                </div>
            </div>

            <div class="setting-row" style="margin-top:20px">
                <div>
                    <p class="stitle">یادآوری خودکار اقدامات معوق</p>
                    <p class="sdesc">نمایش برجسته‌ی اقدام‌های دارای مهلت گذشته در داشبورد و فهرست اقدامات</p>
                </div>
                <label class="switch">
                    <asp:CheckBox ID="chkOverdue" runat="server" />
                    <span class="slider"></span>
                </label>
            </div>

            <div class="actions-row" style="margin-top:20px">
                <asp:Button ID="btnSave" runat="server" Text="ذخیره‌ی تنظیمات" CssClass="btn btn-primary" OnClick="btnSave_Click" />
            </div>
        </div>
    </asp:Panel>

</asp:Content>
