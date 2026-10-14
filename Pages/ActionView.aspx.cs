using System;
using System.Data;
using System.Web;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class ActionView : BasePage
    {
        private int _id;
        private DataRow _row;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!int.TryParse(Request.QueryString["id"], out _id)) { GoList(); return; }
            _row = ActionService.Get(_id, CurrentUser.CanEditMinutes);
            if (_row == null) { GoList(); return; }

            RenderHeader();
            RenderDecision();
            BindFollowUps();

            bool canFollow = ActionService.CanFollow(CurrentUser.CanEditMinutes, ActionService.PersonOfUser(CurrentUser.UserId), _row["ResponsiblePersonId"]);
            bool meetingLocked = Convert.ToInt32(_row["MeetingStatusId"]) == MeetingStatusIds.Final;
            pnlFollowForm.Visible = canFollow;
            pnlNoPermission.Visible = !canFollow;

            if (!IsPostBack && canFollow) LoadForm();
        }

        private void GoList()
        {
            Response.Redirect("~/Pages/Actions.aspx", true);
        }

        private void RenderHeader()
        {
            litTitle.Text = (string)_row["Title"];
            var parts = new System.Collections.Generic.List<string> { ActionService.ResponsibleText(_row), "مهلت: " + ActionService.DueText(_row) };
            litSub.Text = string.Join(" · ", parts);
            litStatusBadge.Text = "<span class=\"badge " + HttpUtility.HtmlEncode((string)_row["CssClass"]) + "\">" + HttpUtility.HtmlEncode((string)_row["StatusTitle"]) + "</span>";
            lnkMeeting.HRef = "~/Pages/MeetingView.aspx?id=" + _row["MeetingId"];
        }

        private void RenderDecision()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<p class=\"topic-text\">").Append(Multiline((string)_row["DecisionText"])).Append("</p>");
            sb.Append("<p class=\"sub-hint\" style=\"margin-top:10px\">از جلسه‌ی «").Append(HttpUtility.HtmlEncode((string)_row["MeetingTitle"]))
              .Append("» — ").Append(PersianDate.ToDisplay((DateTime)_row["MeetingDate"], false)).Append("</p>");
            litDecision.Text = sb.ToString();
        }

        private void BindFollowUps()
        {
            var dt = ActionService.FollowUps(_id);
            rptFollow.DataSource = dt;
            rptFollow.DataBind();
            pnlNoFollow.Visible = dt.Rows.Count == 0;
        }

        private void LoadForm()
        {
            ddlNewStatus.Items.Clear();
            foreach (DataRow r in ActionService.Statuses().Rows)
                ddlNewStatus.Items.Add(new System.Web.UI.WebControls.ListItem((string)r["Title"], r["StatusId"].ToString()));
            ddlNewStatus.SelectedValue = _row["StatusId"].ToString();

            ddlReviewMeeting.Items.Clear();
            ddlReviewMeeting.Items.Add(new System.Web.UI.WebControls.ListItem("— انتخاب نشده —", ""));
            foreach (DataRow m in ActionService.RecentMeetings(CurrentUser.CanEditMinutes, 15).Rows)
                ddlReviewMeeting.Items.Add(new System.Web.UI.WebControls.ListItem(
                    TextNormalizer.ToPersianDigits((string)m["MeetingDateFa"]) + " — " + (string)m["Title"], m["MeetingId"].ToString()));
        }

        protected void btnAddFollow_Click(object sender, EventArgs e)
        {
            int? reviewMeetingId = string.IsNullOrEmpty(ddlReviewMeeting.SelectedValue) ? (int?)null : int.Parse(ddlReviewMeeting.SelectedValue);
            string error;
            if (ActionService.AddFollowUp(_id, int.Parse(ddlNewStatus.SelectedValue), txtComment.Text, reviewMeetingId,
                                          CurrentUser.UserId, CurrentUser.CanEditMinutes, CurrentUser.CanEditMinutes, out error))
            {
                Response.Redirect("~/Pages/ActionView.aspx?id=" + _id + "&saved=1", true);
            }
            else
            {
                litMessage.Text = Alert(error, "alert-error");
            }
        }

        protected static string FaDate(object v)
        {
            return v is DateTime ? PersianDate.ToDisplay((DateTime)v, false) : string.Empty;
        }

        protected static string Multiline(object v)
        {
            var s = v as string;
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return HttpUtility.HtmlEncode(s.Replace("\r\n", "\n").Replace("\r", "\n")).Replace("\n", "<br />");
        }

        protected static string MeetingRef(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            if (row["MeetingId"] is DBNull) return string.Empty;
            var no = row["MeetingNo"] is DBNull ? string.Empty : TextNormalizer.ToPersianDigits((string)row["MeetingNo"]);
            return " · بررسی در جلسه‌ی " + FaDate(row["MeetingDate"]) + (no.Length > 0 ? " (" + no + ")" : string.Empty);
        }
    }
}
