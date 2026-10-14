<%@ Page Title="جلسات" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Meetings.aspx.cs" Inherits="MeetingMinutes.Meetings" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>جلسات</h1>
            <p>وضعیت انجام کار در پایان هر جلسه ثبت می‌شود</p>
        </div>
        <a runat="server" id="lnkNew" href="~/Pages/MeetingEdit.aspx" class="btn btn-primary"><i class="ti ti-plus"></i>ثبت جلسه جدید</a>
    </div>

    <asp:Literal ID="litMessage" runat="server" />

    <asp:Panel runat="server" DefaultButton="btnFilter" CssClass="panel mb">
        <div class="panel-body">
            <div class="filters">
                <div class="fgrp">
                    <label>عنوان یا شماره</label>
                    <asp:TextBox ID="txtQuery" runat="server" CssClass="title-input" placeholder="جستجو..." />
                </div>
                <div class="fgrp">
                    <label>نوع جلسه</label>
                    <asp:DropDownList ID="ddlType" runat="server" />
                </div>
                <div class="fgrp">
                    <label>وضعیت</label>
                    <asp:DropDownList ID="ddlStatus" runat="server" />
                </div>
                <div class="fgrp">
                    <label>از تاریخ (شمسی)</label>
                    <asp:TextBox ID="txtFrom" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <div class="fgrp">
                    <label>تا تاریخ (شمسی)</label>
                    <asp:TextBox ID="txtTo" runat="server" CssClass="date-input" placeholder="انتخاب تاریخ" data-jpicker="1" autocomplete="off" />
                </div>
                <asp:Button ID="btnFilter" runat="server" Text="اعمال فیلتر" CssClass="btn btn-primary" OnClick="btnFilter_Click" />
                <asp:Button ID="btnClear" runat="server" Text="پاک کردن" CssClass="btn" CausesValidation="false" OnClick="btnClear_Click" />
            </div>
            <asp:Literal ID="litFilterError" runat="server" />
        </div>
    </asp:Panel>

    <div class="summary-row">
        <div class="summary-card c-blue">
            <div class="summary-info">
                <span class="summary-label">کل جلسات</span>
                <span class="summary-value">
                    <asp:Literal ID="litTotal" runat="server" Text="۰" /></span>
            </div>
            <div class="summary-ico"><i class="ti ti-calendar-event"></i></div>
        </div>

        <div class="summary-card c-purple">
            <div class="summary-info">
                <span class="summary-label">تکمیل‌شده</span>
                <span class="summary-value">
                    <asp:Literal ID="litDone" runat="server" Text="۰" /></span>
            </div>
            <div class="summary-ico"><i class="ti ti-circle-check"></i></div>
        </div>

        <div class="summary-card c-amber">
            <div class="summary-info">
                <span class="summary-label">ناتمام</span>
                <span class="summary-value">
                    <asp:Literal ID="litOpen" runat="server" Text="۰" /></span>
            </div>
            <div class="summary-ico"><i class="ti ti-hourglass-high"></i></div>
        </div>
    </div>

    <asp:Repeater ID="rptMeetings" runat="server">
        <HeaderTemplate>
            <div class="cards-grid">
        </HeaderTemplate>
        <ItemTemplate>
            <div class="meeting-card">
                <div class="card-top">
                    <div class="avatar <%# AvatarClass(Container.ItemIndex) %>"><%#: Initial(Eval("DisplayTitle")) %></div>
                    <div class="card-title-group">
                        <p class="card-title" title='<%#: Eval("DisplayTitle") %>'><%#: Eval("DisplayTitle") %></p>
                        <p class="card-date"><%#: FaDate(Eval("MeetingDate")) %><%#: FaNo(Eval("MeetingNo")) %></p>
                    </div>
                </div>
                <div class="progress-row"><span>میزان تکمیل صورت‌جلسه</span><span class="progress-value"><%#: Fa(ProgressOf(Container.DataItem)) %>٪</span></div>
                <div class="progress-track">
                    <div class="progress-fill" style='width: <%# ProgressOf(Container.DataItem) %>%'></div>
                </div>
                <div class="card-footer">
                    <span class="task-count"><i class="ti ti-checklist"></i><%#: DecisionsLabel(Eval("DecisionCount")) %></span>
                    <span class="badge <%#: Eval("CssClass") %>"><%#: Eval("StatusTitle") %></span>
                </div>
                <a class="mini" style="margin-top: 10px; text-align: center; text-decoration: none"
                    href='<%#: ResolveUrl("~/Pages/MeetingView.aspx?id=" + Eval("MeetingId")) %>'>مشاهده جزئیات</a>
            </div>
        </ItemTemplate>
        <FooterTemplate></div></FooterTemplate>
    </asp:Repeater>

    <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty">
        <i class="ti ti-calendar-off"></i>جلسه‌ای با این فیلتر یافت نشد.
   
    </asp:Panel>

</asp:Content>
