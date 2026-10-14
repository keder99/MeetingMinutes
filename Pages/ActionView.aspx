<%@ Page Title="جزئیات اقدام" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="ActionView.aspx.cs" Inherits="MeetingMinutes.ActionView" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="breadcrumb">
        <a runat="server" href="~/Pages/Actions.aspx">اقدامات</a><i class="ti ti-chevron-left"></i>
        <span class="current">جزئیات اقدام</span>
    </div>

    <div class="page-head">
        <div>
            <h1><asp:Literal ID="litTitle" runat="server" Mode="Encode" /></h1>
            <p><asp:Literal ID="litSub" runat="server" Mode="Encode" /></p>
        </div>
        <div class="actions">
            <asp:Literal ID="litStatusBadge" runat="server" />
            <a runat="server" id="lnkMeeting" href="~/Pages/Meetings.aspx" class="btn"><i class="ti ti-eye"></i> مشاهده جلسه</a>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel-head"><h2>مصوبه‌ی مربوط</h2></div>
                <div class="panel-body"><asp:Literal ID="litDecision" runat="server" /></div>
            </div>

            <div class="panel" style="margin-top:16px">
                <div class="panel-head"><h2>تاریخچه‌ی پیگیری</h2></div>
                <div class="panel-body">
                    <asp:Repeater ID="rptFollow" runat="server">
                        <ItemTemplate>
                            <div class="followup-row">
                                <div class="rowline">
                                    <b><%#: Eval("NewTitle") %></b>
                                    <span class="sub-hint"><%#: FaDate(Eval("FollowUpDate")) %></span>
                                </div>
                                <p class="sub-hint" style="margin:2px 0"><%#: Eval("ByName") %><%#: MeetingRef(Container.DataItem) %></p>
                                <p class="topic-text"><%# Multiline(Eval("Comment")) %></p>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlNoFollow" runat="server" Visible="false" CssClass="empty"><i class="ti ti-history"></i>هنوز پیگیری‌ای ثبت نشده است.</asp:Panel>
                </div>
            </div>
        </div>

        <div>
            <asp:Panel ID="pnlFollowForm" runat="server" CssClass="panel" DefaultButton="btnAddFollow">
                <div class="panel-head"><h2>ثبت پیگیری</h2></div>
                <div class="panel-body">
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="ddlNewStatus" Text="وضعیت جدید" />
                        <asp:DropDownList ID="ddlNewStatus" runat="server" />
                    </div>
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="ddlReviewMeeting" Text="بررسی‌شده در جلسه‌ی" />
                        <asp:DropDownList ID="ddlReviewMeeting" runat="server" />
                    </div>
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="txtComment" Text="توضیح" />
                        <asp:TextBox ID="txtComment" runat="server" TextMode="MultiLine" Rows="4" />
                    </div>
                    <div class="actions-row">
                        <asp:Button ID="btnAddFollow" runat="server" Text="ثبت پیگیری" CssClass="btn btn-primary" OnClick="btnAddFollow_Click" />
                    </div>
                </div>
            </asp:Panel>
            <asp:Panel ID="pnlNoPermission" runat="server" Visible="false" CssClass="panel">
                <div class="panel-body"><p class="sub-hint">فقط مسئول همین اقدام یا دبیر و مدیر می‌تواند پیگیری ثبت کند.</p></div>
            </asp:Panel>
        </div>
    </div>

</asp:Content>
