using System;
using System.Data;
using System.Text;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class Actions : BasePage
    {
        private bool _remindOverdue = true;

        protected void Page_Load(object sender, EventArgs e)
        {
            _remindOverdue = SettingsService.OverdueReminderEnabled();
            txtDueFrom.Attributes["readonly"] = "readonly";
            txtDueTo.Attributes["readonly"] = "readonly";
            if (IsPostBack) return;

            Fill(ddlPerson, ActionService.ResponsiblePeople(), "PersonId", "FullName", "همه‌ی افراد");
            Fill(ddlUnit, ActionService.ResponsibleUnits(), "OrgUnitId", "Title", "همه‌ی واحدها");

            ddlStatus.Items.Clear();
            ddlStatus.Items.Add(new ListItem("همه‌ی وضعیت‌ها", ""));
            foreach (DataRow r in ActionService.Statuses().Rows)
                ddlStatus.Items.Add(new ListItem((string)r["Title"], r["StatusId"].ToString()));

            // از داشبورد یا صفحه‌ی فرد بیاییم
            int pid;
            if (int.TryParse(Request.QueryString["person"], out pid) && ddlPerson.Items.FindByValue(pid.ToString()) != null)
                ddlPerson.SelectedValue = pid.ToString();
            if (Request.QueryString["overdue"] == "1") chkOnlyOverdue.Checked = true;

            // عضو هیأت و مشاهده‌گر کار‌های خودشان‌اند؛ دبیر و مدیر پیش ‌فرض همه را می‌بینن
            chkOnlyMine.Checked = !CurrentUser.CanEditMinutes;

            BindList();
        }

        private static void Fill(DropDownList ddl, DataTable dt, string valueField, string textField, string emptyText)
        {
            ddl.DataSource = dt;
            ddl.DataValueField = valueField;
            ddl.DataTextField = textField;
            ddl.DataBind();
            ddl.Items.Insert(0, new ListItem(emptyText, ""));
        }

        protected void btnFilter_Click(object sender, EventArgs e) { BindList(); }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtQuery.Text = txtDueFrom.Text = txtDueTo.Text = string.Empty;
            ddlPerson.SelectedIndex = 0; ddlUnit.SelectedIndex = 0; ddlStatus.SelectedIndex = 0;
            chkOnlyMine.Checked = false; chkOnlyOpen.Checked = false; chkOnlyOverdue.Checked = false;
            BindList();
        }

        private bool TryBuildFilter(out ActionFilter filter)
        {
            filter = null;
            litFilterError.Text = string.Empty;
            DateTime? from = null, to = null;
            if (!string.IsNullOrWhiteSpace(txtDueFrom.Text))
            {
                DateTime d;
                if (!PersianDate.TryParse(txtDueFrom.Text, out d)) { FilterError("تاریخ «مهلت از» معتبر نیست."); return false; }
                from = d;
            }
            if (!string.IsNullOrWhiteSpace(txtDueTo.Text))
            {
                DateTime d;
                if (!PersianDate.TryParse(txtDueTo.Text, out d)) { FilterError("تاریخ «مهلت تا» معتبر نیست."); return false; }
                to = d;
            }
            if (from.HasValue && to.HasValue && from.Value > to.Value) { FilterError("تاریخ «از» نمی‌تواند بعد از «تا» باشد."); return false; }

            int? personId = NullableInt(ddlPerson.SelectedValue);
            if (chkOnlyMine.Checked)
            {
                var myPersonId = ActionService.PersonOfUser(CurrentUser.UserId);
                if (!myPersonId.HasValue)
                {
                    FilterError("برای استفاده از فیلتر «فقط اقدام‌های من»، ابتدا در صفحه‌ی «پروفایل من» اطلاعات خود را تکمیل کنید.");
                    return false;
                }
                personId = myPersonId;
            }

            filter = new ActionFilter
            {
                Query = txtQuery.Text,
                PersonId = personId,
                UnitId = NullableInt(ddlUnit.SelectedValue),
                StatusId = NullableInt(ddlStatus.SelectedValue),
                OnlyOpen = chkOnlyOpen.Checked,
                OnlyOverdue = chkOnlyOverdue.Checked,
                DueFrom = from,
                DueTo = to,
                CanSeeDrafts = CurrentUser.CanEditMinutes
            };
            return true;
        }

        private void BindList()
        {
            ActionFilter filter;
            if (!TryBuildFilter(out filter))
            {
                rptActions.Visible = false; pnlEmpty.Visible = false;
                litTotal.Text = litOpenCount.Text = litOverdue.Text = litDone.Text = "۰";
                return;
            }

            var dt = ActionService.Search(filter);
            int open = 0, overdue = 0, done = 0;
            foreach (DataRow r in dt.Rows)
            {
                if ((bool)r["IsOverdue"]) overdue++;
                if ((bool)r["IsClosed"]) done++; else open++;
            }
            litTotal.Text = Fa(dt.Rows.Count);
            litOpenCount.Text = Fa(open);
            litOverdue.Text = Fa(overdue);
            litDone.Text = Fa(done);

            rptActions.DataSource = dt;
            rptActions.DataBind();
            rptActions.Visible = dt.Rows.Count > 0;
            pnlEmpty.Visible = dt.Rows.Count == 0;
        }

        //excel output ai
        protected void btnExport_Click(object sender, EventArgs e)
        {
            ActionFilter filter;
            if (!TryBuildFilter(out filter)) { BindList(); return; }

            var csv = ActionService.ToCsv(ActionService.Search(filter));
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF };   // BOM برای باز شدن درست در Excel
            var body = Encoding.UTF8.GetBytes(csv);

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=actions.csv");
            Response.BinaryWrite(bytes);
            Response.BinaryWrite(body);
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
            return TextNormalizer.ToPersianDigits(v);
        }

        protected bool ShowOverdue(object dataItem)
        {
            return _remindOverdue && (bool)((DataRowView)dataItem).Row["IsOverdue"];
        }

        protected static string Responsible(object dataItem)
        {
            return ActionService.ResponsibleText(((DataRowView)dataItem).Row);
        }

        protected static string Due(object dataItem)
        {
            return ActionService.DueText(((DataRowView)dataItem).Row);
        }
    }
}
