using System;
using System.Data;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class Users : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin);
            if (IsPostBack) return;
            BindList();
        }

        private void BindList()
        {
            rptUsers.DataSource = UserService.List();
            rptUsers.DataBind();
        }

        protected string FormatLogin(object value)
        {
            return value is DateTime ? PersianDate.ToDisplay((DateTime)value, true) : "—";
        }

        protected string AvatarCell(object personId, object photoVersion, object fullName, object username)
        {
            int? pid = personId is DBNull || personId == null ? (int?)null : Convert.ToInt32(personId);
            int ver = photoVersion is DBNull || photoVersion == null ? 0 : Convert.ToInt32(photoVersion);
            string name = fullName as string;
            if (string.IsNullOrWhiteSpace(name)) name = username as string;
            return AvatarHtml.Render(pid, ver, name);
        }

        protected static string Str(object value)
        {
            var s = value as string;
            return string.IsNullOrWhiteSpace(s) ? "—" : s;
        }

        // ---------- فرم ----------
        private void LoadRoles()
        {
            ddlRole.DataSource = UserService.Roles();
            ddlRole.DataTextField = "Title";
            ddlRole.DataValueField = "RoleId";
            ddlRole.DataBind();
        }

        private void LoadPeople(int? currentPersonId)
        {
            ddlPerson.DataSource = UserService.AvailablePeople(currentPersonId);
            ddlPerson.DataTextField = "FullName";
            ddlPerson.DataValueField = "PersonId";
            ddlPerson.DataBind();
            ddlPerson.Items.Insert(0, new ListItem("— بدون فرد —", ""));
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            litMessage.Text = string.Empty;
            ShowEdit(null);
        }

        private void ShowEdit(int? userId)
        {
            pnlEdit.Visible = true;
            hfUserId.Value = userId.HasValue ? userId.Value.ToString() : string.Empty;
            bool isNew = !userId.HasValue;
            pnlNew.Visible = isNew;
            pnlExisting.Visible = !isNew;
            txtUsername.Enabled = isNew;
            LoadRoles();

            if (isNew)
            {
                litEditTitle.Text = "افزودن کاربر جدید";
                LoadPeople(null);
                txtUsername.Text = string.Empty;
                chkActive.Checked = true;
                var member = ddlRole.Items.FindByValue("3");
                if (member != null) ddlRole.SelectedValue = member.Value;
                return;
            }

            var r = UserService.Get(userId.Value);
            if (r == null)
            {
                pnlEdit.Visible = false;
                litMessage.Text = Alert("کاربر یافت نشد.", "alert-error");
                return;
            }

            int? personId = r["PersonId"] is DBNull ? (int?)null : (int)r["PersonId"];
            litEditTitle.Text = "ویرایش کاربر: " + System.Web.HttpUtility.HtmlEncode((string)r["Username"]);
            txtUsername.Text = (string)r["Username"];
            LoadPeople(personId);
            ddlPerson.SelectedValue = personId.HasValue ? personId.Value.ToString() : string.Empty;
            ddlRole.SelectedValue = Convert.ToInt32(r["RoleId"]).ToString();
            chkActive.Checked = (bool)r["IsActive"];
            txtReset.Text = string.Empty;
        }

        protected void rptUsers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var userId = int.Parse((string)e.CommandArgument);
            if (e.CommandName == "edit")
            {
                litMessage.Text = string.Empty;
                ShowEdit(userId);
            }
            else if (e.CommandName == "delete")
            {
                string error;
                if (UserService.Delete(userId, CurrentUser.UserId, out error))
                {
                    pnlEdit.Visible = false;   // اگر همین کاربر در حال ویرایش بود، فرم بسته شود
                    litMessage.Text = Alert("کاربر حذف شد.", "alert-success");
                }
                else
                {
                    litMessage.Text = Alert(error, "alert-error");
                }
                BindList();
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            int? personId = string.IsNullOrEmpty(ddlPerson.SelectedValue) ? (int?)null : int.Parse(ddlPerson.SelectedValue);
            int roleId = int.Parse(ddlRole.SelectedValue);
            string error;

            if (string.IsNullOrEmpty(hfUserId.Value))
            {
                if (!UserService.Create(txtUsername.Text, personId, roleId, txtPassword.Text, CurrentUser.UserId, out error))
                {
                    litMessage.Text = Alert(error, "alert-error");
                    return;
                }
                pnlEdit.Visible = false;
                litMessage.Text = Alert("کاربر ساخته شد. رمز موقت را به او بدهید.", "alert-success");
            }
            else
            {
                if (!UserService.Update(int.Parse(hfUserId.Value), personId, roleId, chkActive.Checked, CurrentUser.UserId, out error))
                {
                    litMessage.Text = Alert(error, "alert-error");
                    return;
                }
                pnlEdit.Visible = false;
                litMessage.Text = Alert("اطلاعات کاربر ذخیره شد. تغییر نقش از ورود بعدی او اعمال می‌شود.", "alert-success");
            }
            BindList();
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            int userId;
            if (!int.TryParse(hfUserId.Value, out userId)) return;

            string error;
            if (UserService.ResetPassword(userId, txtReset.Text, CurrentUser.UserId, out error))
                litMessage.Text = Alert("رمز موقت جدید تنظیم شد. آن را به کاربر بدهید.", "alert-success");
            else
                litMessage.Text = Alert(error, "alert-error");
            BindList();
        }

        protected void btnUnlock_Click(object sender, EventArgs e)
        {
            int userId;
            if (!int.TryParse(hfUserId.Value, out userId)) return;
            UserService.Unlock(userId, CurrentUser.UserId);
            litMessage.Text = Alert("قفل حساب برداشته شد.", "alert-success");
            BindList();
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlEdit.Visible = false;
        }
    }
}
