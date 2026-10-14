using System;
using System.Data;
using System.Linq;
using System.Text;

namespace MeetingMinutes.Infrastructure
{
    public class ReportFilter
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int? TypeId { get; set; }
        public int? PersonId { get; set; }
        public int? UnitId { get; set; }
        public string Query { get; set; }
        public bool CanSeeDrafts { get; set; }
    }

    /// <summary>گزارش‌های تجمیعی روی یک بازه‌ی زمانی (بر اساس تاریخ جلسه، نه مهلت اقدام).
    /// برخلاف صفحه‌ی «اقدامات» که پیگیری روزانه است، این صفحه برای نگاهِ دوره‌ای (مثلاً «سه ماه گذشته») است.</summary>
    public static class ReportService
    {
        public static DataTable Types()
        {
            return Db.Query("SELECT MeetingTypeId, Title FROM dbo.MeetingTypes WHERE IsActive = 1 ORDER BY MeetingTypeId");
        }

        public static DataTable Units()
        {
            return Db.Query("SELECT OrgUnitId, Title FROM dbo.OrgUnits WHERE IsActive = 1 ORDER BY Title");
        }

        public static DataTable People()
        {
            return Db.Query("SELECT PersonId, FullName FROM dbo.People WHERE IsActive = 1 ORDER BY IsBoardMember DESC, LastName, FirstName");
        }

        public static DataRow Summary(ReportFilter f)
        {
            var dt = Db.Query(
                @"SELECT
                    (SELECT COUNT(*) FROM dbo.Meetings m WHERE m.IsDeleted = 0 AND (@drafts = 1 OR m.StatusId <> 1)
                       AND m.MeetingDate BETWEEN @from AND @to AND (@type IS NULL OR m.MeetingTypeId = @type)) AS MeetingCount,
                    (SELECT COUNT(*) FROM dbo.Decisions d JOIN dbo.Meetings m ON m.MeetingId = d.MeetingId
                       WHERE m.IsDeleted = 0 AND (@drafts = 1 OR m.StatusId <> 1)
                         AND m.MeetingDate BETWEEN @from AND @to AND (@type IS NULL OR m.MeetingTypeId = @type)) AS DecisionCount,
                    (SELECT COUNT(DISTINCT a.ActionId) FROM dbo.ActionItems a
                       JOIN dbo.Decisions d ON d.DecisionId = a.DecisionId
                       JOIN dbo.Meetings m ON m.MeetingId = d.MeetingId
                       WHERE m.IsDeleted = 0 AND (@drafts = 1 OR m.StatusId <> 1)
                         AND m.MeetingDate BETWEEN @from AND @to AND (@type IS NULL OR m.MeetingTypeId = @type)
                         AND (@person IS NULL OR a.ResponsiblePersonId = @person)
                         AND (@unit IS NULL OR a.ResponsibleUnitId = @unit)) AS ActionCount,
                    (SELECT COUNT(DISTINCT a.ActionId) FROM dbo.ActionItems a
                       JOIN dbo.ActionStatuses s ON s.StatusId = a.StatusId
                       JOIN dbo.Decisions d ON d.DecisionId = a.DecisionId
                       JOIN dbo.Meetings m ON m.MeetingId = d.MeetingId
                       WHERE m.IsDeleted = 0 AND (@drafts = 1 OR m.StatusId <> 1)
                         AND m.MeetingDate BETWEEN @from AND @to AND (@type IS NULL OR m.MeetingTypeId = @type)
                         AND (@person IS NULL OR a.ResponsiblePersonId = @person)
                         AND (@unit IS NULL OR a.ResponsibleUnitId = @unit)
                         AND s.IsClosed = 1) AS ActionDoneCount,
                    (SELECT COUNT(DISTINCT a.ActionId) FROM dbo.ActionItems a
                       JOIN dbo.ActionStatuses s ON s.StatusId = a.StatusId
                       JOIN dbo.Decisions d ON d.DecisionId = a.DecisionId
                       JOIN dbo.Meetings m ON m.MeetingId = d.MeetingId
                       WHERE m.IsDeleted = 0 AND (@drafts = 1 OR m.StatusId <> 1)
                         AND m.MeetingDate BETWEEN @from AND @to AND (@type IS NULL OR m.MeetingTypeId = @type)
                         AND (@person IS NULL OR a.ResponsiblePersonId = @person)
                         AND (@unit IS NULL OR a.ResponsibleUnitId = @unit)
                         AND s.IsClosed = 0 AND a.DueDate < CAST(GETDATE() AS date)) AS ActionOverdueCount",
                Ps(f));
            return dt.Rows[0];
        }

        public static DataTable MeetingStatusBars(ReportFilter f)
        {
            return Db.Query(
                @"SELECT s.StatusId, s.Title, COUNT(m.MeetingId) AS Cnt
                  FROM dbo.MeetingStatuses s
                  LEFT JOIN dbo.Meetings m ON m.StatusId = s.StatusId AND m.IsDeleted = 0
                       AND m.MeetingDate BETWEEN @from AND @to AND (@type IS NULL OR m.MeetingTypeId = @type)
                  WHERE @drafts = 1 OR s.StatusId <> 1
                  GROUP BY s.StatusId, s.Title
                  ORDER BY s.StatusId", Ps(f));
        }

        public static DataTable Decisions(ReportFilter f)
        {
            var q = TextNormalizer.Clean(f.Query);
            return Db.Query(
                @"SELECT DISTINCT d.DecisionId, d.DecisionText, t.Title AS TopicTitle,
                         m.MeetingId, m.MeetingDate, m.MeetingDateFa, m.MeetingNo,
                         COALESCE(NULLIF(m.Title, N''), mt.Title) AS MeetingTitle,
                         (SELECT COUNT(*) FROM dbo.ActionItems a2 WHERE a2.DecisionId = d.DecisionId) AS ActionCount,
                         (SELECT COUNT(*) FROM dbo.ActionItems a2 JOIN dbo.ActionStatuses s2 ON s2.StatusId = a2.StatusId
                            WHERE a2.DecisionId = d.DecisionId AND s2.IsClosed = 1) AS DoneCount
                  FROM dbo.Decisions d
                  JOIN dbo.Meetings m ON m.MeetingId = d.MeetingId AND m.IsDeleted = 0
                  JOIN dbo.MeetingTypes mt ON mt.MeetingTypeId = m.MeetingTypeId
                  LEFT JOIN dbo.MeetingTopics t ON t.TopicId = d.TopicId
                  LEFT JOIN dbo.ActionItems a ON a.DecisionId = d.DecisionId
                  WHERE (@drafts = 1 OR m.StatusId <> 1)
                    AND m.MeetingDate BETWEEN @from AND @to
                    AND (@type IS NULL OR m.MeetingTypeId = @type)
                    AND (@person IS NULL OR a.ResponsiblePersonId = @person)
                    AND (@unit IS NULL OR a.ResponsibleUnitId = @unit)
                    AND (@q = '' OR d.DecisionText LIKE @like ESCAPE '\' OR t.Title LIKE @like ESCAPE '\')
                  ORDER BY m.MeetingDate DESC, d.DecisionId DESC",
                Ps(f, q, Db.LikePattern(q)));
        }

        public static DataTable Actions(ReportFilter f)
        {
            var q = TextNormalizer.Clean(f.Query);
            return Db.Query(
                @"SELECT a.ActionId, a.Title, COALESCE(p.FullName, u.Title, a.ResponsibleNote) AS ResponsibleName,
                         a.DueDate, a.DueText, s.Title AS StatusTitle, s.CssClass, s.IsClosed,
                         CAST(CASE WHEN s.IsClosed = 0 AND a.DueDate < CAST(GETDATE() AS date) THEN 1 ELSE 0 END AS bit) AS IsOverdue,
                         m.MeetingId, m.MeetingDate, m.MeetingNo, COALESCE(NULLIF(m.Title, N''), mt.Title) AS MeetingTitle
                  FROM dbo.ActionItems a
                  JOIN dbo.ActionStatuses s ON s.StatusId = a.StatusId
                  JOIN dbo.Decisions d ON d.DecisionId = a.DecisionId
                  JOIN dbo.Meetings m ON m.MeetingId = d.MeetingId AND m.IsDeleted = 0
                  JOIN dbo.MeetingTypes mt ON mt.MeetingTypeId = m.MeetingTypeId
                  LEFT JOIN dbo.People p ON p.PersonId = a.ResponsiblePersonId
                  LEFT JOIN dbo.OrgUnits u ON u.OrgUnitId = a.ResponsibleUnitId
                  WHERE (@drafts = 1 OR m.StatusId <> 1)
                    AND m.MeetingDate BETWEEN @from AND @to
                    AND (@type IS NULL OR m.MeetingTypeId = @type)
                    AND (@person IS NULL OR a.ResponsiblePersonId = @person)
                    AND (@unit IS NULL OR a.ResponsibleUnitId = @unit)
                    AND (@q = '' OR a.Title LIKE @like ESCAPE '\')
                  ORDER BY m.MeetingDate DESC, a.ActionId DESC",
                Ps(f, q, Db.LikePattern(q)));
        }

        private static System.Data.SqlClient.SqlParameter[] Ps(ReportFilter f, string q = "", string like = "%")
        {
            return new[]
            {
                Db.P("@from", f.DateFrom.Date), Db.P("@to", f.DateTo.Date),
                Db.P("@type", f.TypeId), Db.P("@person", f.PersonId), Db.P("@unit", f.UnitId),
                Db.P("@drafts", f.CanSeeDrafts), Db.P("@q", q ?? string.Empty), Db.P("@like", like)
            };
        }

        // ---------- خروجی CSV ----------
        public static string DecisionsToCsv(DataTable dt)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", new[] { "ردیف", "تاریخ جلسه", "شماره جلسه", "جلسه", "موضوع", "متن مصوبه", "اقدام‌ها", "انجام‌شده" }.Select(Csv)));
            int i = 0;
            foreach (DataRow r in dt.Rows)
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv((++i).ToString()), Csv(PersianDate.ToJalali((DateTime)r["MeetingDate"])),
                    Csv(r["MeetingNo"] is DBNull ? "" : (string)r["MeetingNo"]), Csv((string)r["MeetingTitle"]),
                    Csv(r["TopicTitle"] is DBNull ? "" : (string)r["TopicTitle"]), Csv((string)r["DecisionText"]),
                    Csv(r["ActionCount"].ToString()), Csv(r["DoneCount"].ToString())
                }));
            return sb.ToString();
        }

        public static string ActionsToCsv(DataTable dt)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", new[] { "ردیف", "تاریخ جلسه", "جلسه", "شرح اقدام", "مسئول", "مهلت", "وضعیت", "معوق" }.Select(Csv)));
            int i = 0;
            foreach (DataRow r in dt.Rows)
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv((++i).ToString()), Csv(PersianDate.ToJalali((DateTime)r["MeetingDate"])), Csv((string)r["MeetingTitle"]),
                    Csv((string)r["Title"]), Csv(r["ResponsibleName"] is DBNull ? "—" : (string)r["ResponsibleName"]),
                    Csv(r["DueDate"] is DateTime ? PersianDate.ToJalali((DateTime)r["DueDate"]) : (r["DueText"] is DBNull ? "" : (string)r["DueText"])),
                    Csv((string)r["StatusTitle"]), Csv((bool)r["IsOverdue"] ? "بله" : "")
                }));
            return sb.ToString();
        }

        private static string Csv(string s)
        {
            s = s ?? string.Empty;
            if (s.Length > 0 && "=+-@".IndexOf(s[0]) >= 0) s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}
