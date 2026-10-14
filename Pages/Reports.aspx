<%@ Page Title="گزارش‌ها" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="MeetingMinutes.Reports" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head no-print">
        <div>
            <h1>گزارش‌ها</h1>
            <p>نمای دوره‌ای مصوبات و اقدام‌ها بر اساس تاریخ جلسه — برای پیگیری روزانه از صفحه‌ی «اقدامات» استفاده کنید</p>
        </div>
        <div class="actions">
            <button type="button" class="btn" onclick="window.print()"><i class="ti ti-printer"></i> چاپ گزارش</button>
        </div>
    </div>

    <asp:Panel runat="server" DefaultButton="btnApply" CssClass="panel mb no-print">
        <div class="panel-body">
            <div class="filters">
                <div class="fgrp">
                    <label>از تاریخ (شمسی) *</label>
                    <asp:TextBox ID="txtFrom" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="fgrp">
                    <label>تا تاریخ (شمسی) *</label>
                    <asp:TextBox ID="txtTo" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="fgrp">
                    <label>نوع جلسه</label>
                    <asp:DropDownList ID="ddlType" runat="server" />
                </div>
                <div class="fgrp">
                    <label>مسئول (فرد)</label>
                    <asp:DropDownList ID="ddlPerson" runat="server" />
                </div>
                <div class="fgrp">
                    <label>مسئول (واحد)</label>
                    <asp:DropDownList ID="ddlUnit" runat="server" />
                </div>
                <div class="fgrp">
                    <label>جست‌وجو در متن</label>
                    <asp:TextBox ID="txtQuery" runat="server" CssClass="title-input" placeholder="عنوان موضوع یا متن مصوبه/اقدام..." />
                </div>
                <asp:Button ID="btnApply" runat="server" Text="اعمال" CssClass="btn btn-primary" OnClick="btnApply_Click" />
            </div>
            <asp:Literal ID="litFilterError" runat="server" />
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlResults" runat="server" Visible="false">
        <p class="report-range"><b><asp:Literal ID="litRangeTitle" runat="server" Mode="Encode" /></b></p>

        <div class="cards">
            <div class="stat c-blue">
                <div class="info">
                    <span class="label">جلسات</span>
                    <div class="num"><asp:Literal ID="litMeetingCount" runat="server" /></div>
                </div>
                <div class="ico"><i class="ti ti-calendar-event"></i></div>
            </div>

            <div class="stat c-purple">
                <div class="info">
                    <span class="label">مصوبات</span>
                    <div class="num"><asp:Literal ID="litDecisionCount" runat="server" /></div>
                </div>
                <div class="ico"><i class="ti ti-gavel"></i></div>
            </div>

            <div class="stat c-green">
                <div class="info">
                    <div class="label">اقدام‌های انجام‌شده</div>
                    <span class="num"><asp:Literal ID="litActionDone" runat="server" /></span>
                    <span class="trend">از <asp:Literal ID="litActionTotal" runat="server" /> اقدام</span>
                </div>
                <div class="ico"><i class="ti ti-circle-check"></i></div>
            </div>

            <div class="stat c-amber">
                <div class="info">
                    <span class="label">اقدام‌های معوق</span>
                    <div class="num"><asp:Literal ID="litActionOverdue" runat="server" /></div>
                </div>
                <div class="ico"><i class="ti ti-alert-triangle"></i></div>
            </div>
        </div>

        <div class="panel mb">
            <div class="panel-head"><h2>توزیع وضعیت جلسات در بازه</h2></div>
            <div class="panel-body">
                <asp:Repeater ID="rptStatusBars" runat="server">
                    <ItemTemplate>
                        <div class="bar-row">
                            <span class="bar-label" title='<%#: Eval("Title") %>'><%#: Eval("Title") %></span>
                            <div class="bar-track"><div class="bar-fill" style='width:<%#: BarPercent(Container.DataItem) %>%'></div></div>
                            <span class="bar-num"><%#: Fa(Eval("Cnt")) %></span>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>

        <div class="panel mb">
            <div class="panel-head">
                <h2>مصوبات (<asp:Literal ID="litDecRowCount" runat="server" />)</h2>
                <asp:LinkButton ID="btnExportDecisions" runat="server" CssClass="btn no-print" CausesValidation="false" OnClick="btnExportDecisions_Click"><i class="ti ti-file-spreadsheet"></i> CSV</asp:LinkButton>
            </div>
            <div class="table-wrap">
                <asp:Repeater ID="rptDecisions" runat="server">
                    <HeaderTemplate>
                        <table class="table table-readable">
                            <thead><tr><th>ردیف</th><th>تاریخ</th><th>جلسه</th><th>موضوع</th><th>متن مصوبه</th><th>اقدام‌ها</th></tr></thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td><span class="row-num"><%#: Fa(Container.ItemIndex + 1) %></span></td>
                            <td><%#: FaDate(Eval("MeetingDate")) %></td>
                            <td><a href='<%#: ResolveUrl("~/Pages/MeetingView.aspx?id=" + Eval("MeetingId")) %>' class="no-print-link"><%#: Eval("MeetingTitle") %></a></td>
                            <td><%#: Eval("TopicTitle") %></td>
                            <td><%#: Eval("DecisionText") %></td>
                            <td><%#: Fa(Eval("DoneCount")) %> / <%#: Fa(Eval("ActionCount")) %></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate></tbody></table></FooterTemplate>
                </asp:Repeater>
                <asp:Panel ID="pnlNoDecisions" runat="server" Visible="false" CssClass="empty"><i class="ti ti-gavel"></i>مصوبه‌ای با این فیلتر یافت نشد.</asp:Panel>
            </div>
        </div>

        <div class="panel">
            <div class="panel-head">
                <h2>اقدام‌ها (<asp:Literal ID="litActRowCount" runat="server" />)</h2>
                <asp:LinkButton ID="btnExportActions" runat="server" CssClass="btn no-print" CausesValidation="false" OnClick="btnExportActions_Click"><i class="ti ti-file-spreadsheet"></i> CSV</asp:LinkButton>
            </div>
            <div class="table-wrap">
                <asp:Repeater ID="rptActions" runat="server">
                    <HeaderTemplate>
                        <table class="table table-readable">
                            <thead><tr><th>ردیف</th><th>تاریخ جلسه</th><th>جلسه</th><th>شرح اقدام</th><th>مسئول</th><th>مهلت</th><th>وضعیت</th></tr></thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class='<%# (bool)Eval("IsOverdue") ? "row-overdue" : "" %>'>
                            <td><span class="row-num"><%#: Fa(Container.ItemIndex + 1) %></span></td>
                            <td><%#: FaDate(Eval("MeetingDate")) %></td>
                            <td><a href='<%#: ResolveUrl("~/Pages/MeetingView.aspx?id=" + Eval("MeetingId")) %>' class="no-print-link"><%#: Eval("MeetingTitle") %></a></td>
                            <td><%#: Eval("Title") %></td>
                            <td><%#: Eval("ResponsibleName") %></td>
                            <td><%#: Due(Container.DataItem) %></td>
                            <td><span class="badge <%#: Eval("CssClass") %>"><%#: Eval("StatusTitle") %></span></td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate></tbody></table></FooterTemplate>
                </asp:Repeater>
                <asp:Panel ID="pnlNoActions" runat="server" Visible="false" CssClass="empty"><i class="ti ti-checklist"></i>اقدامی با این فیلتر یافت نشد.</asp:Panel>
            </div>
        </div>
    </asp:Panel>

</asp:Content>
