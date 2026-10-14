<%@ Page Title="داشبورد" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MeetingMinutes._Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>داشبورد</h1>
            <p>نمای کلی وضعیت جلسات، مصوبات و اقدامات هیأت‌رئیسه</p>
        </div>
    </div>

    <asp:Literal ID="litError" runat="server" />

    <div class="cards">
        <div class="stat c-blue">
            <div class="info">
                <span class="label">کل جلسات</span>
                <div class="num">
                    <asp:Literal ID="litTotal" runat="server" Text="۰" /></div>
                <div class="trend">جلسات ثبت‌شده</div>
            </div>
            <div class="ico"><i class="ti ti-calendar-event"></i></div>
        </div>

        <div class="stat c-green">
            <div class="info">
                <span class="label">جلسات برگزار شده</span>
                <div class="num">
                    <asp:Literal ID="litHeld" runat="server" Text="۰" /></div>
                <div class="trend">از
                    <asp:Literal ID="litHeldOf" runat="server" Text="۰" />
                    جلسه</div>
            </div>
            <div class="ico"><i class="ti ti-circle-check"></i></div>
        </div>

        <div class="stat c-amber">
            <div class="info">
                <span class="label">اقدامات باز</span>
                <div class="num">
                    <asp:Literal ID="litOpen" runat="server" Text="۰" /></div>
                <div class="trend">نیازمند پیگیری<asp:Literal ID="litOverdue" runat="server" /></div>
            </div>
            <div class="ico"><i class="ti ti-list-check"></i></div>
        </div>

        <div class="stat c-purple">
            <div class="info">
                <span class="label">کل مصوبات</span>
                <div class="num">
                    <asp:Literal ID="litDecisions" runat="server" Text="۰" /></div>
                <div class="trend">در
                    <asp:Literal ID="litDecisionsOf" runat="server" Text="۰" />
                    جلسه</div>
            </div>
            <div class="ico"><i class="ti ti-gavel"></i></div>
        </div>
    </div>

    <div class="grid-2">
        <div>
            <div class="panel mb">
                <div class="panel-head">
                    <h2>آخرین جلسات</h2>
                    <a runat="server" href="~/Pages/Meetings.aspx" class="btn">مشاهده همه</a>
                </div>
                <div class="panel-body meeting-list">
                    <asp:Repeater ID="rptMeetings" runat="server">
                        <ItemTemplate>
                            <a class="mrow-link" href='<%#: ResolveUrl("~/Pages/MeetingView.aspx?id=" + Eval("MeetingId")) %>'>
                                <div class="mrow">
                                    <div class="avatar <%# AvatarClass(Container.ItemIndex) %>"><%#: Initial(Eval("DisplayTitle")) %></div>
                                    <div class="grow">
                                        <b style="font-size: 12px"><%#: Eval("DisplayTitle") %></b>
                                        <small style="display: block; color: var(--muted); font-size: 10px; margin-top: 3px">
                                            <%#: FaDate(Eval("MeetingDate")) %> · <%#: DecisionsLabel(Eval("DecisionCount")) %></small>
                                    </div>
                                    <span class="badge <%#: Eval("CssClass") %>"><%#: Eval("StatusTitle") %></span>
                                </div>
                            </a>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlNoMeetings" runat="server" Visible="false" CssClass="empty" Style="padding: 20px">
                        <i class="ti ti-calendar-off"></i>هنوز جلسه‌ای ثبت نشده است.
                   
                    </asp:Panel>
                </div>
            </div>

            <div class="panel">
                <div class="panel-head">
                    <h2>آخرین مصوبات</h2>
                    <a runat="server" href="~/Pages/Meetings.aspx" class="btn">مشاهده همه</a>
                </div>
                <div class="panel-body">
                    <asp:Repeater ID="rptDecisions" runat="server">
                        <ItemTemplate>
                            <a class="mrow-link" href='<%#: ResolveUrl("~/Pages/MeetingView.aspx?id=" + Eval("MeetingId")) %>'>
                                <div class="mrow">
                                    <div class="avatar purple"><%#: Initial(Eval("MeetingTitle")) %></div>
                                    <div class="grow">
                                        <b style="font-size: 12px"><%#: Shorten(Eval("DecisionText")) %></b>
                                        <small style="display: block; color: var(--muted); font-size: 10px; margin-top: 3px">
                                            <%#: Eval("MeetingTitle") %> · <%#: FaDate(Eval("MeetingDate")) %></small>
                                    </div>
                                </div>
                            </a>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlNoDecisions" runat="server" Visible="false" CssClass="empty" Style="padding: 20px">
                        <i class="ti ti-gavel"></i>هنوز مصوبه‌ای ثبت نشده است.
                   
                    </asp:Panel>
                </div>
            </div>
        </div>

        <div>
            <div class="panel mb">
                <div class="panel-head">
                    <h2>اقدامات در انتظار پیگیری</h2>
                    <a runat="server" href="~/Pages/Actions.aspx" class="btn">مشاهده همه</a>
                </div>
                <div class="panel-body">
                    <asp:Repeater ID="rptActions" runat="server">
                        <ItemTemplate>
                            <a class="mrow-link" href='<%#: ResolveUrl("~/Pages/ActionView.aspx?id=" + Eval("ActionId")) %>'>
                                <div class='mrow<%# ShowOverdue(Container.DataItem) ? " overdue-row" : "" %>'>
                                    <div class="avatar <%# ShowOverdue(Container.DataItem) ? "red" : "avatar-pink" %>"><%#: Initial(Eval("ResponsibleName")) %></div>
                                    <div class="grow">
                                        <b style="font-size: 12px"><%#: Eval("Title") %></b>
                                        <small style='display: block; font-size: 10px; margin-top: 3px; color: <%# ShowOverdue(Container.DataItem) ? "var(--red)" : "var(--muted)" %>'>
                                            <%#: Eval("ResponsibleName") %> · مهلت <%#: Due(Container.DataItem) %><%# ShowOverdue(Container.DataItem) ? " (گذشته)" : "" %></small>
                                    </div>
                                    <%# ShowOverdue(Container.DataItem)
                                        ? "<span class=\"badge red\"><i class=\"ti ti-alert-triangle\"></i>معوق</span>"
                                        : "<span class=\"badge " + Eval("CssClass") + "\">" + System.Web.HttpUtility.HtmlEncode((string)Eval("StatusTitle")) + "</span>" %>
                                </div>
                            </a>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Panel ID="pnlNoActions" runat="server" Visible="false" CssClass="empty" Style="padding: 20px">
                        <i class="ti ti-checklist"></i>اقدام بازی وجود ندارد.
                   
                    </asp:Panel>
                </div>
            </div>

            <div class="panel">
                <div class="panel-head">
                    <h2>توزیع وضعیت جلسات</h2>
                </div>
                <div class="panel-body">
                    <asp:Repeater ID="rptStatusBars" runat="server">
                        <ItemTemplate>
                            <div class="bar-row">
                                <span class="bar-label" title='<%#: Eval("Title") %>'><%#: Eval("Title") %></span>
                                <div class="bar-track">
                                    <div class="bar-fill" style='width: <%#: BarPercent(Container.DataItem) %>%'></div>
                                </div>
                                <span class="bar-num"><%#: Fa(Eval("Cnt")) %></span>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
