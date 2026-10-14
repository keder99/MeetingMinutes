using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class _Default : BasePage
    {
        private static readonly string[] AvatarClasses = { "purple", "avatar-blue", "avatar-pink" };
        private bool _remindOverdue = true;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            if (Request.QueryString["denied"] == "1")
                litError.Text = Alert("دسترسی به آن بخش برای نقش شما مجاز نیست.", "alert-error");
            else if (Request.QueryString["pwd"] == "1")
                litError.Text = Alert("رمز عبور با موفقیت تغییر کرد.", "alert-success");

            try
            {
                bool canSeeDrafts = CurrentUser.CanEditMinutes;

                _remindOverdue = SettingsService.OverdueReminderEnabled();

                var t = DashboardService.Stats(canSeeDrafts);
                litTotal.Text = Fa(t["TotalMeetings"]);
                litHeld.Text = Fa(t["HeldMeetings"]);
                litHeldOf.Text = Fa(t["TotalMeetings"]);
                litOpen.Text = Fa(t["OpenActions"]);
                litDecisions.Text = Fa(t["TotalDecisions"]);
                litDecisionsOf.Text = Fa(t["TotalMeetings"]);
                var overdue = (int)t["OverdueActions"];
                if (overdue > 0 && _remindOverdue)
                    litOverdue.Text = " · <span class=\"red-text\">" + Fa(overdue) + " مورد معوق</span>";

                var meetings = DashboardService.RecentMeetings(canSeeDrafts, 5);
                rptMeetings.DataSource = meetings;
                rptMeetings.DataBind();
                pnlNoMeetings.Visible = meetings.Rows.Count == 0;

                var decisions = DashboardService.RecentDecisions(canSeeDrafts, 5);
                rptDecisions.DataSource = decisions;
                rptDecisions.DataBind();
                pnlNoDecisions.Visible = decisions.Rows.Count == 0;

                var actions = DashboardService.PendingActions(canSeeDrafts, 5);
                rptActions.DataSource = actions;
                rptActions.DataBind();
                pnlNoActions.Visible = actions.Rows.Count == 0;

                var statuses = DashboardService.MeetingStatusCounts(canSeeDrafts);
                int max = statuses.Rows.Count == 0 ? 1 : Math.Max(1, statuses.AsEnumerable().Max(r => Convert.ToInt32(r["Cnt"])));
                statuses.Columns.Add("MaxCnt", typeof(int));
                foreach (DataRow r in statuses.Rows) r["MaxCnt"] = max;
                rptStatusBars.DataSource = statuses;
                rptStatusBars.DataBind();
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                litError.Text += Alert("خواندن آمار از دیتابیس ممکن نشد. اسکریپت دیتابیس را بررسی کنید.", "alert-error");
            }
        }

        protected static string Fa(object v)
        {
            return TextNormalizer.ToPersianDigits(v is DBNull ? null : v);
        }

        protected static string FaDate(object v)
        {
            return v is DateTime ? PersianDate.ToDisplay((DateTime)v, false) : string.Empty;
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

        protected static string DecisionsLabel(object count)
        {
            var n = Convert.ToInt32(count);
            return n > 0 ? Fa(n) + " بند مصوب" : "بدون بند مصوب";
        }

        protected static string Shorten(object v)
        {
            var s = (v as string ?? string.Empty).Trim();
            return s.Length > 70 ? s.Substring(0, 70) + "…" : s;
        }

        protected static string Due(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            if (row["DueDate"] is DateTime) return PersianDate.ToDisplay((DateTime)row["DueDate"], false);
            if (!(row["DueText"] is DBNull) && !string.IsNullOrWhiteSpace((string)row["DueText"])) return (string)row["DueText"];
            return "—";
        }

        protected bool ShowOverdue(object dataItem)
        {
            return _remindOverdue && (bool)((DataRowView)dataItem).Row["IsOverdue"];
        }

        protected static int BarPercent(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            int max = (int)row["MaxCnt"], cnt = Convert.ToInt32(row["Cnt"]);
            return max == 0 ? 0 : (int)Math.Round(cnt * 100.0 / max);
        }
    }
}
