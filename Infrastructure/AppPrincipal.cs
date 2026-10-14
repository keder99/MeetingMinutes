using System.Security.Principal;

namespace MeetingMinutes.Infrastructure
{
    public static class RoleCodes
    {
        public const string Admin = "Admin";
        public const string Secretary = "Secretary";
        public const string Member = "Member";
        public const string Viewer = "Viewer";

        public static string Title(string code)
        {
            switch (code)
            {
                case Admin: return "مدیر سامانه";
                case Secretary: return "دبیر جلسه";
                case Member: return "عضو هیأت";
                case Viewer: return "مشاهده‌گر";
                default: return code;
            }
        }
    }

    /// <summary>کاربر جاری؛ در Global.asax از روی کوکی Forms Authentication ساخته می‌شود.</summary>
    public class AppPrincipal : GenericPrincipal
    {
        public int UserId { get; private set; }
        public string RoleCode { get; private set; }
        public string DisplayName { get; private set; }

        public AppPrincipal(IIdentity identity, int userId, string roleCode, string displayName)
            : base(identity, new[] { roleCode })
        {
            UserId = userId;
            RoleCode = roleCode;
            DisplayName = displayName;
        }

        public bool IsAdmin { get { return RoleCode == RoleCodes.Admin; } }

        /// <summary>ثبت و ویرایش جلسه/صورت‌جلسه.</summary>
        public bool CanEditMinutes { get { return RoleCode == RoleCodes.Admin || RoleCode == RoleCodes.Secretary; } }
    }
}
