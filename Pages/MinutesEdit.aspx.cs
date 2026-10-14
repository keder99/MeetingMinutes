using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class MinutesEdit : BasePage
    {
        private int _meetingId;
        private DataRow _meeting;
        private DataTable _decisions, _actions;

        // ======================================================================
        // بارگذاری
        // ======================================================================
        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin, RoleCodes.Secretary);

            if (!int.TryParse(Request.QueryString["id"], out _meetingId)) { Response.Redirect("~/Pages/Meetings.aspx", true); return; }
            _meeting = MeetingService.Get(_meetingId);
            if (_meeting == null) { Response.Redirect("~/Pages/Meetings.aspx", true); return; }
            if ((bool)_meeting["IsLocked"]) { Response.Redirect("~/Pages/MeetingView.aspx?id=" + _meetingId + "&locked=1", true); return; }

            txtDueDate.Attributes["readonly"] = "readonly";
            RenderHeader();
            if (!IsPostBack)
                txtSummary.Text = _meeting["ChairSummary"] is DBNull ? string.Empty : (string)_meeting["ChairSummary"];

            BindTree();   // در هر درخواست (بدون ViewState) تا رویدادهای دکمه‌های تودرتو درست مسیریابی شوند
        }

        private void RenderHeader()
        {
            var title = _meeting["Title"] is DBNull || string.IsNullOrWhiteSpace((string)_meeting["Title"]) ? (string)_meeting["TypeTitle"] : (string)_meeting["Title"];
            litCrumb.Text = title;
            lnkCrumb.HRef = "~/Pages/MeetingView.aspx?id=" + _meetingId;
            lnkView.HRef = "~/Pages/MeetingView.aspx?id=" + _meetingId;
            lnkInfo.HRef = "~/Pages/MeetingEdit.aspx?id=" + _meetingId;
            litSub.Text = TextNormalizer.ToPersianDigits(_meeting["MeetingNo"] is DBNull ? string.Empty : (string)_meeting["MeetingNo"])
                          + " · " + PersianDate.ToDisplay((DateTime)_meeting["MeetingDate"], false);
        }

        private void BindTree()
        {
            _decisions = MinutesService.Decisions(_meetingId);
            _actions = MinutesService.Actions(_meetingId);
            bool hasLoose = _decisions.Select("TopicId IS NULL").Length > 0;
            var topics = MinutesService.TopicsFlat(_meetingId, hasLoose);
            rptTopics.DataSource = topics;
            rptTopics.DataBind();
            pnlEmpty.Visible = topics.Rows.Count == 0;
        }

        protected void rptTopics_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;
            var row = ((DataRowView)e.Item.DataItem).Row;
            var view = new DataView(_decisions)
            {
                RowFilter = (bool)row["IsLoose"] ? "TopicId IS NULL" : "TopicId = " + (int)row["TopicId"],
                Sort = "SortOrder, DecisionId"
            };
            var rpt = (Repeater)e.Item.FindControl("rptDecisions");
            rpt.DataSource = view;
            rpt.DataBind();
        }

        protected void rptDecisions_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;
            var drv = (DataRowView)e.Item.DataItem;
            var view = new DataView(_actions) { RowFilter = "DecisionId = " + (int)drv["DecisionId"], Sort = "ActionId" };
            var rpt = (Repeater)e.Item.FindControl("rptActions");
            rpt.DataSource = view;
            rpt.DataBind();
        }

        // ======================================================================
        // دستورهای دکمه‌های فهرست (موضوع/مصوبه/اقدام)
        // ======================================================================
        protected void OnCmd(object source, RepeaterCommandEventArgs e)
        {
            int id = int.Parse((string)e.CommandArgument);
            string error = null, done = null;
            bool ok = true;
            var uid = CurrentUser.UserId;

            switch (e.CommandName)
            {
                // ---- بازکردن ویرایشگر ----
                case "topic-edit": ShowTopicEditor(id); return;
                case "dec-add": ShowDecisionEditor(null, id == 0 ? (int?)null : id); return;
                case "dec-edit": ShowDecisionEditor(id, null); return;
                case "act-add": ShowActionEditor(null, id); return;
                case "act-edit": ShowActionEditor(id, null); return;

                // ---- عملیات مستقیم ----
                case "topic-up": ok = MinutesService.MoveTopic(_meetingId, id, -1, out error); break;
                case "topic-down": ok = MinutesService.MoveTopic(_meetingId, id, 1, out error); break;
                case "topic-del":
                    ok = MinutesService.DeleteTopic(_meetingId, id, out error);
                    if (ok) { Audit.Write(uid, "Delete", "Topic", id, "حذف موضوع"); done = "موضوع حذف شد."; }
                    break;
                case "dec-up": ok = MinutesService.MoveDecision(_meetingId, id, -1, out error); break;
                case "dec-down": ok = MinutesService.MoveDecision(_meetingId, id, 1, out error); break;
                case "dec-del":
                    ok = MinutesService.DeleteDecision(_meetingId, id, out error);
                    if (ok) { Audit.Write(uid, "Delete", "Decision", id, "حذف مصوبه"); done = "مصوبه حذف شد."; }
                    break;
                case "dec-to-text":
                    ok = MinutesService.DecisionToDiscussion(_meetingId, id, out error);
                    if (ok) { Audit.Write(uid, "Update", "Decision", id, "تبدیل مصوبه به شرح مذاکرات"); done = "مصوبه به شرح مذاکرات منتقل شد."; }
                    break;
                case "act-del":
                    ok = MinutesService.DeleteAction(_meetingId, id, out error);
                    if (ok) { Audit.Write(uid, "Delete", "Action", id, "حذف اقدام"); done = "اقدام حذف شد."; }
                    break;
                case "act-done":
                    ok = MinutesService.MarkActionDone(_meetingId, id, uid, out error);
                    if (ok) { Audit.Write(uid, "Update", "Action", id, "علامت‌گذاری «انجام‌شده»"); done = "اقدام «انجام‌شده» شد."; }
                    break;
            }

            litMessage.Text = ok ? (done == null ? string.Empty : Alert(done, "alert-success")) : Alert(error, "alert-error");
            BindTree();   // داده تغییر کرده است
        }

        // ======================================================================
        // ویرایشگر
        // ======================================================================
        private void OpenEditor(string mode, int? id, int ctx, string title)
        {
            pnlEditor.Visible = true;
            hfMode.Value = mode;
            hfId.Value = id.HasValue ? id.Value.ToString() : string.Empty;
            hfCtx.Value = ctx.ToString();
            litEditorTitle.Text = title;
            pnlTopicFields.Visible = mode == "topic";
            pnlDecisionFields.Visible = mode == "decision";
            pnlActionFields.Visible = mode == "action";
            litMessage.Text = string.Empty;
        }

        private void CloseEditor()
        {
            pnlEditor.Visible = false;
        }

        protected void btnAddTopic_Click(object sender, EventArgs e) { ShowTopicEditor(null); }
        protected void btnAddDecision_Click(object sender, EventArgs e) { ShowDecisionEditor(null, null); }
        protected void btnEditorCancel_Click(object sender, EventArgs e) { CloseEditor(); }

        // ---- موضوع ----
        private void ShowTopicEditor(int? topicId)
        {
            DataRow r = null;
            if (topicId.HasValue)
            {
                r = MinutesService.GetTopic(_meetingId, topicId.Value);
                if (r == null) { litMessage.Text = Alert("موضوع یافت نشد.", "alert-error"); return; }
            }
            OpenEditor("topic", topicId, 0, topicId.HasValue ? "ویرایش موضوع" : "موضوع جدید");

            ddlTopicParent.DataSource = MinutesService.TopLevelTopics(_meetingId, topicId);
            ddlTopicParent.DataValueField = "TopicId";
            ddlTopicParent.DataTextField = "Title";
            ddlTopicParent.DataBind();
            ddlTopicParent.Items.Insert(0, new ListItem("— موضوع سطح اول —", ""));

            txtTopicTitle.Text = r == null ? string.Empty : (string)r["Title"];
            txtTopicText.Text = r == null || r["DiscussionText"] is DBNull ? string.Empty : (string)r["DiscussionText"];
            SelectValue(ddlTopicParent, r == null || r["ParentTopicId"] is DBNull ? string.Empty : r["ParentTopicId"].ToString());

            bool hasChildren = topicId.HasValue && MinutesService.TopicHasChildren(topicId.Value);
            ddlTopicParent.Enabled = !hasChildren;
            pnlParentHint.Visible = hasChildren;
        }

        // ---- مصوبه ----
        private void ShowDecisionEditor(int? decisionId, int? topicId)
        {
            DataRow r = null;
            if (decisionId.HasValue)
            {
                r = MinutesService.GetDecision(_meetingId, decisionId.Value);
                if (r == null) { litMessage.Text = Alert("مصوبه یافت نشد.", "alert-error"); return; }
                topicId = r["TopicId"] is DBNull ? (int?)null : (int)r["TopicId"];
            }
            OpenEditor("decision", decisionId, 0, decisionId.HasValue ? "ویرایش مصوبه" : "مصوبه جدید");

            ddlDecisionTopic.Items.Clear();
            ddlDecisionTopic.Items.Add(new ListItem("— بدون موضوع —", ""));
            foreach (DataRow t in MinutesService.TopicsFlat(_meetingId, false).Rows)
                ddlDecisionTopic.Items.Add(new ListItem(TextNormalizer.ToPersianDigits(t["Number"]) + ". " + (string)t["Title"], t["TopicId"].ToString()));

            SelectValue(ddlDecisionTopic, topicId.HasValue ? topicId.Value.ToString() : string.Empty);
            txtDecisionText.Text = r == null ? string.Empty : (string)r["DecisionText"];
        }

        // ---- اقدام ----
        private void ShowActionEditor(int? actionId, int? decisionId)
        {
            DataRow a = null;
            if (actionId.HasValue)
            {
                a = MinutesService.GetAction(_meetingId, actionId.Value);
                if (a == null) { litMessage.Text = Alert("اقدام یافت نشد.", "alert-error"); return; }
                decisionId = (int)a["DecisionId"];
            }
            var dec = MinutesService.GetDecision(_meetingId, decisionId.Value);
            if (dec == null) { litMessage.Text = Alert("مصوبه یافت نشد.", "alert-error"); return; }

            OpenEditor("action", actionId, decisionId.Value, actionId.HasValue ? "ویرایش اقدام" : "اقدام جدید");

            int? personId = a == null || a["ResponsiblePersonId"] is DBNull ? (int?)null : (int)a["ResponsiblePersonId"];
            int? unitId = a == null || a["ResponsibleUnitId"] is DBNull ? (int?)null : (int)a["ResponsibleUnitId"];
            hfKeepPerson.Value = personId.HasValue ? personId.Value.ToString() : string.Empty;

            ddlPerson.DataSource = MinutesService.PeopleForPicker(personId);
            ddlPerson.DataValueField = "PersonId";
            ddlPerson.DataTextField = "FullName";
            ddlPerson.DataBind();
            ddlPerson.Items.Insert(0, new ListItem("— بدون فرد —", ""));

            ddlUnit.DataSource = OrgUnitService.Options(unitId, null);
            ddlUnit.DataValueField = "OrgUnitId";
            ddlUnit.DataTextField = "Title";
            ddlUnit.DataBind();
            ddlUnit.Items.Insert(0, new ListItem("— بدون واحد —", ""));

            ddlActionStatus.DataSource = MinutesService.ActionStatuses();
            ddlActionStatus.DataValueField = "StatusId";
            ddlActionStatus.DataTextField = "Title";
            ddlActionStatus.DataBind();

            if (a == null)
            {
                txtActionTitle.Text = Shorten((string)dec["DecisionText"], 160);
                SelectValue(ddlPerson, string.Empty);
                SelectValue(ddlUnit, string.Empty);
                txtNote.Text = txtDueDate.Text = txtDueText.Text = string.Empty;
                SelectValue(ddlActionStatus, "1");
            }
            else
            {
                txtActionTitle.Text = (string)a["Title"];
                SelectValue(ddlPerson, personId.HasValue ? personId.Value.ToString() : string.Empty);
                SelectValue(ddlUnit, unitId.HasValue ? unitId.Value.ToString() : string.Empty);
                txtNote.Text = a["ResponsibleNote"] is DBNull ? string.Empty : (string)a["ResponsibleNote"];
                txtDueDate.Text = a["DueDate"] is DateTime ? PersianDate.ToDisplay((DateTime)a["DueDate"], false) : string.Empty;
                txtDueText.Text = a["DueText"] is DBNull ? string.Empty : (string)a["DueText"];
                SelectValue(ddlActionStatus, Convert.ToInt32(a["StatusId"]).ToString());
            }
        }

        // ---- ذخیره ----
        protected void btnEditorSave_Click(object sender, EventArgs e)
        {
            string mode = hfMode.Value, error = null;
            int? id = string.IsNullOrEmpty(hfId.Value) ? (int?)null : int.Parse(hfId.Value);
            int savedId = id ?? 0;
            bool ok = false;
            var uid = CurrentUser.UserId;

            if (mode == "topic")
            {
                ok = MinutesService.SaveTopic(_meetingId, id, txtTopicTitle.Text, txtTopicText.Text, NullableInt(ddlTopicParent.SelectedValue), out savedId, out error);
                if (ok) Audit.Write(uid, id.HasValue ? "Update" : "Create", "Topic", savedId, txtTopicTitle.Text.Trim());
            }
            else if (mode == "decision")
            {
                ok = MinutesService.SaveDecision(_meetingId, id, NullableInt(ddlDecisionTopic.SelectedValue), txtDecisionText.Text, out savedId, out error);
                if (ok) Audit.Write(uid, id.HasValue ? "Update" : "Create", "Decision", savedId, "مصوبه");
            }
            else if (mode == "action")
            {
                DateTime? due = null;
                if (!string.IsNullOrWhiteSpace(txtDueDate.Text))
                {
                    DateTime d;
                    if (!PersianDate.TryParse(txtDueDate.Text, out d))
                    {
                        litMessage.Text = Alert("تاریخ مهلت معتبر نیست؛ آن را از تقویم انتخاب کنید.", "alert-error");
                        return;
                    }
                    due = d;
                }
                ok = MinutesService.SaveAction(_meetingId, id, int.Parse(hfCtx.Value), txtActionTitle.Text,
                        NullableInt(ddlPerson.SelectedValue), NullableInt(ddlUnit.SelectedValue), txtNote.Text,
                        due, txtDueText.Text, int.Parse(ddlActionStatus.SelectedValue), uid, out error);
                if (ok) Audit.Write(uid, id.HasValue ? "Update" : "Create", "Action", id ?? 0, "اقدام");
            }

            if (!ok)
            {
                litMessage.Text = Alert(error, "alert-error");   // ویرایشگر باز می‌ماند
                return;
            }
            CloseEditor();
            litMessage.Text = Alert("ذخیره شد.", "alert-success");
            BindTree();
        }

        protected void btnSaveSummary_Click(object sender, EventArgs e)
        {
            string error;
            if (MinutesService.SaveChairSummary(_meetingId, txtSummary.Text, CurrentUser.UserId, out error))
            {
                Audit.Write(CurrentUser.UserId, "Update", "Meeting", _meetingId, "جمع‌بندی رئیس جلسه");
                litMessage.Text = Alert("جمع‌بندی ذخیره شد.", "alert-success");
            }
            else
            {
                litMessage.Text = Alert(error, "alert-error");
            }
        }

        // ======================================================================
        // اسکریپت‌های سمت مرورگر: جست‌وجوی مسئول و اسکرول به ویرایشگر
        // ======================================================================
        protected void Page_PreRender(object sender, EventArgs e)
        {
            if (!pnlEditor.Visible) return;

            var js = new StringBuilder();
            if (pnlActionFields.Visible)
            {
                int keep;
                var people = MinutesService.PeopleForPicker(int.TryParse(hfKeepPerson.Value, out keep) ? (int?)keep : null);
                var arr = people.Rows.Cast<DataRow>()
                                .Select(r => new { v = r["PersonId"].ToString(), t = (string)r["FullName"], s = (string)r["SearchText"] })
                                .ToArray();
                js.Append("if (window.mmInitPeople) { mmInitPeople('mmPersonSearch','").Append(ddlPerson.ClientID).Append("',")
                  .Append(new JavaScriptSerializer().Serialize(arr)).Append("); }");
            }
            js.Append("var el = document.getElementById('").Append(pnlEditor.ClientID).Append("'); if (el) { el.scrollIntoView({ block: 'start' }); }");
            ClientScript.RegisterStartupScript(GetType(), "mmEditor", js.ToString(), true);
        }

        // ======================================================================
        // کمکی‌های قالب و فرم
        // ======================================================================
        protected static string Fa(object v)
        {
            return TextNormalizer.ToPersianDigits(v is DBNull ? null : v);
        }

        protected static string TopicNumber(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            return (bool)row["IsLoose"] ? string.Empty : Fa(row["Number"]) + ". ";
        }

        protected static string Multiline(object v)
        {
            var s = v as string;
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return HttpUtility.HtmlEncode(s.Replace("\r\n", "\n").Replace("\r", "\n")).Replace("\n", "<br />");
        }

        protected static string ActionResponsible(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            var parts = new[] { row["PersonName"], row["UnitTitle"], row["ResponsibleNote"] }
                .Where(x => !(x is DBNull) && !string.IsNullOrWhiteSpace((string)x)).Select(x => (string)x);
            var s = string.Join(" / ", parts);
            return s.Length == 0 ? "—" : s;
        }

        protected static string ActionDue(object dataItem)
        {
            var row = ((DataRowView)dataItem).Row;
            if (row["DueDate"] is DateTime) return PersianDate.ToDisplay((DateTime)row["DueDate"], false);
            if (!(row["DueText"] is DBNull) && !string.IsNullOrWhiteSpace((string)row["DueText"])) return (string)row["DueText"];
            return "—";
        }

        protected static bool NotDone(object dataItem)
        {
            return Convert.ToInt32(((DataRowView)dataItem).Row["StatusId"]) != 3;
        }

        private static int? NullableInt(string s)
        {
            return string.IsNullOrEmpty(s) ? (int?)null : int.Parse(s);
        }

        private static void SelectValue(DropDownList ddl, string value)
        {
            var item = ddl.Items.FindByValue(value);
            ddl.SelectedIndex = -1;
            if (item != null) item.Selected = true;
            else if (ddl.Items.Count > 0) ddl.SelectedIndex = 0;
        }

        private static string Shorten(string s, int max)
        {
            var t = System.Text.RegularExpressions.Regex.Replace(s ?? string.Empty, @"\s+", " ").Trim();
            if (t.Length <= max) return t;
            var cut = t.Substring(0, max);
            var sp = cut.LastIndexOf(' ');
            return (sp > 0 ? cut.Substring(0, sp) : cut).TrimEnd(' ', '،', '.') + "…";
        }
    }
}
