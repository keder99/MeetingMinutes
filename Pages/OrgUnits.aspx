<%@ Page Title="واحدهای سازمانی" Language="C#"MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="OrgUnits.aspx.cs" Inherits="MeetingMinutes.OrgUnits" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>واحدهای سازمانی</h1>
            <p>معاونت‌ها، ادارات و واحدهایی که می‌توانند مسئول پیگیری مصوبات باشند</p>
        </div>
        <asp:LinkButton ID="btnNew" runat="server" CssClass="btn btn-primary" CausesValidation="false" OnClick="btnNew_Click"><i class="ti ti-plus"></i> افزودن واحد</asp:LinkButton>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel ID="pnlEdit" runat="server" Visible="false" CssClass="panel mb" DefaultButton="btnSave">
        <div class="panel-head"><h2><asp:Literal ID="litEditTitle" runat="server" /></h2></div>
        <div class="panel-body">
            <asp:HiddenField ID="hfUnitId" runat="server" />
            <div class="form-grid">
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtTitle" Text="عنوان واحد *" />
                    <asp:TextBox ID="txtTitle" runat="server" MaxLength="150" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlParent" Text="واحد بالادستی" />
                    <asp:DropDownList ID="ddlParent" runat="server" />
                </div>
                <div class="field">
                    <asp:CheckBox ID="chkActive" runat="server" Text="فعال" CssClass="check" />
                </div>
            </div>
            <div class="actions-row">
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                <asp:Button ID="btnCancel" runat="server" Text="بستن" CssClass="btn" CausesValidation="false" OnClick="btnCancel_Click" />
            </div>
        </div>
    </asp:Panel>

    <div class="panel">
        <div class="table-wrap">
            <asp:Repeater ID="rptUnits" runat="server" OnItemCommand="rptUnits_ItemCommand">
                <HeaderTemplate>
                    <table class="table">
                        <thead><tr><th>عنوان</th><th>واحد بالادستی</th><th>تعداد افراد</th><th>وضعیت</th><th></th></tr></thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td><b><%#: Eval("Title") %></b></td>
                        <td><%#: Eval("ParentTitle") %></td>
                        <td><%#: Eval("PeopleCount") %></td>
                        <td><%# (bool)Eval("IsActive") ? "<span class='badge done'>فعال</span>" : "<span class='badge red'>غیرفعال</span>" %></td>
                        <td>
                            <asp:LinkButton runat="server" CommandName="edit" CommandArgument='<%# Eval("OrgUnitId") %>'
                                CssClass="link-btn" CausesValidation="false" Text="ویرایش" />
                            <asp:LinkButton runat="server" CommandName="toggle"
                                CommandArgument='<%# Eval("OrgUnitId") + "|" + ((bool)Eval("IsActive") ? "0" : "1") %>'
                                CssClass="link-btn" CausesValidation="false"
                                Text='<%# (bool)Eval("IsActive") ? "غیرفعال‌سازی" : "فعال‌سازی" %>'
                                OnClientClick="return confirm('وضعیت این واحد تغییر کند؟');" />
                            <asp:LinkButton runat="server" CommandName="delete" CommandArgument='<%# Eval("OrgUnitId") %>'
                                CssClass="link-btn danger" CausesValidation="false" Text="حذف"
                                OnClientClick="return confirm('این واحد برای همیشه حذف شود؟ این کار قابل بازگشت نیست.');" />
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                        </tbody>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty">
                <i class="ti ti-building"></i>هنوز واحدی ثبت نشده است.
            </asp:Panel>
        </div>
    </div>

</asp:Content>
