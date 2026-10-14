using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace MeetingMinutes.Infrastructure
{
    public static class MeetingStatusIds
    {
        public const int Draft = 1;      // پیش‌نویس
        public const int Pending = 2;    // در انتظار برگزاری
        public const int Held = 3;       // برگزار شده
        public const int Cancelled = 4;  // لغو شده
        public const int Final = 5;      // نهایی و امضا شده (قفل)
    }

    public class MeetingFilter
    {
        public string Query { get; set; }
        public int? TypeId { get; set; }
        public int? StatusId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool CanSeeDrafts { get; set; }
    }

    public class MeetingInput
    {
        public int? MeetingId { get; set; }
        public int MeetingTypeId { get; set; }
        public string LetterNo { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? LocationId { get; set; }
        public string NewLocation { get; set; }
        public int? ChairPersonId { get; set; }
        public int? SecretaryPersonId { get; set; }
        public int StatusId { get; set; }
        public string Preamble { get; set; }
    }

    public class AttendanceRow
    {
        public int PersonId { get; set; }
        public int StatusId { get; set; }
        public string Position { get; set; }
    }

    public static class MeetingService
    {
        private const string LockedMessage = "این جلسه نهایی و قفل شده است؛ برای تغییر، مدیر باید آن را بازگشایی کند.";

        // ---------- کمکی برای تراکنش ----------
        private static SqlCommand Cmd(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] ps)
        {
            var c = new SqlCommand(sql, con, tx);
            if (ps != null && ps.Length > 0) c.Parameters.AddRange(ps);
            return c;
        }

        /// <summary>میزان تکمیل صورت‌جلسه: نهایی/برگزارشده ۱۰۰، دارای مصوبه ۷۵، بقیه ۵۰ و لغوشده ۰.</summary>
        public static int Progress(int statusId, int decisionCount)
        {
            if (statusId == MeetingStatusIds.Final || statusId == MeetingStatusIds.Held) return 100;
            if (statusId == MeetingStatusIds.Cancelled) return 0;
            return decisionCount > 0 ? 75 : 50;
        }

        // ---------- جداول پایه ----------
        public static DataTable Types()
        {
            return Db.Query("SELECT MeetingTypeId, Title FROM dbo.MeetingTypes WHERE IsActive = 1 ORDER BY MeetingTypeId");
        }

        public static DataTable Locations()
        {
            return Db.Query("SELECT LocationId, Title FROM dbo.Locations WHERE IsActive = 1 ORDER BY Title");
        }

        public static DataTable Statuses()
        {
            return Db.Query("SELECT StatusId, Title, CssClass, IsLocked FROM dbo.MeetingStatuses ORDER BY StatusId");
        }

        public static DataTable PeopleOptions()
        {
            return Db.Query("SELECT PersonId, FullName FROM dbo.People WHERE IsActive = 1 ORDER BY IsBoardMember DESC, LastName, FirstName");
        }

        // ---------- فهرست و جزئیات ----------
        public static DataTable Search(MeetingFilter f)
        {
            var q = TextNormalizer.Clean(f.Query);
            var qn = TextNormalizer.Normalize(f.Query);
            return Db.Query(
                @"SELECT m.MeetingId, m.MeetingNo, m.LetterNo, m.MeetingDate, m.MeetingDateFa, m.StatusId,
                         s.Title AS StatusTitle, s.CssClass, mt.Title AS TypeTitle,
                         COALESCE(NULLIF(m.Title, N''), mt.Title) AS DisplayTitle,
                         (SELECT COUNT(*) FROM dbo.Decisions d WHERE d.MeetingId = m.MeetingId) AS DecisionCount,
                         (SELECT COUNT(*) FROM dbo.MeetingAttendees a WHERE a.MeetingId = m.MeetingId) AS AttendeeCount
                  FROM dbo.Meetings m
                  JOIN dbo.MeetingStatuses s ON s.StatusId = m.StatusId
                  JOIN dbo.MeetingTypes mt ON mt.MeetingTypeId = m.MeetingTypeId
                  WHERE m.IsDeleted = 0
                    AND (@drafts = 1 OR m.StatusId <> 1)
                    AND (@q = ''
                         OR m.Title LIKE @likeT ESCAPE '\'
                         OR mt.Title LIKE @likeT ESCAPE '\'
                         OR m.MeetingNo LIKE @likeN ESCAPE '\'
                         OR m.LetterNo LIKE @likeN ESCAPE '\')
                    AND (@type IS NULL OR m.MeetingTypeId = @type)
                    AND (@status IS NULL OR m.StatusId = @status)
                    AND (@from IS NULL OR m.MeetingDate >= @from)
                    AND (@to IS NULL OR m.MeetingDate <= @to)
                  ORDER BY m.MeetingDate DESC, m.MeetingId DESC",
                Db.P("@drafts", f.CanSeeDrafts), Db.P("@q", q),
                Db.P("@likeT", Db.LikePattern(q)), Db.P("@likeN", Db.LikePattern(qn)),
                Db.P("@type", f.TypeId), Db.P("@status", f.StatusId),
                Db.P("@from", f.From), Db.P("@to", f.To));
        }

        public static DataRow Get(int meetingId)
        {
            var dt = Db.Query(
                @"SELECT m.MeetingId, m.MeetingTypeId, m.SeqNo, m.MeetingNo, m.LetterNo, m.Title, m.MeetingDate, m.MeetingDateFa,
                         m.StartTime, m.EndTime, m.LocationId, m.ChairPersonId, m.SecretaryPersonId, m.StatusId,
                         m.Preamble, m.ChairSummary, m.RowVer,
                         mt.Title AS TypeTitle, s.Title AS StatusTitle, s.CssClass, s.IsLocked,
                         l.Title AS LocationTitle, c.FullName AS ChairName, sc.FullName AS SecretaryName,
                         sc.PhotoVersion AS SecretaryPhotoVersion, c.PhotoVersion AS ChairPhotoVersion
                  FROM dbo.Meetings m
                  JOIN dbo.MeetingTypes mt ON mt.MeetingTypeId = m.MeetingTypeId
                  JOIN dbo.MeetingStatuses s ON s.StatusId = m.StatusId
                  LEFT JOIN dbo.Locations l ON l.LocationId = m.LocationId
                  LEFT JOIN dbo.People c ON c.PersonId = m.ChairPersonId
                  LEFT JOIN dbo.People sc ON sc.PersonId = m.SecretaryPersonId
                  WHERE m.MeetingId = @id AND m.IsDeleted = 0", Db.P("@id", meetingId));
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        /// <summary>آیا جلسه هست و قفل نیست؟</summary>
        public static bool IsEditable(int meetingId, out string error)
        {
            error = null;
            var dt = Db.Query(
                @"SELECT s.IsLocked FROM dbo.Meetings m JOIN dbo.MeetingStatuses s ON s.StatusId = m.StatusId
                  WHERE m.MeetingId = @id AND m.IsDeleted = 0", Db.P("@id", meetingId));
            if (dt.Rows.Count == 0) { error = "جلسه یافت نشد."; return false; }
            if ((bool)dt.Rows[0][0]) { error = LockedMessage; return false; }
            return true;
        }

        // ---------- ثبت و ویرایش ----------
        public static bool Save(MeetingInput m, byte[] rowVer, int userId, out int meetingId, out string error)
        {
            error = null;
            meetingId = m.MeetingId ?? 0;

            var title = TextNormalizer.CleanOrNull(m.Title);
            var preamble = TextNormalizer.CleanOrNull(m.Preamble);
            var letterNo = TextNormalizer.CleanOrNull(m.LetterNo);
            if (letterNo != null) letterNo = TextNormalizer.Normalize(letterNo);
            var newLocation = TextNormalizer.CleanOrNull(m.NewLocation);

            if (title != null && title.Length > 300) { error = "عنوان جلسه حداکثر ۳۰۰ نویسه است."; return false; }
            if (letterNo != null && letterNo.Length > 50) { error = "شماره‌ی نامه حداکثر ۵۰ نویسه است."; return false; }
            if (newLocation != null && newLocation.Length > 200) { error = "نام محل حداکثر ۲۰۰ نویسه است."; return false; }
            if (m.StartTime.HasValue && m.EndTime.HasValue && m.EndTime.Value <= m.StartTime.Value)
            { error = "ساعت پایان باید بعد از ساعت شروع باشد."; return false; }
            if (m.StatusId == MeetingStatusIds.Final)
            { error = "برای نهایی‌کردن جلسه از دکمه‌ی «نهایی‌سازی» در صفحه‌ی جلسه استفاده کنید."; return false; }
            if (m.MeetingId.HasValue && rowVer == null)
            { error = "اطلاعات نسخه‌ی رکورد ناقص است؛ صفحه را دوباره باز کنید."; return false; }

            int jy, jm, jd;
            PersianDate.ToJalaliParts(m.Date, out jy, out jm, out jd);
            var dateFa = PersianDate.ToJalali(m.Date);

            using (var con = Db.Open())
            using (var tx = con.BeginTransaction())
            {
                using (var c = Cmd(con, tx, "SELECT COUNT(*) FROM dbo.MeetingTypes WHERE MeetingTypeId = @t AND IsActive = 1", Db.P("@t", m.MeetingTypeId)))
                    if ((int)c.ExecuteScalar() == 0) { error = "نوع جلسه نامعتبر است."; return false; }
                using (var c = Cmd(con, tx, "SELECT COUNT(*) FROM dbo.MeetingStatuses WHERE StatusId = @s AND IsLocked = 0", Db.P("@s", m.StatusId)))
                    if ((int)c.ExecuteScalar() == 0) { error = "وضعیت انتخاب‌شده نامعتبر است."; return false; }

                // محل: اگر محل جدید نوشته شده، پیدا یا ایجاد می‌شود
                int? locationId = m.LocationId;
                if (newLocation != null)
                {
                    object found;
                    using (var c = Cmd(con, tx, "SELECT LocationId FROM dbo.Locations WHERE Title = @t", Db.P("@t", newLocation)))
                        found = c.ExecuteScalar();
                    if (found != null)
                    {
                        locationId = (int)found;
                    }
                    else
                    {
                        using (var c = Cmd(con, tx, "INSERT dbo.Locations (Title) OUTPUT INSERTED.LocationId VALUES (@t)", Db.P("@t", newLocation)))
                            locationId = (int)c.ExecuteScalar();
                    }
                }

                if (!m.MeetingId.HasValue)
                {
                    int seq;
                    string no = NextNumber(con, tx, m.MeetingTypeId, jy, out seq);
                    using (var c = Cmd(con, tx,
                        @"INSERT dbo.Meetings (MeetingTypeId, SeqNo, MeetingNo, LetterNo, Title, MeetingDate, MeetingDateFa,
                                               StartTime, EndTime, LocationId, ChairPersonId, SecretaryPersonId, StatusId, Preamble, CreatedBy)
                          OUTPUT INSERTED.MeetingId
                          VALUES (@t, @seq, @no, @letter, @title, @date, @datefa, @st, @et, @loc, @chair, @sec, @status, @pre, @u)",
                        Db.P("@t", m.MeetingTypeId), Db.P("@seq", seq), Db.P("@no", no), Db.P("@letter", letterNo),
                        Db.P("@title", title), Db.P("@date", m.Date.Date), Db.P("@datefa", dateFa),
                        Db.P("@st", m.StartTime), Db.P("@et", m.EndTime), Db.P("@loc", locationId),
                        Db.P("@chair", m.ChairPersonId), Db.P("@sec", m.SecretaryPersonId),
                        Db.P("@status", m.StatusId), Db.P("@pre", preamble), Db.P("@u", userId)))
                        meetingId = (int)c.ExecuteScalar();
                }
                else
                {
                    DataTable cur = new DataTable();
                    using (var c = Cmd(con, tx,
                        @"SELECT m.MeetingTypeId, m.JalaliYear, s.IsLocked
                          FROM dbo.Meetings m WITH (UPDLOCK) JOIN dbo.MeetingStatuses s ON s.StatusId = m.StatusId
                          WHERE m.MeetingId = @id AND m.IsDeleted = 0", Db.P("@id", m.MeetingId.Value)))
                    using (var da = new SqlDataAdapter(c))
                        da.Fill(cur);

                    if (cur.Rows.Count == 0) { error = "جلسه یافت نشد."; return false; }
                    if ((bool)cur.Rows[0]["IsLocked"]) { error = LockedMessage; return false; }

                    // اگر نوع یا سال شمسی عوض شد، شماره‌ی جدید داده می‌شود؛ وگرنه شماره ثابت می‌ماند
                    bool renumber = Convert.ToInt32(cur.Rows[0]["MeetingTypeId"]) != m.MeetingTypeId
                                    || Convert.ToInt32(cur.Rows[0]["JalaliYear"]) != jy;
                    int seq = 0;
                    string no = null;
                    if (renumber) no = NextNumber(con, tx, m.MeetingTypeId, jy, out seq);

                    int affected;
                    using (var c = Cmd(con, tx,
                        @"UPDATE dbo.Meetings
                          SET MeetingTypeId = @t,
                              SeqNo = CASE WHEN @renum = 1 THEN @seq ELSE SeqNo END,
                              MeetingNo = CASE WHEN @renum = 1 THEN @no ELSE MeetingNo END,
                              LetterNo = @letter, Title = @title, MeetingDate = @date, MeetingDateFa = @datefa,
                              StartTime = @st, EndTime = @et, LocationId = @loc,
                              ChairPersonId = @chair, SecretaryPersonId = @sec, StatusId = @status, Preamble = @pre,
                              UpdatedBy = @u, UpdatedAt = SYSDATETIME()
                          WHERE MeetingId = @id AND RowVer = @rv",
                        Db.P("@t", m.MeetingTypeId), Db.P("@renum", renumber), Db.P("@seq", renumber ? (object)seq : null),
                        Db.P("@no", no), Db.P("@letter", letterNo), Db.P("@title", title),
                        Db.P("@date", m.Date.Date), Db.P("@datefa", dateFa),
                        Db.P("@st", m.StartTime), Db.P("@et", m.EndTime), Db.P("@loc", locationId),
                        Db.P("@chair", m.ChairPersonId), Db.P("@sec", m.SecretaryPersonId),
                        Db.P("@status", m.StatusId), Db.P("@pre", preamble), Db.P("@u", userId),
                        Db.P("@id", m.MeetingId.Value), Db.P("@rv", rowVer)))
                        affected = c.ExecuteNonQuery();

                    if (affected == 0)
                    {
                        error = "این جلسه هم‌زمان توسط کاربر دیگری تغییر کرده است. صفحه را دوباره باز کنید.";
                        return false;
                    }
                }
                tx.Commit();
            }

            Audit.Write(userId, m.MeetingId.HasValue ? "Update" : "Create", "Meeting", meetingId,
                        title ?? ("جلسه " + dateFa));
            return true;
        }

        /// <summary>شماره‌ی بعدی در همان نوع جلسه و سال شمسی (با قفل برای جلوگیری از شماره‌ی تکراری).</summary>
        private static string NextNumber(SqlConnection con, SqlTransaction tx, int typeId, int jalaliYear, out int seq)
        {
            using (var c = Cmd(con, tx,
                @"SELECT ISNULL(MAX(SeqNo), 0) + 1 FROM dbo.Meetings WITH (UPDLOCK, HOLDLOCK)
                  WHERE MeetingTypeId = @t AND JalaliYear = @y", Db.P("@t", typeId), Db.P("@y", jalaliYear)))
                seq = (int)c.ExecuteScalar();

            string prefix;
            using (var c = Cmd(con, tx, "SELECT SettingValue FROM dbo.AppSettings WHERE SettingKey = 'NumberPrefix'"))
            {
                var v = c.ExecuteScalar();
                prefix = v == null || v is DBNull || string.IsNullOrWhiteSpace((string)v) ? "{YYYY}/" : (string)v;
            }
            return prefix.Replace("{YYYY}", jalaliYear.ToString()) + seq.ToString("000");
        }

        // ---------- وضعیت و حذف ----------
        public static bool Finalize(int meetingId, int userId, out string error)
        {
            error = null;
            var r = Get(meetingId);
            if (r == null) { error = "جلسه یافت نشد."; return false; }
            if ((bool)r["IsLocked"]) { error = "این جلسه قبلاً نهایی شده است."; return false; }
            if (Convert.ToInt32(r["StatusId"]) == MeetingStatusIds.Cancelled) { error = "جلسه‌ی لغوشده را نمی‌توان نهایی کرد."; return false; }

            Db.Execute("UPDATE dbo.Meetings SET StatusId = @s, UpdatedBy = @u, UpdatedAt = SYSDATETIME() WHERE MeetingId = @id",
                       Db.P("@s", MeetingStatusIds.Final), Db.P("@u", userId), Db.P("@id", meetingId));
            Audit.Write(userId, "Finalize", "Meeting", meetingId, "نهایی‌سازی و قفل جلسه");
            return true;
        }

        /// <summary>بازگشایی جلسه‌ی نهایی‌شده؛ به وضعیت «برگزار شده» برمی‌گردد. فقط مدیر (در صفحه بررسی می‌شود).</summary>
        public static bool Reopen(int meetingId, int userId, out string error)
        {
            error = null;
            var r = Get(meetingId);
            if (r == null) { error = "جلسه یافت نشد."; return false; }
            if (!(bool)r["IsLocked"]) { error = "این جلسه قفل نیست."; return false; }

            Db.Execute("UPDATE dbo.Meetings SET StatusId = @s, UpdatedBy = @u, UpdatedAt = SYSDATETIME() WHERE MeetingId = @id",
                       Db.P("@s", MeetingStatusIds.Held), Db.P("@u", userId), Db.P("@id", meetingId));
            Audit.Write(userId, "Reopen", "Meeting", meetingId, "بازگشایی جلسه‌ی نهایی‌شده");
            return true;
        }

        public static bool Delete(int meetingId, int userId, out string error)
        {
            if (!IsEditable(meetingId, out error)) return false;
            Db.Execute("UPDATE dbo.Meetings SET IsDeleted = 1, UpdatedBy = @u, UpdatedAt = SYSDATETIME() WHERE MeetingId = @id",
                       Db.P("@u", userId), Db.P("@id", meetingId));
            Audit.Write(userId, "Delete", "Meeting", meetingId, "حذف جلسه");
            return true;
        }

        // ---------- حاضران ----------
        public static DataTable Attendees(int meetingId)
        {
            return Db.Query(
                @"SELECT a.PersonId, p.FullName, p.PhotoVersion, a.AttendanceStatusId, ast.Title AS StatusTitle, a.IsGuest,
                 COALESCE(a.PositionAtMeeting, p.Position) AS Position, a.PositionAtMeeting
                 FROM dbo.MeetingAttendees a
                 JOIN dbo.People p ON p.PersonId = a.PersonId
                 JOIN dbo.AttendanceStatuses ast ON ast.StatusId = a.AttendanceStatusId
                 WHERE a.MeetingId = @m
                 ORDER BY a.IsGuest, p.LastName, p.FirstName", Db.P("@m", meetingId));
        }
        public static DataTable PeopleNotAttending(int meetingId)
        {
            return Db.Query(
                @"SELECT p.PersonId, p.FullName FROM dbo.People p
                  WHERE p.IsActive = 1
                    AND NOT EXISTS (SELECT 1 FROM dbo.MeetingAttendees a WHERE a.MeetingId = @m AND a.PersonId = p.PersonId)
                  ORDER BY p.IsBoardMember DESC, p.LastName, p.FirstName", Db.P("@m", meetingId));
        }

        public static bool AddAttendee(int meetingId, int personId, bool isGuest, out string error)
        {
            if (!IsEditable(meetingId, out error)) return false;
            Db.Execute(
                @"INSERT dbo.MeetingAttendees (MeetingId, PersonId, AttendanceStatusId, IsGuest, PositionAtMeeting)
                  SELECT @m, p.PersonId, 1, @g, p.Position FROM dbo.People p
                  WHERE p.PersonId = @p
                    AND NOT EXISTS (SELECT 1 FROM dbo.MeetingAttendees a WHERE a.MeetingId = @m AND a.PersonId = @p)",
                Db.P("@m", meetingId), Db.P("@p", personId), Db.P("@g", isGuest));
            return true;
        }

        /// <summary>همه‌ی اعضای فعال هیأت را (که هنوز نیستند) با وضعیت «حاضر» اضافه می‌کند.</summary>
        public static bool AddAllBoardMembers(int meetingId, out int added, out string error)
        {
            added = 0;
            if (!IsEditable(meetingId, out error)) return false;
            added = Db.Execute(
                @"INSERT dbo.MeetingAttendees (MeetingId, PersonId, AttendanceStatusId, IsGuest, PositionAtMeeting)
                  SELECT @m, p.PersonId, 1, 0, p.Position FROM dbo.People p
                  WHERE p.IsActive = 1 AND p.IsBoardMember = 1
                    AND NOT EXISTS (SELECT 1 FROM dbo.MeetingAttendees a WHERE a.MeetingId = @m AND a.PersonId = p.PersonId)",
                Db.P("@m", meetingId));
            return true;
        }

        public static bool SaveAttendance(int meetingId, IEnumerable<AttendanceRow> rows, out string error)
        {
            if (!IsEditable(meetingId, out error)) return false;
            using (var con = Db.Open())
            using (var tx = con.BeginTransaction())
            {
                foreach (var r in rows)
                {
                    var pos = TextNormalizer.CleanOrNull(r.Position);
                    if (pos != null && pos.Length > 150) pos = pos.Substring(0, 150);
                    using (var c = Cmd(con, tx,
                        @"UPDATE dbo.MeetingAttendees SET AttendanceStatusId = @s, PositionAtMeeting = @pos
                          WHERE MeetingId = @m AND PersonId = @p AND EXISTS (SELECT 1 FROM dbo.AttendanceStatuses WHERE StatusId = @s)",
                        Db.P("@s", r.StatusId), Db.P("@pos", pos), Db.P("@m", meetingId), Db.P("@p", r.PersonId)))
                        c.ExecuteNonQuery();
                }
                tx.Commit();
            }
            return true;
        }

        public static bool RemoveAttendee(int meetingId, int personId, out string error)
        {
            if (!IsEditable(meetingId, out error)) return false;
            Db.Execute("DELETE dbo.MeetingAttendees WHERE MeetingId = @m AND PersonId = @p", Db.P("@m", meetingId), Db.P("@p", personId));
            return true;
        }

        // ---------- دستور جلسه ----------
        public static DataTable Agenda(int meetingId)
        {
            return Db.Query("SELECT AgendaItemId, SortOrder, Title FROM dbo.MeetingAgendaItems WHERE MeetingId = @m ORDER BY SortOrder",
                            Db.P("@m", meetingId));
        }

        public static bool AddAgendaItem(int meetingId, string title, out string error)
        {
            title = TextNormalizer.Clean(title);
            if (title.Length == 0) { error = "عنوان بند دستور جلسه را وارد کنید."; return false; }
            if (title.Length > 500) { error = "عنوان بند حداکثر ۵۰۰ نویسه است."; return false; }
            if (!IsEditable(meetingId, out error)) return false;

            Db.Execute(
                @"INSERT dbo.MeetingAgendaItems (MeetingId, SortOrder, Title)
                  SELECT @m, ISNULL(MAX(SortOrder), 0) + 1, @t FROM dbo.MeetingAgendaItems WHERE MeetingId = @m",
                Db.P("@m", meetingId), Db.P("@t", title));
            return true;
        }

        public static bool RemoveAgendaItem(int meetingId, int itemId, out string error)
        {
            if (!IsEditable(meetingId, out error)) return false;
            Db.Execute(
                @"DELETE dbo.MeetingAgendaItems WHERE AgendaItemId = @i AND MeetingId = @m;
                  WITH x AS (SELECT SortOrder, ROW_NUMBER() OVER (ORDER BY SortOrder) AS rn
                             FROM dbo.MeetingAgendaItems WHERE MeetingId = @m)
                  UPDATE x SET SortOrder = rn;",
                Db.P("@i", itemId), Db.P("@m", meetingId));
            return true;
        }

        /// <param name="direction">-1 = بالاتر، +1 = پایین‌تر</param>
        public static bool MoveAgendaItem(int meetingId, int itemId, int direction, out string error)
        {
            if (!IsEditable(meetingId, out error)) return false;
            Db.Execute(
                @"DECLARE @so smallint = (SELECT SortOrder FROM dbo.MeetingAgendaItems WHERE AgendaItemId = @i AND MeetingId = @m);
                  DECLARE @nid int, @nso smallint;
                  SELECT TOP 1 @nid = AgendaItemId, @nso = SortOrder
                  FROM dbo.MeetingAgendaItems
                  WHERE MeetingId = @m AND ((@d < 0 AND SortOrder < @so) OR (@d > 0 AND SortOrder > @so))
                  ORDER BY CASE WHEN @d < 0 THEN -SortOrder ELSE SortOrder END;
                  IF @nid IS NOT NULL AND @so IS NOT NULL
                  BEGIN
                      UPDATE dbo.MeetingAgendaItems SET SortOrder = @nso WHERE AgendaItemId = @i;
                      UPDATE dbo.MeetingAgendaItems SET SortOrder = @so WHERE AgendaItemId = @nid;
                  END",
                Db.P("@i", itemId), Db.P("@m", meetingId), Db.P("@d", direction));
            return true;
        }

        // ---------- محتوای صورت‌جلسه (برای نمایش) ----------
        public static DataTable Topics(int meetingId)
        {
            return Db.Query("SELECT TopicId, ParentTopicId, SortOrder, Title, DiscussionText FROM dbo.MeetingTopics WHERE MeetingId = @m ORDER BY SortOrder, TopicId",
                            Db.P("@m", meetingId));
        }

        public static DataTable Decisions(int meetingId)
        {
            return Db.Query("SELECT DecisionId, TopicId, SortOrder, DecisionText FROM dbo.Decisions WHERE MeetingId = @m ORDER BY SortOrder, DecisionId",
                            Db.P("@m", meetingId));
        }

        public static DataTable Actions(int meetingId)
        {
            return Db.Query(
                @"SELECT a.ActionId, a.DecisionId, a.Title, COALESCE(p.FullName, u.Title, a.ResponsibleNote) AS Responsible,
                         a.DueDate, a.DueText, s.Title AS StatusTitle, s.CssClass
                  FROM dbo.ActionItems a
                  JOIN dbo.Decisions d ON d.DecisionId = a.DecisionId
                  JOIN dbo.ActionStatuses s ON s.StatusId = a.StatusId
                  LEFT JOIN dbo.People p ON p.PersonId = a.ResponsiblePersonId
                  LEFT JOIN dbo.OrgUnits u ON u.OrgUnitId = a.ResponsibleUnitId
                  WHERE d.MeetingId = @m ORDER BY a.ActionId", Db.P("@m", meetingId));
        }
    }
}
