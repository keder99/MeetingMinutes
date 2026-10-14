<%@ WebHandler Language="C#" Class="MeetingMinutes.AvatarHandler" %>
using System;
using System.IO;
using System.Web;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public class AvatarHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            if (ctx.User == null || !ctx.User.Identity.IsAuthenticated) { ctx.Response.StatusCode = 401; return; }

            int id;
            if (!int.TryParse(ctx.Request.QueryString["id"], out id)) { ctx.Response.StatusCode = 404; return; }

            var path = Path.Combine(ctx.Server.MapPath("~/App_Data/Avatars"), id + ".jpg");
            if (!File.Exists(path)) { ctx.Response.StatusCode = 404; return; }

            ctx.Response.ContentType = "image/jpeg";
            ctx.Response.Cache.SetCacheability(HttpCacheability.Private);
            ctx.Response.Cache.SetMaxAge(TimeSpan.FromDays(365));
            ctx.Response.TransmitFile(path);
        }
    }
}