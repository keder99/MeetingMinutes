<%@ Page Title="اقدامات" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Actions.aspx.cs" Inherits="MeetingMinutes.Actions" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>اقدامات</h1>
            <p>تمام اقدام‌های دارای مسئول و مهلت، استخراج‌شده از مصوبات</p>
        </div>
        <div class="actions">
            <asp:LinkButton ID="btnExport" runat="server" CssClass="btn" CausesValidation="false" OnClick="btnExport_Click"><i class="ti ti-file-spreadsheet"></i> خروجی CSV</asp:LinkButton>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel runat="server" DefaultButton="btnFilter" CssClass="panel mb">
        <div class="panel-body">
            <div class="filters">
                <div class="fgrp">
                    <label>جست‌وجو</label>
                    <asp:TextBox ID="txtQuery" runat="server" CssClass="title-input" placeholder="عنوان، مصوبه، مسئول..." />
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
                    <label>وضعیت</label>
                    <asp:DropDownList ID="ddlStatus" runat="server" />
                </div>
                <div class="fgrp">
                    <label>مهلت از (شمسی)</label>
                    <asp:TextBox ID="txtDueFrom" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="fgrp">
                    <label>مهلت تا (شمسی)</label>
                    <asp:TextBox ID="txtDueTo" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="fgrp">
                    <asp:CheckBox ID="chkOnlyMine" runat="server" Text="فقط اقدام‌های من" />
                </div>
                <div class="fgrp">
                    <asp:CheckBox ID="chkOnlyOpen" runat="server" Text="فقط باز" /></div>
                <div class="fgrp">
                    <asp:CheckBox ID="chkOnlyOverdue" runat="server" Text="فقط معوق" /></div>
                <asp:Button ID="btnFilter" runat="server" Text="اعمال فیلتر" CssClass="btn btn-primary" OnClick="btnFilter_Click" />
                <asp:Button ID="btnClear" runat="server" Text="پاک کردن" CssClass="btn" CausesValidation="false" OnClick="btnClear_Click" />
            </div>
            <asp:Literal ID="litFilterError" runat="server" />
        </div>
    </asp:Panel>

    <div class="summary-row">
        <div class="summary-card c-blue">
            <div class="summary-info">
                <span class="summary-label">کل اقدام‌ها</span>
                <span class="summary-value">
                    <asp:Literal ID="litTotal" runat="server" Text="۰" /></span>
            </div>
            <div class="summary-ico"><i class="ti ti-list-details"></i></div>
        </div>

        <div class="summary-card c-amber">
            <div class="summary-info">
                <span class="summary-label">باز</span>
                <span class="summary-value">
                    <asp:Literal ID="litOpenCount" runat="server" Text="۰" /></span>
            </div>
            <div class="summary-ico"><i class="ti ti-hourglass-high"></i></div>
        </div>

        <div class="summary-card c-red">
            <div class="summary-info">
                <span class="summary-label">معوق</span>
                <span class="summary-value">
                    <asp:Literal ID="litOverdue" runat="server" Text="۰" /></span>
            </div>
            <div class="summary-ico"><i class="ti ti-alert-triangle"></i></div>
        </div>

        <div class="summary-card c-green">
            <div class="summary-info">
                <span class="summary-label">انجام‌شده</span>
                <span class="summary-value">
                    <asp:Literal ID="litDone" runat="server" Text="۰" /></span>
            </div>
            <div class="summary-ico"><i class="ti ti-circle-check"></i></div>
        </div>
    </div>

    <div class="panel">
        <div class="table-wrap">
            <asp:Repeater ID="rptActions" runat="server">
                <HeaderTemplate>
                    <table class="table table-readable">
                        <thead>
                            <tr>
                                <th style="width: 5%;">ردیف</th>
                                <th style="width: 40%;">عنوان اقدام</th>
                                <th>مسئول</th>
                                <th>مهلت</th>
                                <th>جلسه</th>
                                <th>وضعیت</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr class='<%# ShowOverdue(Container.DataItem) ? "row-overdue" : "" %>'>
                        <td><span class="row-num"><%#: Fa(Container.ItemIndex + 1) %></span></td>
                        <td title='<%#: Eval("Title") %>'><b><%#: Eval("Title") %></b></td>
                        <td><%#: Responsible(Container.DataItem) %></td>
                        <td><%#: Due(Container.DataItem) %><%# ShowOverdue(Container.DataItem) ? " <span class='badge red'>معوق</span>" : "" %></td>
                        <td><%#: Eval("MeetingTitle") %></td>
                        <td><span class="badge <%#: Eval("CssClass") %>"><%#: Eval("StatusTitle") %></span></td>
                        <td><a href='<%#: ResolveUrl("~/Pages/MeetingView.aspx?id=" + Eval("MeetingId")) %>' class="mini icon-only" title="مشاهده جلسه"><i class="ti ti-eye"></i></a>
                            <a href='<%#: ResolveUrl("~/Pages/ActionView.aspx?id=" + Eval("ActionId")) %>' class="mini icon-only" title="جزئیات و پیگیری"><i class="ti ti-list-check"></i></a></td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </tbody>
                    </table>
               
                </FooterTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty">
                <i class="ti ti-checklist"></i>اقدامی با این فیلتر یافت نشد.
           
            </asp:Panel>
        </div>
    </div>

</asp:Content>
