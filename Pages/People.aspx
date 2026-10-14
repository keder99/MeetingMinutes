<%@ Page Title="افراد" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="People.aspx.cs" Inherits="MeetingMinutes.People" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>افراد</h1>
            <p>اعضای هیأت، مهمانان و مسئولان پیگیری مصوبات</p>
        </div>
        <asp:LinkButton ID="btnNew" runat="server" CssClass="btn btn-primary" CausesValidation="false" OnClick="btnNew_Click"><i class="ti ti-plus"></i> افزودن فرد</asp:LinkButton>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel ID="pnlEdit" runat="server" Visible="false" CssClass="panel mb" DefaultButton="btnSave">
        <div class="panel-head"><h2><asp:Literal ID="litEditTitle" runat="server" /></h2></div>
        <div class="panel-body">
            <asp:HiddenField ID="hfPersonId" runat="server" />
            <datalist id="prefixList">
                <option value="دکتر"></option><option value="مهندس"></option><option value="آقای"></option>
                <option value="خانم"></option><option value="خانم دکتر"></option><option value="حجت‌الاسلام"></option>
            </datalist>

            <div class="form-grid">
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtPrefix" Text="عنوان (دکتر، مهندس، ...)" />
                    <asp:TextBox ID="txtPrefix" runat="server" MaxLength="30" list="prefixList" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtFirstName" Text="نام" />
                    <asp:TextBox ID="txtFirstName" runat="server" MaxLength="80" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtLastName" Text="نام خانوادگی *" />
                    <asp:TextBox ID="txtLastName" runat="server" MaxLength="80" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtPosition" Text="سمت" />
                    <asp:TextBox ID="txtPosition" runat="server" MaxLength="150" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlUnit" Text="واحد سازمانی" />
                    <asp:DropDownList ID="ddlUnit" runat="server" />
                </div>
                <div class="field">
                    <asp:CheckBox ID="chkBoard" runat="server" Text="عضو هیأت" CssClass="check" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtEmail" Text="ایمیل" />
                    <asp:TextBox ID="txtEmail" runat="server" CssClass="ltr" MaxLength="150" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtMobile" Text="تلفن همراه" />
                    <asp:TextBox ID="txtMobile" runat="server" CssClass="ltr" MaxLength="20" />
                </div>
                <div class="field">
                    <asp:CheckBox ID="chkActive" runat="server" Text="فعال" CssClass="check" />
                </div>
            </div>

            <div class="actions-row">
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                <asp:Button ID="btnCancel" runat="server" Text="بستن" CssClass="btn" CausesValidation="false" OnClick="btnCancel_Click" />
            </div>

            <asp:Panel ID="pnlAliases" runat="server" Visible="false" DefaultButton="btnAddAlias">
                <div class="sub-title">نام‌های مستعار</div>
                <p class="sub-hint">املاهای دیگرِ همین فرد در صورت‌جلسه‌های قدیمی (مثلاً «قبائی» برای «قبایی»). جست‌وجو با این نام‌ها هم فرد را پیدا می‌کند.</p>
                <div class="chips">
                    <asp:Repeater ID="rptAliases" runat="server" OnItemCommand="rptAliases_ItemCommand">
                        <ItemTemplate>
                            <span class="chip"><%#: Eval("AliasName") %>
                                <asp:LinkButton runat="server" CommandName="remove" CommandArgument='<%# Eval("AliasId") %>'
                                    ToolTip="حذف" CausesValidation="false" Text="×"
                                    OnClientClick="return confirm('این نام مستعار حذف شود؟');" />
                            </span>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <div class="inline-add">
                    <asp:TextBox ID="txtAlias" runat="server" MaxLength="200" placeholder="نام مستعار جدید" />
                    <asp:Button ID="btnAddAlias" runat="server" Text="افزودن" CssClass="btn" CausesValidation="false" OnClick="btnAddAlias_Click" />
                </div>
            </asp:Panel>
        </div>
    </asp:Panel>

    <asp:Panel runat="server" DefaultButton="btnSearch" CssClass="filters">
        <div class="fgrp">
            <label>جست‌وجوی نام</label>
            <asp:TextBox ID="txtSearch" runat="server" CssClass="title-input" placeholder="نام یا نام مستعار..." />
        </div>
        <div class="fgrp">
            <label>وضعیت</label>
            <asp:DropDownList ID="ddlActive" runat="server">
                <asp:ListItem Value="" Text="همه" />
                <asp:ListItem Value="1" Text="فعال" />
                <asp:ListItem Value="0" Text="غیرفعال" />
            </asp:DropDownList>
        </div>
        <div class="fgrp">
            <label>عضویت در هیأت</label>
            <asp:DropDownList ID="ddlBoard" runat="server">
                <asp:ListItem Value="" Text="همه" />
                <asp:ListItem Value="1" Text="عضو هیأت" />
                <asp:ListItem Value="0" Text="غیرعضو" />
            </asp:DropDownList>
        </div>
        <asp:Button ID="btnSearch" runat="server" Text="اعمال فیلتر" CssClass="btn btn-primary" OnClick="btnSearch_Click" />
    </asp:Panel>

    <div class="panel">
        <div class="table-wrap">
            <asp:Repeater ID="rptPeople" runat="server" OnItemCommand="rptPeople_ItemCommand">
                <HeaderTemplate>
                    <table class="table">
                        <thead><tr><th>نام</th><th>سمت</th><th>واحد</th><th>هیأت</th><th>حساب کاربری</th><th>وضعیت</th><th></th></tr></thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td><b><%#: Eval("FullName") %></b></td>
                        <td><%#: Eval("Position") %></td>
                        <td><%#: Eval("UnitTitle") %></td>
                        <td><%# (bool)Eval("IsBoardMember") ? "<span class='badge done'>عضو</span>" : "<span class='badge draft'>غیرعضو</span>" %></td>
                        <td><%# (bool)Eval("HasUser") ? "<i class='ti ti-check'></i>" : "—" %></td>
                        <td><%# (bool)Eval("IsActive") ? "<span class='badge done'>فعال</span>" : "<span class='badge red'>غیرفعال</span>" %></td>
                        <td>
                            <asp:LinkButton runat="server" CommandName="edit" CommandArgument='<%# Eval("PersonId") %>'
                                CssClass="link-btn" CausesValidation="false" Text="ویرایش" />
                            <asp:LinkButton runat="server" CommandName="toggle"
                                CommandArgument='<%# Eval("PersonId") + "|" + ((bool)Eval("IsActive") ? "0" : "1") %>'
                                CssClass="link-btn" CausesValidation="false"
                                Text='<%# (bool)Eval("IsActive") ? "غیرفعال‌سازی" : "فعال‌سازی" %>'
                                OnClientClick="return confirm('وضعیت این فرد تغییر کند؟');" />
                            <asp:LinkButton runat="server" CommandName="delete" CommandArgument='<%# Eval("PersonId") %>'
                                CssClass="link-btn danger" CausesValidation="false" Text="حذف"
                                OnClientClick="return confirm('این فرد برای همیشه حذف شود؟ این کار قابل بازگشت نیست.');" />
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                        </tbody>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty">
                <i class="ti ti-users"></i>فردی یافت نشد.
            </asp:Panel>
        </div>
    </div>

</asp:Content>
