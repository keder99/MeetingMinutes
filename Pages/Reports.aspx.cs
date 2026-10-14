using System;
using System.Data;
using System.Text;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class Reports : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            txtFrom.Attributes["readonly"] = "readonly";
            txtTo.Attributes["readonly"] = "readonly";
            if (IsPostBack) return;

            Fill(ddlType, ReportService.Types(), "MeetingTypeId", "Title", "همه‌ی انواع");
            Fill(ddlPerson, ReportService.People(), "PersonId", "FullName", "همه‌ی افراد");
            Fill(ddlUnit, ReportService.Units(), "OrgUnitId", "Title", "همه‌ی واحدها");

            // پیش‌فرض: سه ماه گذشته تا امروز
            var today = DateTime.Today;
            txtTo.Text = PersianDate.ToDisplay(today, false);
            txtFrom.Text = PersianDate.ToDisplay(today.AddDays(-90), false);

            RunReport();
        }

        private static void Fill(DropDownList ddl, DataTable dt, string valueField, string textField, string emptyText)
        {
            ddl.DataSource = dt;
            ddl.DataValueField = valueField;
            ddl.DataTextField = textField;
            ddl.DataBind();
            ddl.Items.Insert(0, new ListItem(emptyText, ""));
        }

        protected void btnApply_Click(object sender, EventArgs e) { RunReport(); }

        private bool TryBuildFilter(out ReportFilter filter)
        {
            filter = null;
            litFilterError.Text = string.Empty;

            DateTime from, to;
            if (!PersianDate.TryParse(txtFrom.Text, out from)) { FilterError("تاریخ «از» را از تقویم انتخاب کنید."); return false; }
            if (!PersianDate.TryParse(txtTo.Text, out to)) { FilterError("تاریخ «تا» را از تقویم انتخاب کنید."); return false; }
            if (from > to) { FilterError("تاریخ «از» نمی‌تواند بعد از «تا» باشد."); return false; }

            filter = new ReportFilter
            {
                DateFrom = from,
                DateTo = to,
                TypeId = NullableInt(ddlType.SelectedValue),
                PersonId = NullableInt(ddlPerson.SelectedValue),
                UnitId = NullableInt(ddlUnit.SelectedValue),
                Query = txtQuery.Text,
                CanSeeDrafts = CurrentUser.CanEditMinutes
            };
            return true;
        }

        private ReportFilter _filter;

        private void RunReport()
        {
            if (!TryBuildFilter(out _filter))
            {
                pnlResults.Visible = false;
                return;
            }
            pnlResults.Visible = true;

            litRangeTitle.Text = "از " + PersianDate.ToDisplay(_filter.DateFrom, false) + " تا " + PersianDate.ToDisplay(_filter.DateTo, false);

            var s = ReportService.Summary(_filter);
            litMeetingCount.Text = Fa(s["MeetingCount"]);
            litDecisionCount.Text = Fa(s["DecisionCount"]);
            litActionTotal.Text = Fa(s["ActionCount"]);
            litActionDone.Text = Fa(s["ActionDoneCount"]);
            litActionOverdue.Text = Fa(s["ActionOverdueCount"]);

            rptStatusBars.DataSource = ReportService.MeetingStatusBars(_filter);
            rptStatusBars.DataBind();

            var decisions = ReportService.Decisions(_filter);
            litDecRowCount.Text = Fa(decisions.Rows.Count);
            rptDecisions.DataSource = decisions;
            rptDecisions.DataBind();
            rptDecisions.Visible = decisions.Rows.Count > 0;
            pnlNoDecisions.Visible = decisions.Rows.Count == 0;

            var actions = ReportService.Actions(_filter);
            litActRowCount.Text = Fa(actions.Rows.Count);
            rptActions.DataSource = actions;
            rptActions.DataBind();
            rptActions.Visible = actions.Rows.Count > 0;
            pnlNoActions.Visible = actions.Rows.Count == 0;
        }

        protected void btnExportDecisions_Click(object sender, EventArgs e)
        {
            ReportFilter f;
            if (!TryBuildFilter(out f)) { RunReport(); return; }
            DownloadCsv(ReportService.DecisionsToCsv(ReportService.Decisions(f)), "decisions-report.csv");
        }

        protected void btnExportActions_Click(object sender, EventArgs e)
        {
            ReportFilter f;
            if (!TryBuildFilter(out f)) { RunReport(); return; }
            DownloadCsv(ReportService.ActionsToCsv(ReportService.Actions(f)), "actions-report.csv");
        }

        private void DownloadCsv(string csv, string fileName)
        {
            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            Response.BinaryWrite(new byte[] { 0xEF, 0xBB, 0xBF });
            Response.BinaryWrite(Encoding.UTF8.GetBytes(csv));
            Response.End();
        }

        private void FilterError(string text)
        {
            litFilterError.Text = "<div class=\"filter-err\" style=\"display:block\">" + System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }

        private static int? NullableInt(string s)
        {
            return string.IsNullOrEmpty(s) ? (int?)null : int.Parse(s);
        }

        protected static string Fa(object v)
        {
            return TextNormalizer.ToPersianDigits(v is DBNull ? null : v);
        }

        protected static string FaDate(object v)
        {
            return v is DateTime ? PersianDate.ToDisplay((DateTime)v, false) : string.Empty;
        }

        protected static string Due(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            if (row["DueDate"] is DateTime) return PersianDate.ToDisplay((DateTime)row["DueDate"], false);
            if (!(row["DueText"] is DBNull) && !string.IsNullOrWhiteSpace((string)row["DueText"])) return (string)row["DueText"];
            return "—";
        }

        protected static int BarPercent(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            int cnt = Convert.ToInt32(row["Cnt"]);
            var table = row.Table;
            int max = 1;
            foreach (DataRow r in table.Rows) max = Math.Max(max, Convert.ToInt32(r["Cnt"]));
            return (int)Math.Round(cnt * 100.0 / max);
        }
    }
}
