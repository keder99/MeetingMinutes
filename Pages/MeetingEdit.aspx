<%@ Page Title="ثبت جلسه" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="MeetingEdit.aspx.cs" Inherits="MeetingMinutes.MeetingEdit" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="breadcrumb">
        <a runat="server" href="~/Pages/Meetings.aspx">جلسات</a><i class="ti ti-chevron-left"></i>
        <span class="current"><asp:Literal ID="litCrumb" runat="server" Mode="Encode" /></span>
    </div>

    <div class="page-head">
        <div>
            <h1><asp:Literal ID="litHeading" runat="server" Mode="Encode" /></h1>
            <p><asp:Literal ID="litSub" runat="server" Mode="Encode" /></p>
        </div>
        <div class="actions">
            <a runat="server" id="lnkMinutes" href="~/Pages/Meetings.aspx" class="btn btn-primary"><i class="ti ti-file-text"></i> مشروح و مصوبات</a>
            <a runat="server" id="lnkView" href="~/Pages/Meetings.aspx" class="btn"><i class="ti ti-eye"></i> مشاهده جلسه</a>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <%-- ===== اطلاعات جلسه ===== --%>
    <asp:Panel ID="pnlInfo" runat="server" CssClass="panel mb" DefaultButton="btnSave">
        <div class="panel-head"><h2>اطلاعات جلسه</h2></div>
        <div class="panel-body">
            <asp:HiddenField ID="hfMeetingId" runat="server" />
            <asp:HiddenField ID="hfRowVer" runat="server" />
            <div class="form-grid">
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlType" Text="نوع جلسه *" />
                    <asp:DropDownList ID="ddlType" runat="server" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtLetterNo" Text="شماره‌ی نامه (سربرگ اداری)" />
                    <asp:TextBox ID="txtLetterNo" runat="server" CssClass="ltr" MaxLength="50" />
                </div>
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtTitle" Text="عنوان جلسه" />
                    <asp:TextBox ID="txtTitle" runat="server" MaxLength="300" />
                    <div class="hint">اختیاری؛ اگر خالی بماند، نوع جلسه به‌عنوان عنوان نمایش داده می‌شود.</div>
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtDate" Text="تاریخ (شمسی) *" />
                    <asp:TextBox ID="txtDate" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlStatus" Text="وضعیت *" />
                    <asp:DropDownList ID="ddlStatus" runat="server" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtStart" Text="ساعت شروع" />
                    <asp:TextBox ID="txtStart" runat="server" CssClass="ltr" MaxLength="5" placeholder="10:00" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtEnd" Text="ساعت پایان" />
                    <asp:TextBox ID="txtEnd" runat="server" CssClass="ltr" MaxLength="5" placeholder="11:45" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlLocation" Text="محل جلسه" />
                    <asp:DropDownList ID="ddlLocation" runat="server" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtNewLocation" Text="یا محل جدید" />
                    <asp:TextBox ID="txtNewLocation" runat="server" MaxLength="200" placeholder="در صورت نبودن در فهرست، اینجا بنویسید" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlChair" Text="رئیس جلسه" />
                    <asp:DropDownList ID="ddlChair" runat="server" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlSecretary" Text="دبیر جلسه" />
                    <asp:DropDownList ID="ddlSecretary" runat="server" />
                </div>
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtPreamble" Text="شرح آغاز جلسه" />
                    <asp:TextBox ID="txtPreamble" runat="server" TextMode="MultiLine" Rows="4" />
                    <div class="hint">تلاوت قرآن، سخن اخلاقی، قدردانی‌ها و مقدمه‌ی رئیس جلسه.</div>
                </div>
            </div>
            <div class="actions-row">
                <asp:Button ID="btnSave" runat="server" Text="ذخیره" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                <a runat="server" href="~/Pages/Meetings.aspx" class="btn">بازگشت به فهرست</a>
            </div>
        </div>
    </asp:Panel>

    <%-- ===== حاضران ===== --%>
    <asp:Panel ID="pnlAttendees" runat="server" Visible="false" CssClass="panel mb">
        <div class="panel-head">
            <h2>حاضران و غایبان</h2>
            <asp:Button ID="btnAddBoard" runat="server" Text="افزودن همه‌ی اعضای هیأت" CssClass="btn" CausesValidation="false" OnClick="btnAddBoard_Click" />
        </div>
        <div class="panel-body">
            <asp:Repeater ID="rptAtt" runat="server" OnItemCommand="rptAtt_ItemCommand">
                <ItemTemplate>
                    <div class="att-row">
                        <div>
                            <span class="att-name"><%#: Eval("FullName") %></span>
                            <%# (bool)Eval("IsGuest") ? "<span class='badge draft'>مهمان</span>" : "" %>
                            <asp:HiddenField ID="hfPid" runat="server" Value='<%# Eval("PersonId") %>' />
                        </div>
                        <div><asp:TextBox ID="txtPos" runat="server" Text='<%# Eval("PositionAtMeeting") %>' MaxLength="150" placeholder='<%# Eval("Position") %>' /></div>
                        <div>
                            <asp:DropDownList ID="ddlAtt" runat="server" SelectedValue='<%# Eval("AttendanceStatusId") %>'>
                                <asp:ListItem Value="1" Text="حاضر" />
                                <asp:ListItem Value="2" Text="غایب" />
                                <asp:ListItem Value="3" Text="غایب موجه" />
                            </asp:DropDownList>
                        </div>
                        <div>
                            <asp:LinkButton runat="server" CommandName="remove" CommandArgument='<%# Eval("PersonId") %>'
                                CssClass="link-btn danger" CausesValidation="false" Text="حذف"
                                OnClientClick="return confirm('این فرد از فهرست حاضران حذف شود؟');" />
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlNoAtt" runat="server" Visible="false" CssClass="empty"><i class="ti ti-users"></i>هنوز حاضری ثبت نشده است.</asp:Panel>

            <div class="inline-add" style="max-width:none;margin-top:14px">
                <asp:DropDownList ID="ddlAddPerson" runat="server" />
                <asp:CheckBox ID="chkAddGuest" runat="server" Text="مهمان" />
                <asp:Button ID="btnAddAttendee" runat="server" Text="افزودن" CssClass="btn" CausesValidation="false" OnClick="btnAddAttendee_Click" />
            </div>
            <div class="actions-row">
                <asp:Button ID="btnSaveAtt" runat="server" Text="ذخیره‌ی حضور و غیاب" CssClass="btn btn-primary" CausesValidation="false" OnClick="btnSaveAtt_Click" />
            </div>
        </div>
    </asp:Panel>

    <%-- ===== دستور جلسه ===== --%>
    <asp:Panel ID="pnlAgenda" runat="server" Visible="false" CssClass="panel mb" DefaultButton="btnAddAgenda">
        <div class="panel-head"><h2>دستور جلسه</h2></div>
        <div class="panel-body">
            <asp:Repeater ID="rptAgenda" runat="server" OnItemCommand="rptAgenda_ItemCommand">
                <ItemTemplate>
                    <div class="agenda-row">
                        <span class="no"><%#: Fa(Container.ItemIndex + 1) %>.</span>
                        <span class="t"><%#: Eval("Title") %></span>
                        <asp:LinkButton runat="server" CommandName="up" CommandArgument='<%# Eval("AgendaItemId") %>' CssClass="link-btn" CausesValidation="false" ToolTip="بالاتر" Text="↑" />
                        <asp:LinkButton runat="server" CommandName="down" CommandArgument='<%# Eval("AgendaItemId") %>' CssClass="link-btn" CausesValidation="false" ToolTip="پایین‌تر" Text="↓" />
                        <asp:LinkButton runat="server" CommandName="remove" CommandArgument='<%# Eval("AgendaItemId") %>' CssClass="link-btn danger" CausesValidation="false" Text="حذف"
                            OnClientClick="return confirm('این بند حذف شود؟');" />
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlNoAgenda" runat="server" Visible="false" CssClass="empty"><i class="ti ti-list-numbers"></i>هنوز بندی برای دستور جلسه ثبت نشده است.</asp:Panel>

            <div class="inline-add" style="max-width:none;margin-top:14px">
                <asp:TextBox ID="txtAgenda" runat="server" MaxLength="500" placeholder="بند جدید دستور جلسه" />
                <asp:Button ID="btnAddAgenda" runat="server" Text="افزودن" CssClass="btn" CausesValidation="false" OnClick="btnAddAgenda_Click" />
            </div>
        </div>
    </asp:Panel>

</asp:Content>
