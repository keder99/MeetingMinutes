<%@ Page Title="ثبت صورت‌جلسه" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="MinutesEdit.aspx.cs" Inherits="MeetingMinutes.MinutesEdit" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="breadcrumb">
        <a runat="server" href="~/Pages/Meetings.aspx">جلسات</a><i class="ti ti-chevron-left"></i>
        <a runat="server" id="lnkCrumb" href="~/Pages/Meetings.aspx"><asp:Literal ID="litCrumb" runat="server" Mode="Encode" /></a><i class="ti ti-chevron-left"></i>
        <span class="current">مشروح و مصوبات</span>
    </div>

    <div class="page-head">
        <div>
            <h1>مشروح مذاکرات، مصوبات و اقدامات</h1>
            <p><asp:Literal ID="litSub" runat="server" Mode="Encode" /></p>
        </div>
        <div class="actions">
            <a runat="server" id="lnkInfo" href="~/Pages/Meetings.aspx" class="btn"><i class="ti ti-edit"></i> اطلاعات جلسه</a>
            <a runat="server" id="lnkView" href="~/Pages/Meetings.aspx" class="btn"><i class="ti ti-eye"></i> مشاهده جلسه</a>
            <asp:LinkButton ID="btnAddTopic" runat="server" CssClass="btn" CausesValidation="false" OnClick="btnAddTopic_Click"><i class="ti ti-plus"></i> موضوع جدید</asp:LinkButton>
            <asp:LinkButton ID="btnAddDecision" runat="server" CssClass="btn btn-primary" CausesValidation="false" OnClick="btnAddDecision_Click"><i class="ti ti-plus"></i> مصوبه جدید</asp:LinkButton>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <%-- ================= ویرایشگر (موضوع / مصوبه / اقدام) ================= --%>
    <asp:Panel ID="pnlEditor" runat="server" Visible="false" CssClass="panel mb" DefaultButton="btnEditorSave">
        <div class="panel-head"><h2><asp:Literal ID="litEditorTitle" runat="server" Mode="Encode" /></h2></div>
        <div class="panel-body">
            <asp:HiddenField ID="hfMode" runat="server" />
            <asp:HiddenField ID="hfId" runat="server" />
            <asp:HiddenField ID="hfCtx" runat="server" />
            <asp:HiddenField ID="hfKeepPerson" runat="server" />

            <asp:Panel ID="pnlTopicFields" runat="server" Visible="false" CssClass="form-grid">
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtTopicTitle" Text="عنوان موضوع *" />
                    <asp:TextBox ID="txtTopicTitle" runat="server" MaxLength="300" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlTopicParent" Text="زیرموضوعِ" />
                    <asp:DropDownList ID="ddlTopicParent" runat="server" />
                    <asp:Panel ID="pnlParentHint" runat="server" Visible="false" CssClass="hint">این موضوع خودش زیرموضوع دارد و نمی‌تواند زیرموضوعِ دیگری شود.</asp:Panel>
                </div>
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtTopicText" Text="شرح مذاکرات" />
                    <asp:TextBox ID="txtTopicText" runat="server" TextMode="MultiLine" Rows="6" />
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlDecisionFields" runat="server" Visible="false" CssClass="form-grid">
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="ddlDecisionTopic" Text="موضوع" />
                    <asp:DropDownList ID="ddlDecisionTopic" runat="server" />
                </div>
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtDecisionText" Text="متن مصوبه *" />
                    <asp:TextBox ID="txtDecisionText" runat="server" TextMode="MultiLine" Rows="5" />
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlActionFields" runat="server" Visible="false" CssClass="form-grid">
                <div class="field full">
                    <asp:Label runat="server" AssociatedControlID="txtActionTitle" Text="شرح اقدام *" />
                    <asp:TextBox ID="txtActionTitle" runat="server" MaxLength="400" />
                </div>
                <div class="field">
                    <label for="mmPersonSearch">جست‌وجوی مسئول (نام یا املای دیگر)</label>
                    <input type="text" id="mmPersonSearch" placeholder="مثلاً قبایی" autocomplete="off" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlPerson" Text="مسئول (فرد)" />
                    <asp:DropDownList ID="ddlPerson" runat="server" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlUnit" Text="مسئول (واحد)" />
                    <asp:DropDownList ID="ddlUnit" runat="server" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtNote" Text="یا توضیح مسئول" />
                    <asp:TextBox ID="txtNote" runat="server" MaxLength="200" placeholder="مثلاً «معاونین» — وقتی فرد یا واحد در فهرست نیست" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtDueDate" Text="مهلت (تاریخ شمسی)" />
                    <asp:TextBox ID="txtDueDate" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="txtDueText" Text="یا متن مهلت" />
                    <asp:TextBox ID="txtDueText" runat="server" MaxLength="100" placeholder="مثلاً «تا جلسه آینده»" />
                </div>
                <div class="field">
                    <asp:Label runat="server" AssociatedControlID="ddlActionStatus" Text="وضعیت" />
                    <asp:DropDownList ID="ddlActionStatus" runat="server" />
                </div>
                <div class="field full"><div class="hint">حداقل یکی از «فرد»، «واحد» یا «توضیح مسئول» لازم است.</div></div>
            </asp:Panel>

            <div class="actions-row">
                <asp:Button ID="btnEditorSave" runat="server" Text="ذخیره" CssClass="btn btn-primary" OnClick="btnEditorSave_Click" />
                <asp:Button ID="btnEditorCancel" runat="server" Text="انصراف" CssClass="btn" CausesValidation="false" OnClick="btnEditorCancel_Click" />
            </div>
        </div>
    </asp:Panel>

    <%-- ================= ساختار: موضوع ← مصوبه ← اقدام ================= --%>
    <div class="panel mb">
        <div class="panel-head"><h2>موضوعات و مصوبات</h2></div>
        <div class="panel-body">
            <asp:Repeater ID="rptTopics" runat="server" EnableViewState="false" OnItemDataBound="rptTopics_ItemDataBound" OnItemCommand="OnCmd">
                <ItemTemplate>
                    <div class="topic" style='margin-right:<%# ((int)Eval("Level")) * 24 %>px'>
                        <div class="rowline">
                            <p class="topic-title"><b><%# TopicNumber(Container.DataItem) %><%#: Eval("Title") %></b></p>
                            <span class="row-actions">
                                <asp:LinkButton runat="server" CommandName="topic-up" CommandArgument='<%# Eval("TopicId") %>' Visible='<%# !(bool)Eval("IsLoose") %>' CssClass="link-btn" Text="↑" ToolTip="بالاتر" CausesValidation="false" />
                                <asp:LinkButton runat="server" CommandName="topic-down" CommandArgument='<%# Eval("TopicId") %>' Visible='<%# !(bool)Eval("IsLoose") %>' CssClass="link-btn" Text="↓" ToolTip="پایین‌تر" CausesValidation="false" />
                                <asp:LinkButton runat="server" CommandName="topic-edit" CommandArgument='<%# Eval("TopicId") %>' Visible='<%# !(bool)Eval("IsLoose") %>' CssClass="link-btn" Text="ویرایش" CausesValidation="false" />
                                <asp:LinkButton runat="server" CommandName="dec-add" CommandArgument='<%# Eval("TopicId") %>' CssClass="link-btn" Text="+ مصوبه" CausesValidation="false" />
                                <asp:LinkButton runat="server" CommandName="topic-del" CommandArgument='<%# Eval("TopicId") %>' Visible='<%# !(bool)Eval("IsLoose") %>' CssClass="link-btn danger" Text="حذف" CausesValidation="false"
                                    OnClientClick="return confirm('این موضوع حذف شود؟ مصوبات آن حذف نمی‌شوند و «بدون موضوع» می‌شوند.');" />
                            </span>
                        </div>
                        <p class="topic-text"><%# Multiline(Eval("DiscussionText")) %></p>

                        <asp:Repeater ID="rptDecisions" runat="server" EnableViewState="false" OnItemDataBound="rptDecisions_ItemDataBound" OnItemCommand="OnCmd">
                            <ItemTemplate>
                                <div class="decision">
                                    <div class="rowline">
                                        <p><%# Multiline(Eval("DecisionText")) %></p>
                                        <span class="row-actions">
                                            <asp:LinkButton runat="server" CommandName="dec-up" CommandArgument='<%# Eval("DecisionId") %>' CssClass="link-btn" Text="↑" ToolTip="بالاتر" CausesValidation="false" />
                                            <asp:LinkButton runat="server" CommandName="dec-down" CommandArgument='<%# Eval("DecisionId") %>' CssClass="link-btn" Text="↓" ToolTip="پایین‌تر" CausesValidation="false" />
                                            <asp:LinkButton runat="server" CommandName="dec-edit" CommandArgument='<%# Eval("DecisionId") %>' CssClass="link-btn" Text="ویرایش" CausesValidation="false" />
                                            <asp:LinkButton runat="server" CommandName="act-add" CommandArgument='<%# Eval("DecisionId") %>' CssClass="link-btn" Text="+ اقدام" CausesValidation="false" />
                                            <asp:LinkButton runat="server" CommandName="dec-to-text" CommandArgument='<%# Eval("DecisionId") %>' CssClass="link-btn" Text="به شرح" ToolTip="این‌ها مصوبه نیست؛ به شرح مذاکرات منتقل شود" CausesValidation="false"
                                                OnClientClick="return confirm('متن به «شرح مذاکرات» منتقل و این مصوبه (همراه اقدام‌هایش) حذف شود؟');" />
                                            <asp:LinkButton runat="server" CommandName="dec-del" CommandArgument='<%# Eval("DecisionId") %>' CssClass="link-btn danger" Text="حذف" CausesValidation="false"
                                                OnClientClick="return confirm('این مصوبه و اقدام‌های آن حذف شود؟');" />
                                        </span>
                                    </div>
                                    <asp:Repeater ID="rptActions" runat="server" EnableViewState="false" OnItemCommand="OnCmd">
                                        <ItemTemplate>
                                            <div class="action-chip">
                                                <i class="ti ti-user-circle"></i><b><%#: ActionResponsible(Container.DataItem) %></b>
                                                <span>·</span>مهلت: <%#: ActionDue(Container.DataItem) %>
                                                <span>·</span><span class="badge <%#: Eval("CssClass") %>"><%#: Eval("StatusTitle") %></span>
                                                <asp:LinkButton runat="server" CommandName="act-done" CommandArgument='<%# Eval("ActionId") %>' Visible='<%# NotDone(Container.DataItem) %>' CssClass="link-btn" Text="انجام شد" CausesValidation="false" />
                                                <asp:LinkButton runat="server" CommandName="act-edit" CommandArgument='<%# Eval("ActionId") %>' CssClass="link-btn" Text="ویرایش" CausesValidation="false" />
                                                <asp:LinkButton runat="server" CommandName="act-del" CommandArgument='<%# Eval("ActionId") %>' CssClass="link-btn danger" Text="حذف" CausesValidation="false"
                                                    OnClientClick="return confirm('این اقدام حذف شود؟');" />
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty"><i class="ti ti-list-details"></i>هنوز موضوع یا مصوبه‌ای ثبت نشده است. با «موضوع جدید» یا «مصوبه جدید» شروع کنید.</asp:Panel>
        </div>
    </div>

    <%-- ================= جمع‌بندی ================= --%>
    <div class="panel mb">
        <div class="panel-head"><h2>جمع‌بندی رئیس جلسه</h2></div>
        <div class="panel-body">
            <asp:TextBox ID="txtSummary" runat="server" TextMode="MultiLine" Rows="4" CssClass="wide-text" />
            <div class="actions-row">
                <asp:Button ID="btnSaveSummary" runat="server" Text="ذخیره‌ی جمع‌بندی" CssClass="btn btn-primary" CausesValidation="false" OnClick="btnSaveSummary_Click" />
            </div>
        </div>
    </div>

    <script src="<%= ResolveUrl("~/Scripts/minutes.js") %>"></script>

</asp:Content>
