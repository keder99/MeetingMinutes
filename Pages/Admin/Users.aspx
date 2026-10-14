<%@ Page Title="کاربران" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="MeetingMinutes.Users" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>کاربران</h1>
            <p>حساب‌های ورود به سامانه و سطح دسترسی آن‌ها</p>
        </div>
        <asp:LinkButton ID="btnNew" runat="server" CssClass="btn btn-primary" CausesValidation="false" OnClick="btnNew_Click"><i class="ti ti-plus"></i> افزودن کاربر</asp:LinkButton>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel ID="pnlEdit" runat="server" Visible="false" CssClass="panel mb" DefaultButton="btnSave">
        <div class="panel-head">
            <h2>
                <asp:Literal ID="litEditTitle" runat="server" /></h2>
        </div>
        <div class="panel-body">
            <asp:HiddenField ID="hfUserId" runat="server" />
            <div class="form-grid">
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtUsername" Text="نام کاربری *" />
                    <asp:TextBox ID="txtUsername" runat="server" CssClass="ltr" MaxLength="50" autocomplete="off" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlPerson" Text="فرد مربوط" />
                    <asp:DropDownList ID="ddlPerson" runat="server" />
                    <div class="hint">فقط افراد فعالی که هنوز حساب ندارند نمایش داده می‌شوند. فرد را ابتدا در صفحه‌ی «افراد» ثبت کنید.</div>
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlRole" Text="نقش *" />
                    <asp:DropDownList ID="ddlRole" runat="server" />
                </div>
                <div class="field">
                    <asp:CheckBox ID="chkActive" runat="server" Text="حساب فعال" CssClass="check" />
                </div>
            </div>

            <asp:Panel ID="pnlNew" runat="server" CssClass="form-grid" Visible="false" Style="margin-top: 12px">
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtPassword" Text="رمز عبور موقت * (حداقل ۸ نویسه)" />
                    <div class="password-field">
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="ltr" MaxLength="100" autocomplete="new-password" />
                    <button type="button" class="pw-toggle" tabindex="-1" aria-label="نمایش رمز عبور"><i class="ti ti-eye"></i></button>
                    </div>
                    <div class="hint">این رمز را به کاربر بدهید؛ در اولین ورود باید آن را تغییر دهد.</div>
                </div>
            </asp:Panel>

            <div class="actions-row">
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                <asp:Button ID="btnCancel" runat="server" Text="بستن" CssClass="btn" CausesValidation="false" OnClick="btnCancel_Click" />
            </div>

            <asp:Panel ID="pnlExisting" runat="server" Visible="false" DefaultButton="btnReset">
                <div class="sub-title">ریست رمز عبور</div>
                <p class="sub-hint">رمز موقت جدید می‌گذارد و قفل حساب را هم برمی‌دارد. کاربر در ورود بعدی باید رمز را عوض کند. رمز موقت جدید را نوشته و سپس ریست رمز را بزنید تا تغییر ذخیره شود.</p>
                <div class="field inline-add">
                    <asp:TextBox ID="txtReset" runat="server" TextMode="Password" MaxLength="100" Style="width: 50%;" placeholder="رمز موقت جدید" autocomplete="new-password" />
                    <asp:Button ID="btnReset" runat="server" Text="ریست رمز" CssClass="btn" Style="width: 25%;" CausesValidation="false" OnClick="btnReset_Click"
                        OnClientClick="return confirm('رمز این کاربر ریست شود؟');" />
                </div>
                <div class="actions-row">
                    <asp:Button ID="btnUnlock" runat="server" Text="باز کردن قفل حساب" CssClass="btn" CausesValidation="false" OnClick="btnUnlock_Click" />
                </div>
                <p class="sub-hint" style="margin: 3px 0px 0px !important">پس از 5 تلاش ناموفق برای ورود، حساب به مدت 15 دقیقه قفل می‌شود. برای باز کرد قفل حساب کاربر کلیک کنید. </p>
            </asp:Panel>
        </div>
    </asp:Panel>

    <div class="panel">
        <div class="table-wrap">
            <asp:Repeater ID="rptUsers" runat="server" OnItemCommand="rptUsers_ItemCommand">
                <HeaderTemplate>
                    <table class="table">
                        <thead>
                            <tr>
                                <th style="width: 5%;"></th>
                                <th>نام کاربری</th>
                                <th>فرد</th>
                                <th>ایمیل</th>
                                <th>تلفن همراه</th>
                                <th>نقش</th>
                                <th>وضعیت</th>
                                <th>آخرین ورود</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                    <tr>
                        <td class="avatar-cell"><%# AvatarCell(Eval("PersonId"), Eval("PhotoVersion"), Eval("FullName"), Eval("Username")) %></td>
                        <td><b dir="ltr"><%#: Eval("Username") %></b></td>
                        <td><%#: Eval("FullName") %></td>
                        <td dir="ltr"><%#: Str(Eval("Email")) %></td>
                        <td dir="ltr"><%#: Str(Eval("Mobile")) %></td>
                        <td><%#: Eval("RoleTitle") %></td>
                        <td>
                            <%# (bool)Eval("IsActive") ? "<span class='badge done'>فعال</span>" : "<span class='badge red'>غیرفعال</span>" %>
                            <%# (bool)Eval("IsLocked") ? "<span class='badge pending'>قفل‌شده</span>" : "" %>
                            <%# (bool)Eval("MustChangePassword") ? "<span class='badge draft'>رمز موقت</span>" : "" %>
                        </td>
                        <td><%# FormatLogin(Eval("LastLoginAt")) %></td>
                        <td>
                            <asp:LinkButton runat="server" CommandName="edit" CommandArgument='<%# Eval("UserId") %>'
                                CssClass="link-btn" CausesValidation="false" Text="ویرایش" />
                            <asp:LinkButton runat="server" CommandName="delete" CommandArgument='<%# Eval("UserId") %>'
                                CssClass="link-btn danger" CausesValidation="false" Text="حذف"
                                OnClientClick="return confirm('این کاربر برای همیشه حذف شود؟ این کار قابل بازگشت نیست.');" />
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </tbody>
                    </table>
               
                </FooterTemplate>
            </asp:Repeater>
        </div>
    </div>
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
