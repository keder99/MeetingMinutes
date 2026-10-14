using System;
using System.Data;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class Meetings : BasePage
    {
        private static readonly string[] AvatarClasses = { "purple", "avatar-blue", "avatar-pink" };

        protected void Page_Load(object sender, EventArgs e)
        {
            // فیلد تاریخ فقط با تقویم پر می‌شود ولی مقدارش به سرور می‌رسد
            txtFrom.Attributes["readonly"] = "readonly";
            txtTo.Attributes["readonly"] = "readonly";

            if (IsPostBack) return;

            lnkNew.Visible = CurrentUser.CanEditMinutes;
            if (Request.QueryString["deleted"] == "1")
                litMessage.Text = Alert("جلسه حذف شد.", "alert-success");

            ddlType.DataSource = MeetingService.Types();
            ddlType.DataTextField = "Title";
            ddlType.DataValueField = "MeetingTypeId";
            ddlType.DataBind();
            ddlType.Items.Insert(0, new ListItem("همه‌ی انواع", ""));

            ddlStatus.DataSource = MeetingService.Statuses();
            ddlStatus.DataTextField = "Title";
            ddlStatus.DataValueField = "StatusId";
            ddlStatus.DataBind();
            ddlStatus.Items.Insert(0, new ListItem("همه وضعیت‌ها", ""));
            if (!CurrentUser.CanEditMinutes)   // پیش‌نویس فقط برای دبیر و مدیر
                ddlStatus.Items.Remove(ddlStatus.Items.FindByValue(MeetingStatusIds.Draft.ToString()));

            BindList();
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            BindList();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtQuery.Text = txtFrom.Text = txtTo.Text = string.Empty;
            ddlType.SelectedIndex = 0;
            ddlStatus.SelectedIndex = 0;
            BindList();
        }

        private void BindList()
        {
            litFilterError.Text = string.Empty;

            DateTime? from = null, to = null;
            if (!string.IsNullOrWhiteSpace(txtFrom.Text))
            {
                DateTime d;
                if (!PersianDate.TryParse(txtFrom.Text, out d)) { FilterError("تاریخ «از» معتبر نیست."); return; }
                from = d;
            }
            if (!string.IsNullOrWhiteSpace(txtTo.Text))
            {
                DateTime d;
                if (!PersianDate.TryParse(txtTo.Text, out d)) { FilterError("تاریخ «تا» معتبر نیست."); return; }
                to = d;
            }
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                FilterError("تاریخ «از» نمی‌تواند بعد از تاریخ «تا» باشد.");
                return;
            }

            var dt = MeetingService.Search(new MeetingFilter
            {
                Query = txtQuery.Text,
                TypeId = string.IsNullOrEmpty(ddlType.SelectedValue) ? (int?)null : int.Parse(ddlType.SelectedValue),
                StatusId = string.IsNullOrEmpty(ddlStatus.SelectedValue) ? (int?)null : int.Parse(ddlStatus.SelectedValue),
                From = from,
                To = to,
                CanSeeDrafts = CurrentUser.CanEditMinutes
            });

            int done = 0, open = 0;
            foreach (DataRow r in dt.Rows)
            {
                var s = Convert.ToInt32(r["StatusId"]);
                if (s == MeetingStatusIds.Held || s == MeetingStatusIds.Final) done++;
                else if (s == MeetingStatusIds.Draft || s == MeetingStatusIds.Pending) open++;
            }
            litTotal.Text = Fa(dt.Rows.Count);
            litDone.Text = Fa(done);
            litOpen.Text = Fa(open);

            rptMeetings.DataSource = dt;
            rptMeetings.DataBind();
            rptMeetings.Visible = dt.Rows.Count > 0;
            pnlEmpty.Visible = dt.Rows.Count == 0;
        }

        private void FilterError(string text)
        {
            litFilterError.Text = "<div class=\"filter-err\" style=\"display:block\">" + System.Web.HttpUtility.HtmlEncode(text) + "</div>";
            rptMeetings.Visible = false;
            pnlEmpty.Visible = false;
        }

        // ---------- کمکی‌های قالب ----------
        protected static string Fa(object v)
        {
            return TextNormalizer.ToPersianDigits(v is DBNull ? null : v);
        }

        protected static string FaDate(object v)
        {
            return v is DateTime ? PersianDate.ToDisplay((DateTime)v, false) : string.Empty;
        }

        protected static string FaNo(object v)
        {
            return v is DBNull || v == null ? string.Empty : " · " + Fa(v);
        }

        protected static string AvatarClass(int index)
        {
            return AvatarClasses[index % AvatarClasses.Length];
        }

        protected static string Initial(object title)
        {
            var s = title as string;
            return string.IsNullOrWhiteSpace(s) ? "؟" : s.Trim().Substring(0, 1);
        }

        protected static int ProgressOf(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            return MeetingService.Progress(Convert.ToInt32(row["StatusId"]), Convert.ToInt32(row["DecisionCount"]));
        }

        protected static string DecisionsLabel(object count)
        {
            var n = Convert.ToInt32(count);
            return n > 0 ? Fa(n) + " بند مصوب" : "بدون بند مصوب";
        }
    }
}
