using System.Web;

namespace MeetingMinutes.Infrastructure
{
    public static class AvatarHtml
    {
        public static string Render(int? personId, int version, string displayName, string extraClass = "")
        {
            if (personId.HasValue && version > 0)
            {
                var url = VirtualPathUtility.ToAbsolute("~/Pages/Avatar.ashx") + "?id=" + personId + "&v=" + version;
                return "<img class=\"avatar " + extraClass + "\" src=\"" + url + "\" alt=\"\" />";
            }
            var ch = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim().Substring(0, 1);
            return "<span class=\"avatar avatar-initial " + extraClass + "\">" + HttpUtility.HtmlEncode(ch) + "</span>";
        }
    }
}