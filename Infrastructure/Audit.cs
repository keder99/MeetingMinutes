using System;
using System.Diagnostics;

namespace MeetingMinutes.Infrastructure
{
    /// <summary>ثبت رویدادها در جدول AuditLog. خطای ثبت لاگ نباید کار اصلی را متوقف کند.</summary>
    public static class Audit
    {
        public static void Write(int? userId, string action, string entityName, int? entityId, string summary)
        {
            try
            {
                if (summary != null && summary.Length > 500) summary = summary.Substring(0, 500);
                Db.Execute(
                    "INSERT dbo.AuditLog (UserId, Action, EntityName, EntityId, Summary) VALUES (@u, @a, @e, @i, @s)",
                    Db.P("@u", userId), Db.P("@a", action), Db.P("@e", entityName),
                    Db.P("@i", entityId), Db.P("@s", summary));
            }
            catch (Exception ex)
            {
                Trace.TraceError("Audit.Write failed: " + ex);
            }
        }
    }
}
