using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class MeetingEdit : BasePage
    {
        private int MeetingId
        {
            get { int id; return int.TryParse(hfMeetingId.Value, out id) ? id : 0; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin, RoleCodes.Secretary);
            txtDate.Attributes["readonly"] = "readonly";
            if (IsPostBack) return;

            LoadLookups();
            int id;
            if (int.TryParse(Request.QueryString["id"], out id)) LoadMeeting(id);
            else NewMeeting();

            if (Request.QueryString["saved"] == "1")
                litMessage.Text = Alert("جلسه ثبت شد. اکنون حاضران و دستور جلسه را تکمیل کنید.", "alert-success");
        }

        //  بارگذاری
        private void LoadLookups()
        {
            Fill(ddlType, MeetingService.Types(), "MeetingTypeId", "Title", null);
            Fill(ddlLocation, MeetingService.Locations(), "LocationId", "Title", "— انتخاب محل —");
            var people = MeetingService.PeopleOptions();
            Fill(ddlChair, people, "PersonId", "FullName", "— انتخاب نشده —");
            Fill(ddlSecretary, people, "PersonId", "FullName", "— انتخاب نشده —");

            ddlStatus.Items.Clear();
            foreach (DataRow r in MeetingService.Statuses().Rows)
                if (!(bool)r["IsLocked"])
                    ddlStatus.Items.Add(new ListItem((string)r["Title"], r["StatusId"].ToString()));
        }

        private static void Fill(DropDownList ddl, DataTable dt, string valueField, string textField, string emptyText)
        {
            ddl.DataSource = dt;
            ddl.DataValueField = valueField;
            ddl.DataTextField = textField;
            ddl.DataBind();
            if (emptyText != null) ddl.Items.Insert(0, new ListItem(emptyText, ""));
        }

        private static void Select(DropDownList ddl, object value)
        {
            var v = value is DBNull || value == null ? string.Empty : Convert.ToInt32(value).ToString();
            var item = ddl.Items.FindByValue(v);
            ddl.SelectedIndex = -1;
            if (item != null) item.Selected = true;
        }

        private void NewMeeting()
        {
            litCrumb.Text = "ثبت جلسه جدید";
            litHeading.Text = "ثبت جلسه جدید";
            litSub.Text = "ابتدا اطلاعات اصلی را ثبت کنید؛ سپس حاضران و دستور جلسه را تکمیل کنید.";
            lnkView.Visible = false;
            lnkMinutes.Visible = false;

            hfMeetingId.Value = string.Empty;
            hfRowVer.Value = string.Empty;
            txtDate.Text = PersianDate.ToDisplay(DateTime.Today, false);
            ddlStatus.SelectedValue = MeetingStatusIds.Draft.ToString();
        }

        private void LoadMeeting(int id)
        {
            var r = MeetingService.Get(id);
            if (r == null)
            {
                Response.Redirect("~/Pages/Meetings.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if ((bool)r["IsLocked"])
            {
                Response.Redirect("~/Pages/MeetingView.aspx?id=" + id + "&locked=1", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            var display = r["Title"] is DBNull || string.IsNullOrWhiteSpace((string)r["Title"]) ? (string)r["TypeTitle"] : (string)r["Title"];
            litCrumb.Text = "ویرایش: " + display;
            litHeading.Text = "ویرایش جلسه";
            litSub.Text = TextNormalizer.ToPersianDigits(r["MeetingNo"] is DBNull ? "" : (string)r["MeetingNo"]) + " · " + display;
            lnkView.HRef = "~/Pages/MeetingView.aspx?id=" + id;
            lnkView.Visible = true;
            lnkMinutes.HRef = "~/Pages/MinutesEdit.aspx?id=" + id;
            lnkMinutes.Visible = true;

            hfMeetingId.Value = id.ToString();
            hfRowVer.Value = Convert.ToBase64String((byte[])r["RowVer"]);

            Select(ddlType, r["MeetingTypeId"]);
            txtLetterNo.Text = r["LetterNo"] is DBNull ? string.Empty : (string)r["LetterNo"];
            txtTitle.Text = r["Title"] is DBNull ? string.Empty : (string)r["Title"];
            txtDate.Text = PersianDate.ToDisplay((DateTime)r["MeetingDate"], false);
            Select(ddlStatus, r["StatusId"]);
            txtStart.Text = FormatTime(r["StartTime"]);
            txtEnd.Text = FormatTime(r["EndTime"]);
            Select(ddlLocation, r["LocationId"]);
            txtNewLocation.Text = string.Empty;
            Select(ddlChair, r["ChairPersonId"]);
            Select(ddlSecretary, r["SecretaryPersonId"]);
            txtPreamble.Text = r["Preamble"] is DBNull ? string.Empty : (string)r["Preamble"];

            pnlAttendees.Visible = true;
            pnlAgenda.Visible = true;
            BindAttendees();
            BindAgenda();
        }

        private static string FormatTime(object v)
        {
            return v is TimeSpan ? TextNormalizer.ToPersianDigits(((TimeSpan)v).ToString(@"hh\:mm")) : string.Empty;
        }

        // ---------- ذخیره‌ی اطلاعات جلسه ----------
        protected void btnSave_Click(object sender, EventArgs e)
        {
            DateTime date;
            if (!PersianDate.TryParse(txtDate.Text, out date))
            {
                litMessage.Text = Alert("تاریخ جلسه معتبر نیست. آن را از تقویم انتخاب کنید (نمونه: ۱۴۰۴/۰۷/۰۵).", "alert-error");
                return;
            }
            TimeSpan? start, end;
            if (!TryTime(txtStart.Text, out start)) { litMessage.Text = Alert("ساعت شروع معتبر نیست (نمونه: ۱۰:۰۰).", "alert-error"); return; }
            if (!TryTime(txtEnd.Text, out end)) { litMessage.Text = Alert("ساعت پایان معتبر نیست (نمونه: ۱۱:۴۵).", "alert-error"); return; }

            int? id = MeetingId > 0 ? MeetingId : (int?)null;
            var input = new MeetingInput
            {
                MeetingId = id,
                MeetingTypeId = int.Parse(ddlType.SelectedValue),
                LetterNo = txtLetterNo.Text,
                Title = txtTitle.Text,
                Date = date,
                StartTime = start,
                EndTime = end,
                LocationId = NullableInt(ddlLocation.SelectedValue),
                NewLocation = txtNewLocation.Text,
                ChairPersonId = NullableInt(ddlChair.SelectedValue),
                SecretaryPersonId = NullableInt(ddlSecretary.SelectedValue),
                StatusId = int.Parse(ddlStatus.SelectedValue),
                Preamble = txtPreamble.Text
            };
            byte[] rowVer = string.IsNullOrEmpty(hfRowVer.Value) ? null : Convert.FromBase64String(hfRowVer.Value);

            int savedId;
            string error;
            if (!MeetingService.Save(input, rowVer, CurrentUser.UserId, out savedId, out error))
            {
                litMessage.Text = Alert(error, "alert-error");
                return;
            }

            if (!id.HasValue)
            {
                Response.Redirect("~/Pages/MeetingEdit.aspx?id=" + savedId + "&saved=1", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            LoadLookups();          // ممکن است محل جدیدی ساخته شده باشد
            LoadMeeting(savedId);   // نسخه‌ی رکورد (RowVer) را هم به‌روز می‌کند
            litMessage.Text = Alert("تغییرات ذخیره شد.", "alert-success");
        }

        private static int? NullableInt(string s)
        {
            return string.IsNullOrEmpty(s) ? (int?)null : int.Parse(s);
        }

        private static bool TryTime(string text, out TimeSpan? value)
        {
            value = null;
            var t = TextNormalizer.Normalize(text);
            if (t.Length == 0) return true;
            var m = Regex.Match(t, @"^(\d{1,2})[:.](\d{2})$");
            if (!m.Success) return false;
            int h = int.Parse(m.Groups[1].Value), min = int.Parse(m.Groups[2].Value);
            if (h > 23 || min > 59) return false;
            value = new TimeSpan(h, min, 0);
            return true;
        }

        // ---------- حاضران ----------
        private void BindAttendees()
        {
            var dt = MeetingService.Attendees(MeetingId);
            rptAtt.DataSource = dt;
            rptAtt.DataBind();
            rptAtt.Visible = dt.Rows.Count > 0;
            pnlNoAtt.Visible = dt.Rows.Count == 0;

            Fill(ddlAddPerson, MeetingService.PeopleNotAttending(MeetingId), "PersonId", "FullName", "— انتخاب فرد —");
            chkAddGuest.Checked = false;
        }

        /// <summary>مقادیر فعلی ردیف‌های صفحه را (پیش از هر عملیات دیگر) ذخیره می‌کند تا با بازخوانی از بین نروند.</summary>
        private bool PersistRows(out string error)
        {
            var rows = new List<AttendanceRow>();
            foreach (RepeaterItem it in rptAtt.Items)
            {
                var hf = (HiddenField)it.FindControl("hfPid");
                var pos = (TextBox)it.FindControl("txtPos");
                var ddl = (DropDownList)it.FindControl("ddlAtt");
                rows.Add(new AttendanceRow { PersonId = int.Parse(hf.Value), StatusId = int.Parse(ddl.SelectedValue), Position = pos.Text });
            }
            return MeetingService.SaveAttendance(MeetingId, rows, out error);
        }

        protected void btnSaveAtt_Click(object sender, EventArgs e)
        {
            string error;
            if (PersistRows(out error)) litMessage.Text = Alert("حضور و غیاب ذخیره شد.", "alert-success");
            else litMessage.Text = Alert(error, "alert-error");
            BindAttendees();
        }

        protected void btnAddAttendee_Click(object sender, EventArgs e)
        {
            string error;
            if (string.IsNullOrEmpty(ddlAddPerson.SelectedValue))
            {
                litMessage.Text = Alert("ابتدا فرد مورد نظر را انتخاب کنید.", "alert-error");
                return;
            }
            if (!PersistRows(out error) || !MeetingService.AddAttendee(MeetingId, int.Parse(ddlAddPerson.SelectedValue), chkAddGuest.Checked, out error))
                litMessage.Text = Alert(error, "alert-error");
            else
                litMessage.Text = string.Empty;
            BindAttendees();
        }

        protected void btnAddBoard_Click(object sender, EventArgs e)
        {
            string error;
            int added = 0;
            if (!PersistRows(out error) || !MeetingService.AddAllBoardMembers(MeetingId, out added, out error))
                litMessage.Text = Alert(error, "alert-error");
            else
                litMessage.Text = Alert(added > 0 ? Fa(added) + " نفر از اعضای هیأت اضافه شدند." : "همه‌ی اعضای فعال هیأت قبلاً در فهرست هستند.", "alert-success");
            BindAttendees();
        }

        protected void rptAtt_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "remove") return;
            string error;
            if (!PersistRows(out error) || !MeetingService.RemoveAttendee(MeetingId, int.Parse((string)e.CommandArgument), out error))
                litMessage.Text = Alert(error, "alert-error");
            BindAttendees();
        }

        // ---------- دستور جلسه ----------
        private void BindAgenda()
        {
            var dt = MeetingService.Agenda(MeetingId);
            rptAgenda.DataSource = dt;
            rptAgenda.DataBind();
            rptAgenda.Visible = dt.Rows.Count > 0;
            pnlNoAgenda.Visible = dt.Rows.Count == 0;
        }

        protected void btnAddAgenda_Click(object sender, EventArgs e)
        {
            string error;
            if (MeetingService.AddAgendaItem(MeetingId, txtAgenda.Text, out error))
            {
                txtAgenda.Text = string.Empty;
                litMessage.Text = string.Empty;
            }
            else
            {
                litMessage.Text = Alert(error, "alert-error");
            }
            BindAgenda();
        }

        protected void rptAgenda_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var itemId = int.Parse((string)e.CommandArgument);
            string error = null;
            bool ok = true;
            switch (e.CommandName)
            {
                case "up": ok = MeetingService.MoveAgendaItem(MeetingId, itemId, -1, out error); break;
                case "down": ok = MeetingService.MoveAgendaItem(MeetingId, itemId, 1, out error); break;
                case "remove": ok = MeetingService.RemoveAgendaItem(MeetingId, itemId, out error); break;
            }
            if (!ok) litMessage.Text = Alert(error, "alert-error");
            BindAgenda();
        }

        protected static string Fa(object v)
        {
            return TextNormalizer.ToPersianDigits(v);
        }
    }
}
