using System;
using System.Data;
using System.Text;
using System.Web;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    /// <summary>
    /// خروجی Word (.doc) از صورت‌جلسه، برای زمانی که سازمان می‌خواهد سربرگ، امضا یا ویرایش نهایی
    /// را در خودِ Word انجام دهد. فایل واقعاً HTML است با فرمت ویژه‌ی Word (mso)؛ Word آن را مثل
    /// یک سند معمولی باز و ویرایش می‌کند. برای PDF، از دکمه‌ی «چاپ» در همین صفحه و گزینه‌ی
    /// «Save as PDF» چاپگر مرورگر استفاده کنید.
    /// همان قاعده‌ی دسترسیِ صفحه‌ی جزئیات جلسه اینجا هم اعمال می‌شود: پیش‌نویس فقط برای دبیر/مدیر.
    /// </summary>
    public class MeetingWordExport : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            int meetingId;
            if (!int.TryParse(context.Request.QueryString["id"], out meetingId)) { Fail(context, 400, "درخواست نامعتبر است."); return; }

            var user = context.User as AppPrincipal;
            if (user == null) { Fail(context, 401, "برای دریافت این فایل باید وارد شوید."); return; }

            var m = MeetingService.Get(meetingId);
            if (m == null) { Fail(context, 404, "جلسه یافت نشد."); return; }

            int statusId = Convert.ToInt32(m["StatusId"]);
            if (statusId == MeetingStatusIds.Draft && !user.CanEditMinutes)
            { Fail(context, 403, "دسترسی به این جلسه مجاز نیست."); return; }

            var html = Builder.Build(meetingId, m);

            var display = m["Title"] is DBNull || string.IsNullOrWhiteSpace((string)m["Title"]) ? (string)m["TypeTitle"] : (string)m["Title"];
            var fileTag = m["MeetingNo"] is DBNull ? meetingId.ToString() : ((string)m["MeetingNo"]).Replace("/", "-");
            var asciiName = "Minutes-" + fileTag + ".doc";
            var fullName = display + " (" + TextNormalizer.ToPersianDigits((string)m["MeetingDateFa"]) + ").doc";

            context.Response.Clear();
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "application/msword";
            context.Response.AddHeader("Content-Disposition",
                "attachment; filename=\"" + asciiName + "\"; filename*=UTF-8''" + Uri.EscapeDataString(fullName));
            context.Response.Write(html);
        }

        private static void Fail(HttpContext context, int statusCode, string message)
        {
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.Write(message);
        }

        /// <summary>ساخت خروجیِ HTML سازگار با Word (نشانه‌های mso برای اندازه‌ی صفحه و راست‌به‌چپ).</summary>
        private static class Builder
        {
            private const string FontStack = "'B Nazanin','IRANSans','Tahoma',Arial,sans-serif";

            public static string Build(int meetingId, DataRow m)
            {
                var sb = new StringBuilder();
                var orgName = SettingsService.OrgName();
                var display = m["Title"] is DBNull || string.IsNullOrWhiteSpace((string)m["Title"]) ? (string)m["TypeTitle"] : (string)m["Title"];

                sb.Append("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>");
                sb.Append("<head><meta charset='utf-8'><title>").Append(E(display)).Append("</title>");
                sb.Append("<!--[if gte mso 9]><xml><w:WordDocument><w:View>Print</w:View><w:Zoom>100</w:Zoom>" +
                           "<w:DoNotOptimizeForBrowser/></w:WordDocument></xml><![endif]-->");
                sb.Append("<style>")
                  .Append("@page Section1 {size:8.27in 11.69in; margin:2.5cm 2cm 2.5cm 2cm; mso-header-margin:1cm; mso-footer-margin:1cm;}")
                  .Append("div.Section1 {page:Section1;}")
                  .Append("body{font-family:").Append(FontStack).Append(";font-size:13pt;line-height:1.9;direction:rtl}")
                  .Append("h1{font-size:16pt;text-align:center;margin:0 0 4pt}")
                  .Append("h2{font-size:13pt;background:#efefef;padding:4pt 8pt;margin:16pt 0 6pt;border-right:4pt solid #666}")
                  .Append("table{border-collapse:collapse;width:100%;margin-bottom:10pt}")
                  .Append("td,th{border:1pt solid #999;padding:5pt 8pt;text-align:right;vertical-align:top;font-size:11pt}")
                  .Append("th{background:#f3f3f3}")
                  .Append(".center{text-align:center}.muted{color:#555;font-size:10pt}")
                  .Append(".topic-title{font-weight:bold;margin:10pt 0 3pt}.decision{margin:4pt 0 4pt 14pt}")
                  .Append(".sign-row{margin-top:40pt;width:100%}.sign-cell{width:50%;display:inline-block;text-align:center}")
                  .Append("</style></head><body lang='fa-ir' dir='rtl'><div class='Section1'>");

                // ---------- سربرگ ----------
                if (orgName.Length > 0) sb.Append("<p class='center' style='font-weight:bold;margin-bottom:2pt'>").Append(E(orgName)).Append("</p>");
                sb.Append("<h1>صورت‌جلسه ").Append(E((string)m["TypeTitle"])).Append("</h1>");
                if (!string.IsNullOrWhiteSpace(display) && display != (string)m["TypeTitle"])
                    sb.Append("<p class='center' style='font-weight:bold'>").Append(E(display)).Append("</p>");

                // ---------- مشخصات جلسه ----------
                sb.Append("<table><tr>")
                  .Append(Cell("تاریخ", PersianDate.ToDisplay((DateTime)m["MeetingDate"], false)))
                  .Append(Cell("ساعت", TimeText(m)))
                  .Append(Cell("شماره", m["MeetingNo"] is DBNull ? "—" : Fa((string)m["MeetingNo"])))
                  .Append("</tr><tr>")
                  .Append(Cell("محل برگزاری", m["LocationTitle"] is DBNull ? "—" : (string)m["LocationTitle"]))
                  .Append(Cell("رئیس جلسه", m["ChairName"] is DBNull ? "—" : (string)m["ChairName"]))
                  .Append(Cell("دبیر جلسه", m["SecretaryName"] is DBNull ? "—" : (string)m["SecretaryName"]))
                  .Append("</tr></table>");

                if (!(m["Preamble"] is DBNull) && !string.IsNullOrWhiteSpace((string)m["Preamble"]))
                {
                    sb.Append("<h2>شرح آغاز جلسه</h2><p>").Append(Multiline((string)m["Preamble"])).Append("</p>");
                }

                // ---------- حاضران و غایبان ----------
                var att = MeetingService.Attendees(meetingId);
                sb.Append("<h2>حاضران و غایبان</h2><table><tr><th>ردیف</th><th>نام</th><th>سمت</th><th>وضعیت</th></tr>");
                int n = 0;
                foreach (DataRow a in att.Rows)
                {
                    var pos = a["Position"] is DBNull ? string.Empty : (string)a["Position"];
                    if ((bool)a["IsGuest"]) pos = "مهمان" + (pos.Length > 0 ? " — " + pos : string.Empty);
                    sb.Append("<tr>").Append(Cell(Fa(++n))).Append(Cell((string)a["FullName"])).Append(Cell(pos.Length == 0 ? "—" : pos))
                      .Append(Cell((string)a["StatusTitle"])).Append("</tr>");
                }
                if (att.Rows.Count == 0) sb.Append("<tr><td colspan='4' class='muted center'>حاضری ثبت نشده است.</td></tr>");
                sb.Append("</table>");

                // ---------- دستور جلسه ----------
                var agenda = MeetingService.Agenda(meetingId);
                if (agenda.Rows.Count > 0)
                {
                    sb.Append("<h2>دستور جلسه</h2><ol>");
                    foreach (DataRow r in agenda.Rows) sb.Append("<li>").Append(E((string)r["Title"])).Append("</li>");
                    sb.Append("</ol>");
                }

                // ---------- مشروح، مصوبات و اقدام‌ها ----------
                AppendTopics(sb, meetingId);

                if (!(m["ChairSummary"] is DBNull) && !string.IsNullOrWhiteSpace((string)m["ChairSummary"]))
                {
                    sb.Append("<h2>جمع‌بندی رئیس جلسه</h2><p>").Append(Multiline((string)m["ChairSummary"])).Append("</p>");
                }

                // ---------- محل امضا ----------
                sb.Append("<table style='margin-top:30pt;border:none' cellspacing='0'><tr style='border:none'>")
                  .Append("<td style='border:none;text-align:center;width:50%'>امضای رئیس جلسه<br/><br/><br/>ـــــــــــــــــ<br/>")
                  .Append(m["ChairName"] is DBNull ? "" : E((string)m["ChairName"])).Append("</td>")
                  .Append("<td style='border:none;text-align:center;width:50%'>امضای دبیر جلسه<br/><br/><br/>ـــــــــــــــــ<br/>")
                  .Append(m["SecretaryName"] is DBNull ? "" : E((string)m["SecretaryName"])).Append("</td></tr></table>");

                sb.Append("</div></body></html>");
                return sb.ToString();
            }

            private static void AppendTopics(StringBuilder sb, int meetingId)
            {
                var topics = MeetingService.Topics(meetingId);
                var decisions = MeetingService.Decisions(meetingId);
                var actions = MeetingService.Actions(meetingId);
                if (topics.Rows.Count == 0 && decisions.Rows.Count == 0) return;

                sb.Append("<h2>مشروح مذاکرات، مصوبات و اقدامات</h2>");
                int n = 0;
                foreach (DataRow t in topics.Select("ParentTopicId IS NULL", "SortOrder, TopicId"))
                {
                    n++;
                    AppendTopic(sb, t, Fa(n), decisions, actions);
                    int k = 0;
                    foreach (DataRow c in topics.Select("ParentTopicId = " + (int)t["TopicId"], "SortOrder, TopicId"))
                        AppendTopic(sb, c, Fa(n) + "-" + Fa(++k), decisions, actions);
                }
                var loose = decisions.Select("TopicId IS NULL", "SortOrder, DecisionId");
                if (loose.Length > 0)
                {
                    sb.Append("<p class='topic-title'>مصوبات جلسه</p>");
                    foreach (var d in loose) AppendDecision(sb, d, actions);
                }
            }

            private static void AppendTopic(StringBuilder sb, DataRow t, string number, DataTable decisions, DataTable actions)
            {
                sb.Append("<p class='topic-title'>").Append(number).Append(". ").Append(E((string)t["Title"])).Append("</p>");
                if (!(t["DiscussionText"] is DBNull) && !string.IsNullOrWhiteSpace((string)t["DiscussionText"]))
                    sb.Append("<p>").Append(Multiline((string)t["DiscussionText"])).Append("</p>");
                foreach (var d in decisions.Select("TopicId = " + (int)t["TopicId"], "SortOrder, DecisionId"))
                    AppendDecision(sb, d, actions);
            }

            private static void AppendDecision(StringBuilder sb, DataRow d, DataTable actions)
            {
                sb.Append("<p class='decision'>— ").Append(Multiline((string)d["DecisionText"])).Append("</p>");
                var acts = actions.Select("DecisionId = " + (int)d["DecisionId"], "ActionId");
                if (acts.Length == 0) return;
                sb.Append("<table style='margin-right:14pt;width:calc(100% - 14pt)'><tr><th>مسئول</th><th>مهلت</th><th>وضعیت</th></tr>");
                foreach (var a in acts)
                {
                    string resp = a["Responsible"] is DBNull ? "—" : (string)a["Responsible"];
                    string due = a["DueDate"] is DateTime ? PersianDate.ToDisplay((DateTime)a["DueDate"], false)
                               : (a["DueText"] is DBNull ? "—" : (string)a["DueText"]);
                    sb.Append("<tr>").Append(Cell(resp)).Append(Cell(due)).Append(Cell((string)a["StatusTitle"])).Append("</tr>");
                }
                sb.Append("</table>");
            }

            private static string TimeText(DataRow m)
            {
                var st = m["StartTime"] is TimeSpan ? (TimeSpan?)m["StartTime"] : null;
                var et = m["EndTime"] is TimeSpan ? (TimeSpan?)m["EndTime"] : null;
                if (st.HasValue && et.HasValue) return Fa(st.Value.ToString(@"hh\:mm")) + " تا " + Fa(et.Value.ToString(@"hh\:mm"));
                if (st.HasValue) return "از " + Fa(st.Value.ToString(@"hh\:mm"));
                return "—";
            }

            private static string Cell(string text)
            {
                return "<td>" + E(text) + "</td>";
            }

            private static string Cell(string label, string value)
            {
                return "<th>" + E(label) + "</th><td>" + E(value) + "</td>";
            }

            private static string E(string s)
            {
                return HttpUtility.HtmlEncode(s ?? string.Empty);
            }

            private static string Multiline(string s)
            {
                return E(s.Replace("\r\n", "\n").Replace("\r", "\n")).Replace("\n", "<br/>");
            }

            private static string Fa(object v)
            {
                return TextNormalizer.ToPersianDigits(v);
            }
        }
    }
}
