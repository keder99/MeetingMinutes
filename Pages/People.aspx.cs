using System;
using System.Data;
using System.Web;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class People : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin, RoleCodes.Secretary);
            if (IsPostBack) return;
            LoadUnits(null);
            BindList();
        }

        // ---------- فهرست ----------
        private void BindList()
        {
            var dt = PeopleService.Search(txtSearch.Text, ddlActive.SelectedValue, ddlBoard.SelectedValue);
            rptPeople.DataSource = dt;
            rptPeople.DataBind();
            rptPeople.Visible = dt.Rows.Count > 0;
            pnlEmpty.Visible = dt.Rows.Count == 0;
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindList();
        }

        protected void rptPeople_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var arg = (string)e.CommandArgument;
            if (e.CommandName == "edit")
            {
                ShowEdit(int.Parse(arg));
            }
            else if (e.CommandName == "toggle")
            {
                var parts = arg.Split('|');
                string error;
                if (PeopleService.SetActive(int.Parse(parts[0]), parts[1] == "1", out error))
                {
                    Audit.Write(CurrentUser.UserId, "Update", "Person", int.Parse(parts[0]),
                                parts[1] == "1" ? "فعال‌سازی فرد" : "غیرفعال‌سازی فرد");
                    litMessage.Text = Alert("وضعیت فرد تغییر کرد.", "alert-success");
                }
                else
                {
                    litMessage.Text = Alert(error, "alert-error");
                }
                BindList();
            }
            else if (e.CommandName == "delete")
            {
                int personId = int.Parse(arg);
                string error;
                if (PeopleService.Delete(personId, out error))
                {
                    Audit.Write(CurrentUser.UserId, "Delete", "Person", personId, "حذف فرد");
                    pnlEdit.Visible = false;   // اگر همین فرد در حال ویرایش بود، فرم بسته شود
                    litMessage.Text = Alert("فرد حذف شد.", "alert-success");
                }
                else
                {
                    litMessage.Text = Alert(error, "alert-error");
                }
                BindList();
            }
        }

        // ---------- فرم ----------
        private void LoadUnits(int? includeId)
        {
            ddlUnit.DataSource = OrgUnitService.Options(includeId, null);
            ddlUnit.DataTextField = "Title";
            ddlUnit.DataValueField = "OrgUnitId";
            ddlUnit.DataBind();
            ddlUnit.Items.Insert(0, new ListItem("— بدون واحد —", ""));
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            litMessage.Text = string.Empty;
            ShowEdit(null);
        }

        private void ShowEdit(int? personId)
        {
            pnlEdit.Visible = true;
            hfPersonId.Value = personId.HasValue ? personId.Value.ToString() : string.Empty;

            if (!personId.HasValue)
            {
                litEditTitle.Text = "افزودن فرد جدید";
                LoadUnits(null);
                txtPrefix.Text = txtFirstName.Text = txtLastName.Text = txtPosition.Text = txtEmail.Text = txtMobile.Text = string.Empty;
                chkBoard.Checked = true;
                chkActive.Checked = true;
                pnlAliases.Visible = false;
                return;
            }

            var r = PeopleService.Get(personId.Value);
            if (r == null)
            {
                pnlEdit.Visible = false;
                litMessage.Text = Alert("فرد یافت نشد.", "alert-error");
                return;
            }

            litEditTitle.Text = "ویرایش: " + HttpUtility.HtmlEncode((string)r["FullName"]);
            int? unitId = r["OrgUnitId"] is DBNull ? (int?)null : (int)r["OrgUnitId"];
            LoadUnits(unitId);
            ddlUnit.SelectedValue = unitId.HasValue ? unitId.Value.ToString() : string.Empty;

            txtPrefix.Text = Str(r["Prefix"]);
            txtFirstName.Text = Str(r["FirstName"]);
            txtLastName.Text = (string)r["LastName"];
            txtPosition.Text = Str(r["Position"]);
            txtEmail.Text = Str(r["Email"]);
            txtMobile.Text = Str(r["Mobile"]);
            chkBoard.Checked = (bool)r["IsBoardMember"];
            chkActive.Checked = (bool)r["IsActive"];

            pnlAliases.Visible = true;
            txtAlias.Text = string.Empty;
            BindAliases(personId.Value);
        }

        private static string Str(object v)
        {
            return v is DBNull ? string.Empty : (string)v;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            int? id = string.IsNullOrEmpty(hfPersonId.Value) ? (int?)null : int.Parse(hfPersonId.Value);
            var input = new PersonInput
            {
                PersonId = id,
                Prefix = txtPrefix.Text,
                FirstName = txtFirstName.Text,
                LastName = txtLastName.Text,
                Position = txtPosition.Text,
                OrgUnitId = string.IsNullOrEmpty(ddlUnit.SelectedValue) ? (int?)null : int.Parse(ddlUnit.SelectedValue),
                IsBoardMember = chkBoard.Checked,
                IsActive = chkActive.Checked,
                Email = txtEmail.Text,
                Mobile = txtMobile.Text
            };

            int savedId;
            string error;
            if (!PeopleService.Save(input, out savedId, out error))
            {
                litMessage.Text = Alert(error, "alert-error");
                return;
            }

            Audit.Write(CurrentUser.UserId, id.HasValue ? "Update" : "Create", "Person", savedId, input.LastName);
            BindList();
            ShowEdit(savedId);   // در حالت ویرایش می‌ماند تا بتوان نام مستعار افزود
            litMessage.Text = Alert("اطلاعات ذخیره شد.", "alert-success");
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlEdit.Visible = false;
        }

        // ---------- نام‌های مستعار ----------
        private void BindAliases(int personId)
        {
            rptAliases.DataSource = PeopleService.Aliases(personId);
            rptAliases.DataBind();
        }

        protected void btnAddAlias_Click(object sender, EventArgs e)
        {
            int personId;
            if (!int.TryParse(hfPersonId.Value, out personId)) return;

            string error;
            if (PeopleService.AddAlias(personId, txtAlias.Text, out error))
            {
                Audit.Write(CurrentUser.UserId, "Update", "Person", personId, "افزودن نام مستعار");
                txtAlias.Text = string.Empty;
                litMessage.Text = string.Empty;
            }
            else
            {
                litMessage.Text = Alert(error, "alert-error");
            }
            BindAliases(personId);
        }

        protected void rptAliases_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int personId;
            if (e.CommandName != "remove" || !int.TryParse(hfPersonId.Value, out personId)) return;
            PeopleService.RemoveAlias(personId, int.Parse((string)e.CommandArgument));
            Audit.Write(CurrentUser.UserId, "Update", "Person", personId, "حذف نام مستعار");
            BindAliases(personId);
        }
    }
}
