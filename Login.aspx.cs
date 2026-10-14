using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web;
using System.Web.Security;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class Login : System.Web.UI.Page
    {
        private const string DbErrorText =
            "اتصال به دیتابیس برقرار نشد. رشته‌ی اتصال «MeetingDb» در Web.config و اجرای اسکریپت دیتابیس را بررسی کنید.";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect(FormsAuthentication.DefaultUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            try
            {
                pnlSetup.Visible = !AuthService.HasAnyUser();
            }
            catch (Exception ex) when (ex is SqlException || ex is System.Configuration.ConfigurationErrorsException)
            {
                System.Diagnostics.Debug.WriteLine("[Login Error] " + ex.Message);
                ShowMessage(DbErrorText, "alert-error");
                btnLogin.Enabled = false;
                return;
            }

            if (Request.QueryString["setup"] == "1")
                ShowMessage("مدیر اولیه ساخته شد. اکنون وارد شوید.", "alert-success");
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                var username = txtUsername.Text.Trim();
                var r = AuthService.Login(username, txtPassword.Text, Request.UserHostAddress);
                if (!r.Success)
                {
                    ShowMessage(r.Error, "alert-error");
                    return;
                }

                AuthService.SignIn(Context, r, username);

                var returnUrl = Request.QueryString["ReturnUrl"];
                Response.Redirect(IsLocalUrl(returnUrl) ? returnUrl : FormsAuthentication.DefaultUrl, false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                ShowMessage(DbErrorText, "alert-error");
            }
        }

        // جلوگیری از Open Redirect مسیرهای داخلی سایت
        private static bool IsLocalUrl(string url)
        {
            return !string.IsNullOrEmpty(url)
                && url.StartsWith("/", StringComparison.Ordinal)
                && !url.StartsWith("//", StringComparison.Ordinal)
                && !url.StartsWith("/\\", StringComparison.Ordinal);
        }

        private void ShowMessage(string text, string cssClass)
        {
            litMessage.Text = "<div class=\"alert " + cssClass + "\">" + HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
