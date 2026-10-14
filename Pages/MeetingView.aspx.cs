using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class MeetingView : BasePage
    {
        private int _id;
        protected bool CanManageAttachments;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!int.TryParse(Request.QueryString["id"], out _id)) { GoList(); return; }

            var r = MeetingService.Get(_id);
            if (r == null) { GoList(); return; }

            bool canEdit = CurrentUser.CanEditMinutes;
            int statusId = Convert.ToInt32(r["StatusId"]);
            bool locked = (bool)r["IsLocked"];

            // پیش‌نویس فقط برای دبیر و مدیر
            if (statusId == MeetingStatusIds.Draft && !canEdit)
            {
                Response.Redirect("~/Pages/Default.aspx?denied=1", true);
                return;
            }

            if (!IsPostBack) ShowFlashMessage();

            RenderHeader(r, statusId, locked, canEdit);
            RenderBody(r);
            RenderAttendees();

            CanManageAttachments = canEdit;
            BindAttachments();
        }

        private void GoList()
        {
            Response.Redirect("~/Pages/Meetings.aspx", true);
        }

        private void ShowFlashMessage()
        {
            switch (Request.QueryString["msg"])
            {
                case "finalized": litMessage.Text = Alert("جلسه نهایی و قفل شد.", "alert-success"); break;
                case "reopened": litMessage.Text = Alert("جلسه بازگشایی شد و به وضعیت «برگزار شده» برگشت.", "alert-success"); break;
            }
            if (Request.QueryString["locked"] == "1")
                litMessage.Text = Alert("این جلسه نهایی و قفل شده است؛ برای ویرایش، مدیر باید آن را بازگشایی کند.", "alert-info");
        }

        // ---------- سرصفحه و دکمه‌ها ----------
        private void RenderHeader(DataRow r, int statusId, bool locked, bool canEdit)
        {
            var display = Display(r);
            litCrumb.Text = display;
            litTitle.Text = display;

            var parts = new System.Collections.Generic.List<string>();
            if (!(r["MeetingNo"] is DBNull)) parts.Add(Fa((string)r["MeetingNo"]));
            parts.Add(PersianDate.ToDisplay((DateTime)r["MeetingDate"], false));
            var time = TimeText(r);
            if (time.Length > 0) parts.Add(time);
            litSub.Text = string.Join(" · ", parts);

            litStatusBadge.Text = "<span class=\"badge " + E(Str(r["CssClass"])) + "\">" + E((string)r["StatusTitle"]) + "</span>";

            lnkEdit.Visible = canEdit && !locked;
            lnkEdit.HRef = "~/Pages/MeetingEdit.aspx?id=" + _id;
            lnkMinutes.Visible = canEdit && !locked;
            lnkMinutes.HRef = "~/Pages/MinutesEdit.aspx?id=" + _id;
            lnkWord.HRef = "~/Pages/MeetingWordExport.ashx?id=" + _id;
            btnFinalize.Visible = canEdit && !locked && statusId != MeetingStatusIds.Cancelled;
            btnReopen.Visible = CurrentUser.IsAdmin && locked;
            btnDelete.Visible = CurrentUser.IsAdmin && !locked;

            // پیش‌نویس بودن متن صورت‌جلسه تا زمان امضا؛ فقط «نهایی» و «لغو شده» نشان خاص دارند
            string text = "پیش‌نویس", cls = string.Empty;
            if (statusId == MeetingStatusIds.Final) { text = "نهایی"; cls = "final"; }
            else if (statusId == MeetingStatusIds.Cancelled) { text = "لغو شده"; cls = "cancelled"; }
            litWatermark.Text = "<div class=\"print-watermark " + cls + "\">" + E(text) + "</div>";
        }

        private static string Display(DataRow r)
        {
            return r["Title"] is DBNull || string.IsNullOrWhiteSpace((string)r["Title"]) ? (string)r["TypeTitle"] : (string)r["Title"];
        }

        private static string TimeText(DataRow r)
        {
            var st = r["StartTime"] is TimeSpan ? (TimeSpan?)r["StartTime"] : null;
            var et = r["EndTime"] is TimeSpan ? (TimeSpan?)r["EndTime"] : null;
            Func<TimeSpan, string> f = t => Fa(t.ToString(@"hh\:mm"));
            if (st.HasValue && et.HasValue) return f(st.Value) + " تا " + f(et.Value);
            if (st.HasValue) return "از " + f(st.Value);
            return string.Empty;
        }

        // ---------- بدنه‌ی صورت‌جلسه ----------
        private void RenderBody(DataRow r)
        {
            var sb = new StringBuilder();

            sb.Append("<div class=\"info-grid\">");
            Info(sb, "تاریخ", PersianDate.ToDisplay((DateTime)r["MeetingDate"], false));
            var time = TimeText(r);
            Info(sb, "ساعت", time.Length > 0 ? time : "—");
            Info(sb, "محل", r["LocationTitle"] is DBNull ? "—" : (string)r["LocationTitle"]);
            Info(sb, "رئیس جلسه", r["ChairName"] is DBNull ? "—" : (string)r["ChairName"]);
            Info(sb, "نوع جلسه", (string)r["TypeTitle"]);
            Info(sb, "شماره‌ی نامه", r["LetterNo"] is DBNull ? "—" : Fa((string)r["LetterNo"]));
            sb.Append("</div>");

            if (!(r["Preamble"] is DBNull) && !string.IsNullOrWhiteSpace((string)r["Preamble"]))
            {
                sb.Append("<div class=\"section\"><h3 class=\"section-title\">شرح آغاز جلسه</h3><div class=\"topic\"><p class=\"topic-text\">")
                  .Append(Multiline((string)r["Preamble"])).Append("</p></div></div>");
            }

            var agenda = MeetingService.Agenda(_id);
            if (agenda.Rows.Count > 0)
            {
                sb.Append("<div class=\"section\"><h3 class=\"section-title\">دستور جلسه</h3>");
                int i = 0;
                foreach (DataRow a in agenda.Rows)
                    sb.Append("<div class=\"topic\"><b>").Append(Fa(++i)).Append(". ").Append(E((string)a["Title"])).Append("</b></div>");
                sb.Append("</div>");
            }

            RenderTopics(sb);

            if (!(r["ChairSummary"] is DBNull) && !string.IsNullOrWhiteSpace((string)r["ChairSummary"]))
            {
                sb.Append("<div class=\"section\"><h3 class=\"section-title\">جمع‌بندی رئیس جلسه</h3><div class=\"topic\"><p class=\"topic-text\">")
                  .Append(Multiline((string)r["ChairSummary"])).Append("</p></div></div>");
            }

            litMain.Text = sb.ToString();
        }

        private void RenderTopics(StringBuilder sb)
        {
            var topics = MeetingService.Topics(_id);
            var decisions = MeetingService.Decisions(_id);
            var actions = MeetingService.Actions(_id);

            sb.Append("<div class=\"section\"><h3 class=\"section-title\">مشروح مذاکرات، مصوبات و اقدامات</h3>");

            if (topics.Rows.Count == 0 && decisions.Rows.Count == 0)
            {
                sb.Append("<p class=\"sub-hint\">هنوز مشروح مذاکرات و مصوبه‌ای ثبت نشده است.</p></div>");
                return;
            }

            // موضوعات سطح اول و زیرموضوع‌ها (شماره‌گذاری مثل ۳-۱)
            int n = 0;
            foreach (DataRow t in topics.Select("ParentTopicId IS NULL", "SortOrder, TopicId"))
            {
                n++;
                AppendTopic(sb, t, Fa(n), decisions, actions);
                int k = 0;
                foreach (DataRow c in topics.Select("ParentTopicId = " + (int)t["TopicId"], "SortOrder, TopicId"))
                    AppendTopic(sb, c, Fa(n) + "-" + Fa(++k), decisions, actions);
            }

            // مصوباتی که به هیچ موضوعی وصل نیستند
            var loose = decisions.Select("TopicId IS NULL", "SortOrder, DecisionId");
            if (loose.Length > 0)
            {
                sb.Append("<div class=\"topic\"><p class=\"topic-title\"><b>مصوبات جلسه</b></p>");
                foreach (var d in loose) AppendDecision(sb, d, actions);
                sb.Append("</div>");
            }
            sb.Append("</div>");
        }

        private static void AppendTopic(StringBuilder sb, DataRow t, string number, DataTable decisions, DataTable actions)
        {
            sb.Append("<div class=\"topic\"><p class=\"topic-title\"><b>").Append(number).Append(". ").Append(E((string)t["Title"])).Append("</b></p>");
            if (!(t["DiscussionText"] is DBNull) && !string.IsNullOrWhiteSpace((string)t["DiscussionText"]))
                sb.Append("<p class=\"topic-text\">").Append(Multiline((string)t["DiscussionText"])).Append("</p>");
            foreach (var d in decisions.Select("TopicId = " + (int)t["TopicId"], "SortOrder, DecisionId"))
                AppendDecision(sb, d, actions);
            sb.Append("</div>");
        }

        private static void AppendDecision(StringBuilder sb, DataRow d, DataTable actions)
        {
            sb.Append("<div class=\"decision\"><p>").Append(Multiline((string)d["DecisionText"])).Append("</p>");
            foreach (var a in actions.Select("DecisionId = " + (int)d["DecisionId"], "ActionId"))
            {
                string resp = a["Responsible"] is DBNull ? "—" : (string)a["Responsible"];
                string due = a["DueDate"] is DateTime ? PersianDate.ToDisplay((DateTime)a["DueDate"], false)
                           : (a["DueText"] is DBNull ? "—" : (string)a["DueText"]);
                sb.Append("<div class=\"action-chip\"><i class=\"ti ti-user-circle\"></i><b>").Append(E(resp)).Append("</b><span>·</span>مهلت: ")
                  .Append(E(due)).Append("<span>·</span><span class=\"badge ").Append(E(Str(a["CssClass"]))).Append("\">")
                  .Append(E((string)a["StatusTitle"])).Append("</span></div>");
            }
            sb.Append("</div>");
        }

        // ---------- حاضران و دبیر ----------
        private void RenderAttendees()
        {
            var att = MeetingService.Attendees(_id);
            litAttCount.Text = Fa(att.Rows.Count) + " نفر";

            var sb = new StringBuilder();
            foreach (DataRow a in att.Rows)
            {
                int s = Convert.ToInt32(a["AttendanceStatusId"]);
                string avatarClass = s == 1 ? "purple" : "pink";
                string badge = s == 1 ? "done" : (s == 3 ? "pending" : "red");
                string name = (string)a["FullName"];
                string position = a["Position"] is DBNull ? string.Empty : (string)a["Position"];

                sb.Append("<div class=\"mrow\">")
                  .Append(AvatarHtml.Render(IntOrNull(a["PersonId"]), IntOr0(a["PhotoVersion"]), Initial(name), avatarClass))
                  .Append("<div class=\"grow\"><b>").Append(E(name)).Append("</b>")
                  .Append("<small style=\"display:block;color:var(--muted);font-size:10px\">").Append(E(position)).Append("</small></div>")
                  .Append("<span class=\"badge ").Append(badge).Append("\">").Append(E((string)a["StatusTitle"])).Append("</span></div>");
            }
            litAttendees.Text = att.Rows.Count == 0 ? "<p class=\"sub-hint\">حاضرانی ثبت نشده است.</p>" : sb.ToString();

            var m = MeetingService.Get(_id);
            if (m["SecretaryName"] is DBNull)
            {
                litSecretary.Text = "<p class=\"sub-hint\">دبیری تعیین نشده است.</p>";
            }
            else
            {
                var sec = (string)m["SecretaryName"];
                litSecretary.Text = "<div class=\"mrow\">"
                    + AvatarHtml.Render(IntOrNull(m["SecretaryPersonId"]), IntOr0(m["SecretaryPhotoVersion"]), Initial(sec), "pink")
                    + "<div class=\"grow\"><b>" + E(sec) + "</b></div></div>";
            }
        }

        private static int? IntOrNull(object v)
        {
            return v is DBNull || v == null ? (int?)null : Convert.ToInt32(v);
        }

        private static int IntOr0(object v)
        {
            return v is DBNull || v == null ? 0 : Convert.ToInt32(v);
        }

        // ---------- عملیات ----------
        protected void btnFinalize_Click(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin, RoleCodes.Secretary);
            string error;
            if (MeetingService.Finalize(_id, CurrentUser.UserId, out error))
                Response.Redirect("~/Pages/MeetingView.aspx?id=" + _id + "&msg=finalized", true);
            litMessage.Text = Alert(error, "alert-error");
        }

        protected void btnReopen_Click(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin);
            string error;
            if (MeetingService.Reopen(_id, CurrentUser.UserId, out error))
                Response.Redirect("~/Pages/MeetingView.aspx?id=" + _id + "&msg=reopened", true);
            litMessage.Text = Alert(error, "alert-error");
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin);
            string error;
            if (MeetingService.Delete(_id, CurrentUser.UserId, out error))
                Response.Redirect("~/Pages/Meetings.aspx?deleted=1", true);
            litMessage.Text = Alert(error, "alert-error");
        }

        // ---------- کمکی ----------
        private static void Info(StringBuilder sb, string label, string value)
        {
            sb.Append("<div class=\"info\"><label>").Append(E(label)).Append("</label><b>").Append(E(value)).Append("</b></div>");
        }

        private static string Str(object v)
        {
            return v is DBNull || v == null ? string.Empty : (string)v;
        }

        private static string E(string s)
        {
            return HttpUtility.HtmlEncode(s);
        }

        private static string Multiline(string s)
        {
            return E(s.Replace("\r\n", "\n").Replace("\r", "\n")).Replace("\n", "<br />");
        }

        private static string Fa(object v)
        {
            return TextNormalizer.ToPersianDigits(v);
        }

        // ---------- ضمائم ----------
        private void BindAttachments()
        {
            var dt = AttachmentService.List(_id);
            rptAttachments.DataSource = dt;
            rptAttachments.DataBind();
            pnlNoAttachments.Visible = dt.Rows.Count == 0;

            pnlUpload.Visible = CanManageAttachments;
            if (CanManageAttachments) litAllowedExt.Text = AttachmentService.AllowedExtensionsText;
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (!CanManageAttachments) return;
            string error;
            if (AttachmentService.Upload(_id, int.Parse(ddlKind.SelectedValue), fuAttachment.HasFile ? fuAttachment.PostedFile : null, CurrentUser.UserId, out error))
                litMessage.Text = Alert("ضمیمه بارگذاری شد.", "alert-success");
            else
                litMessage.Text = Alert(error, "alert-error");
            BindAttachments();
        }

        protected void rptAttachments_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "delete" || !CanManageAttachments) return;
            string error;
            if (AttachmentService.Delete(int.Parse((string)e.CommandArgument), CurrentUser.UserId, out error))
                litMessage.Text = Alert("ضمیمه حذف شد.", "alert-success");
            else
                litMessage.Text = Alert(error, "alert-error");
            BindAttachments();
        }

        protected static string FaDate(object v)
        {
            return v is DateTime ? PersianDate.ToDisplay((DateTime)v, true) : string.Empty;
        }

        protected static string KindTitle(object kind)
        {
            return AttachmentKinds.Title(Convert.ToInt32(kind));
        }

        protected static string SizeText(object bytes)
        {
            return AttachmentService.FormatSize(bytes);
        }

        protected static string FileIcon(object contentType)
        {
            var ct = contentType as string ?? string.Empty;
            if (ct.StartsWith("image/")) return "ti-photo";
            if (ct == "application/pdf") return "ti-file-type-pdf";
            return "ti-file-type-doc";
        }

        private static string Initial(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "؟";
            var words = name.Trim().Split(' ');
            return words[words.Length - 1].Substring(0, 1);
        }
    }
}
