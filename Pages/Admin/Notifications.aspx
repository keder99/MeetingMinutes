<%@ Page Title="اعلان‌ها" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Notifications.aspx.cs" Inherits="MeetingMinutes.Notifications" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>اعلان‌ها</h1>
            <p>پیام‌هایی که در نوار بالای سامانه برای همه‌ی کاربران نمایش داده می‌شود</p>
        </div>
        <asp:LinkButton ID="btnNew" runat="server" CssClass="btn btn-primary" CausesValidation="false" OnClick="btnNew_Click"><i class="ti ti-plus"></i> اعلان جدید</asp:LinkButton>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel ID="pnlEdit" runat="server" Visible="false" CssClass="panel mb" DefaultButton="btnSave">
        <div class="panel-head"><h2><asp:Literal ID="litEditTitle" runat="server" /></h2></div>
        <div class="panel-body">
            <asp:HiddenField ID="hfId" runat="server" />
            <div class="form-grid">
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtTitle" Text="عنوان (اختیاری)" />
                    <asp:TextBox ID="txtTitle" runat="server" MaxLength="200" placeholder="اگر خالی بماند، بخشی از متن پیام به‌عنوان عنوان نشان داده می‌شود" />
                </div>
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtBody" Text="متن پیام *" />
                    <asp:TextBox ID="txtBody" runat="server" TextMode="MultiLine" Rows="4" MaxLength="1000" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtStart" Text="نمایش از تاریخ (شمسی)" />
                    <asp:TextBox ID="txtStart" runat="server" CssClass="date-input" placeholder="اگر خالی بماند، بلافاصله" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtEnd" Text="نمایش تا تاریخ (شمسی)" />
                    <asp:TextBox ID="txtEnd" runat="server" CssClass="date-input" placeholder="اگر خالی بماند، بدون انقضا" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="field full"><div class="hint">پیام از ابتدای «تاریخ شروع» تا پایان روزِ «تاریخ پایان» برای همه‌ی کاربران نمایش داده می‌شود؛ هر کاربر می‌تواند جدا آن را ببندد.</div></div>
            </div>
            <div class="actions-row">
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                <asp:Button ID="btnCancel" runat="server" Text="انصراف" CssClass="btn" CausesValidation="false" OnClick="btnCancel_Click" />
            </div>
        </div>
    </asp:Panel>

    <div class="panel">
        <div class="table-wrap">
            <asp:Repeater ID="rptList" runat="server" OnItemCommand="rptList_ItemCommand">
                <HeaderTemplate>
                    <table class="table">
                        <thead><tr><th>وضعیت</th><th>عنوان/متن</th><th>بازه‌ی نمایش</th><th>ایجادکننده</th><th>خوانده‌شده توسط</th><th></th></tr></thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td><span class='badge <%#: StateClass(Eval("StateCode")) %>'><%#: StateText(Eval("StateCode")) %></span></td>
                        <td style="max-width:360px"><b><%#: NotifTitle(Eval("Title"), Eval("Body")) %></b>
                            <div class="sub-hint" style="margin:2px 0 0"><%#: Eval("Body") %></div></td>
                        <td style="white-space:nowrap"><%#: RangeText(Container.DataItem) %></td>
                        <td><%#: Eval("CreatedByName") %></td>
                        <td><%#: Fa(Eval("DismissCount")) %> نفر</td>
                        <td>
                            <asp:LinkButton runat="server" CommandName="edit" CommandArgument='<%# Eval("NotificationId") %>' CssClass="link-btn" CausesValidation="false" Text="ویرایش" />
                            <asp:LinkButton runat="server" CommandName="delete" CommandArgument='<%# Eval("NotificationId") %>' CssClass="link-btn danger" CausesValidation="false" Text="حذف"
                                OnClientClick="return confirm('این اعلان حذف شود؟');" />
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate></tbody></table></FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty"><i class="ti ti-speakerphone"></i>هنوز اعلانی ثبت نشده است.</asp:Panel>
        </div>
    </div>

</asp:Content>
