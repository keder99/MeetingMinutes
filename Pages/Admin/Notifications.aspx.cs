using System;
using System.Data;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class Notifications : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin);
            txtStart.Attributes["readonly"] = "readonly";
            txtEnd.Attributes["readonly"] = "readonly";
            if (IsPostBack) return;
            BindList();
        }

        private void BindList()
        {
            var dt = NotificationService.AdminList();
            rptList.DataSource = dt;
            rptList.DataBind();
            rptList.Visible = dt.Rows.Count > 0;
            pnlEmpty.Visible = dt.Rows.Count == 0;
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            litMessage.Text = string.Empty;
            ShowEdit(null);
        }

        private void ShowEdit(int? id)
        {
            pnlEdit.Visible = true;
            hfId.Value = id.HasValue ? id.Value.ToString() : string.Empty;

            if (!id.HasValue)
            {
                litEditTitle.Text = "اعلان جدید";
                txtTitle.Text = txtBody.Text = txtStart.Text = txtEnd.Text = string.Empty;
                return;
            }

            var r = NotificationService.Get(id.Value);
            if (r == null)
            {
                pnlEdit.Visible = false;
                litMessage.Text = Alert("اعلان یافت نشد.", "alert-error");
                return;
            }

            litEditTitle.Text = "ویرایش اعلان";
            txtTitle.Text = r["Title"] is DBNull ? string.Empty : (string)r["Title"];
            txtBody.Text = (string)r["Body"];
            txtStart.Text = r["StartsAt"] is DateTime ? PersianDate.ToDisplay((DateTime)r["StartsAt"], false) : string.Empty;
            // پایان در دیتابیس آخرِ همان روز است (۲۳:۵۹:۵۹)؛ برای نمایش، تاریخِ همان روز کافی است
            txtEnd.Text = r["EndsAt"] is DateTime ? PersianDate.ToDisplay((DateTime)r["EndsAt"], false) : string.Empty;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            DateTime? start = null, end = null;
            if (!string.IsNullOrWhiteSpace(txtStart.Text))
            {
                DateTime d;
                if (!PersianDate.TryParse(txtStart.Text, out d)) { litMessage.Text = Alert("تاریخ شروع معتبر نیست.", "alert-error"); return; }
                start = d.Date;
            }
            if (!string.IsNullOrWhiteSpace(txtEnd.Text))
            {
                DateTime d;
                if (!PersianDate.TryParse(txtEnd.Text, out d)) { litMessage.Text = Alert("تاریخ پایان معتبر نیست.", "alert-error"); return; }
                end = d.Date.AddDays(1).AddSeconds(-1);   // تا پایانِ همان روز نمایش داده شود
            }

            int? id = string.IsNullOrEmpty(hfId.Value) ? (int?)null : int.Parse(hfId.Value);
            int savedId;
            string error;
            if (!NotificationService.Save(id, txtTitle.Text, txtBody.Text, start, end, CurrentUser.UserId, out savedId, out error))
            {
                litMessage.Text = Alert(error, "alert-error");
                return;
            }

            pnlEdit.Visible = false;
            BindList();
            litMessage.Text = Alert("اعلان ذخیره شد.", "alert-success");
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlEdit.Visible = false;
        }

        protected void rptList_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
        {
            var id = int.Parse((string)e.CommandArgument);
            if (e.CommandName == "edit")
            {
                litMessage.Text = string.Empty;
                ShowEdit(id);
            }
            else if (e.CommandName == "delete")
            {
                NotificationService.Delete(id, CurrentUser.UserId);
                litMessage.Text = Alert("اعلان حذف شد.", "alert-success");
                pnlEdit.Visible = false;
                BindList();
            }
        }

        // ---------- کمکی‌های قالب ----------
        protected static string Fa(object v)
        {
            return TextNormalizer.ToPersianDigits(v);
        }

        protected static string NotifTitle(object title, object body)
        {
            if (!(title is DBNull) && !string.IsNullOrWhiteSpace((string)title)) return (string)title;
            var b = (string)body;
            return b.Length > 40 ? b.Substring(0, 40) + "…" : b;
        }

        protected static string StateText(object code)
        {
            switch (Convert.ToInt32(code))
            {
                case 0: return "زمان‌بندی‌شده";
                case 2: return "منقضی‌شده";
                default: return "فعال";
            }
        }

        protected static string StateClass(object code)
        {
            switch (Convert.ToInt32(code))
            {
                case 0: return "pending";
                case 2: return "red";
                default: return "done";
            }
        }

        protected static string RangeText(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            var from = row["StartsAt"] is DateTime ? PersianDate.ToDisplay((DateTime)row["StartsAt"], false) : "بلافاصله";
            var to = row["EndsAt"] is DateTime ? PersianDate.ToDisplay((DateTime)row["EndsAt"], false) : "بدون انقضا";
            return from + " تا " + to;
        }
    }
}
