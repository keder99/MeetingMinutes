using System;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class ChangePassword : BasePage
    {
        // کاربری که رمز موقت داره فقط به همین صفحه میرسه
        protected override bool AllowPendingPasswordChange
        {
            get { return true; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.QueryString["forced"] == "1")
                litMessage.Text = Alert("برای ادامه، باید رمز عبور موقت خود را تغییر دهید.", "alert-info");
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (txtNew.Text != txtConfirm.Text)
            {
                litMessage.Text = Alert("رمز جدید و تکرار آن یکسان نیستند.", "alert-error");
                return;
            }

            string error;
            if (!UserService.ChangeOwnPassword(CurrentUser.UserId, txtCurrent.Text, txtNew.Text, out error))
            {
                litMessage.Text = Alert(error, "alert-error");
                return;
            }

            Response.Redirect("~/Pages/Default.aspx?pwd=1", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
