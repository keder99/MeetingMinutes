<%@ Page Title="جزئیات جلسه" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="MeetingView.aspx.cs" Inherits="MeetingMinutes.MeetingView" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Literal ID="litWatermark" runat="server" />

    <div class="breadcrumb">
        <a runat="server" href="~/Pages/Default.aspx">داشبورد</a><i class="ti ti-chevron-left"></i>
        <a runat="server" href="~/Pages/Meetings.aspx">جلسات</a><i class="ti ti-chevron-left"></i>
        <span class="current"><asp:Literal ID="litCrumb" runat="server" Mode="Encode" /></span>
    </div>

    <div class="page-head">
        <div>
            <h1><asp:Literal ID="litTitle" runat="server" Mode="Encode" /></h1>
            <p><asp:Literal ID="litSub" runat="server" Mode="Encode" /></p>
        </div>
        <div class="actions">
            <asp:Literal ID="litStatusBadge" runat="server" />
            <a runat="server" id="lnkMinutes" href="~/Pages/Meetings.aspx" class="btn btn-primary"><i class="ti ti-file-text"></i> ثبت مشروح و مصوبات</a>
            <a runat="server" id="lnkEdit" href="~/Pages/Meetings.aspx" class="btn"><i class="ti ti-edit"></i> ویرایش</a>
            <asp:LinkButton ID="btnFinalize" runat="server" CssClass="btn" CausesValidation="false" OnClick="btnFinalize_Click"
                OnClientClick="return confirm('جلسه نهایی و قفل شود؟ پس از آن ویرایش ممکن نیست مگر با بازگشایی توسط مدیر.');"><i class="ti ti-lock"></i> نهایی‌سازی</asp:LinkButton>
            <asp:LinkButton ID="btnReopen" runat="server" CssClass="btn" CausesValidation="false" OnClick="btnReopen_Click"
                OnClientClick="return confirm('جلسه بازگشایی شود؟');"><i class="ti ti-lock-open"></i> بازگشایی</asp:LinkButton>
            <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn" CausesValidation="false" OnClick="btnDelete_Click"
                OnClientClick="return confirm('این جلسه حذف شود؟');"><i class="ti ti-trash"></i> حذف</asp:LinkButton>
            <a runat="server" id="lnkWord" href="~/Pages/MeetingWordExport.ashx" class="btn btn-primary"><i class="ti ti-file-type-doc"></i> دانلود Word</a>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel-head"><h2>اطلاعات جلسه</h2></div>
                <div class="panel-body"><asp:Literal ID="litMain" runat="server" /></div>
            </div>
        </div>
        <div>
            <div class="panel">
                <div class="panel-head"><h2>حضور و غیاب</h2><span><asp:Literal ID="litAttCount" runat="server" Mode="Encode" /></span></div>
                <div class="panel-body"><asp:Literal ID="litAttendees" runat="server" /></div>
            </div>
            <div class="panel" style="margin-top:16px">
                <div class="panel-head"><h2>دبیر جلسه</h2></div>
                <div class="panel-body"><asp:Literal ID="litSecretary" runat="server" /></div>
            </div>

            <div class="panel" style="margin-top:16px">
                <div class="panel-head"><h2>ضمائم</h2></div>
                <div class="panel-body">
                    <asp:Repeater ID="rptAttachments" runat="server" OnItemCommand="rptAttachments_ItemCommand">
                        <ItemTemplate>
                            <div class="att-file-row">
                                <i class='ti <%#: FileIcon(Eval("ContentType")) %>'></i>
                                <div class="grow">
                                    <a href='<%#: ResolveUrl("~/Pages/AttachmentDownload.ashx?id=" + Eval("AttachmentId")) %>' target="_blank" title='<%#: Eval("FileName") %>'><%#: Eval("FileName") %></a>
                                    <small style="display:block;color:var(--muted);font-size:10px;margin-top:2px">
                                        <%#: KindTitle(Eval("Kind")) %> · <%#: SizeText(Eval("SizeBytes")) %> · <%#: Eval("UploaderName") %> · <%#: FaDate(Eval("UploadedAt")) %>
                                    </small>
                                </div>
                                <asp:LinkButton runat="server" Visible="<%# CanManageAttachments %>" CommandName="delete" CommandArgument='<%# Eval("AttachmentId") %>'
                                    CssClass="link-btn danger" CausesValidation="false" Text="حذف"
                                    OnClientClick="return confirm('این ضمیمه حذف شود؟');" />
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlNoAttachments" runat="server" Visible="false" CssClass="empty"><i class="ti ti-paperclip"></i>ضمیمه‌ای بارگذاری نشده است.</asp:Panel>

                    <asp:Panel ID="pnlUpload" runat="server" Visible="false" CssClass="upload-row">
                        <asp:DropDownList ID="ddlKind" runat="server">
                            <asp:ListItem Value="1" Text="اسکن صورت‌جلسه امضاشده" />
                            <asp:ListItem Value="2" Text="ضمیمه" Selected="True" />
                        </asp:DropDownList>
                        <asp:FileUpload ID="fuAttachment" runat="server" />
                        <asp:Button ID="btnUpload" runat="server" Text="بارگذاری" CssClass="btn" CausesValidation="false" OnClick="btnUpload_Click" />
                        <div class="hint">فرمت‌های مجاز: <asp:Literal ID="litAllowedExt" runat="server" /> — حداکثر ۱۵ مگابایت</div>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
