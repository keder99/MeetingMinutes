using System;
using System.Web.UI.WebControls;
using MeetingMinutes.Infrastructure;

namespace MeetingMinutes
{
    public partial class OrgUnits : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole(RoleCodes.Admin, RoleCodes.Secretary);
            if (IsPostBack) return;
            BindList();
        }

        private void BindList()
        {
            var dt = OrgUnitService.List();
            rptUnits.DataSource = dt;
            rptUnits.DataBind();
            rptUnits.Visible = dt.Rows.Count > 0;
            pnlEmpty.Visible = dt.Rows.Count == 0;
        }

        private void LoadParents(int? includeId, int? excludeId)
        {
            ddlParent.DataSource = OrgUnitService.Options(includeId, excludeId);
            ddlParent.DataTextField = "Title";
            ddlParent.DataValueField = "OrgUnitId";
            ddlParent.DataBind();
            ddlParent.Items.Insert(0, new ListItem("— بدون والد (واحد سطح بالا) —", ""));
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            litMessage.Text = string.Empty;
            ShowEdit(null);
        }

        private void ShowEdit(int? unitId)
        {
            pnlEdit.Visible = true;
            hfUnitId.Value = unitId.HasValue ? unitId.Value.ToString() : string.Empty;

            if (!unitId.HasValue)
            {
                litEditTitle.Text = "افزودن واحد جدید";
                LoadParents(null, null);
                txtTitle.Text = string.Empty;
                chkActive.Checked = true;
                return;
            }

            var r = OrgUnitService.Get(unitId.Value);
            if (r == null)
            {
                pnlEdit.Visible = false;
                litMessage.Text = Alert("واحد یافت نشد.", "alert-error");
                return;
            }

            int? parentId = r["ParentUnitId"] is DBNull ? (int?)null : (int)r["ParentUnitId"];
            litEditTitle.Text = "ویرایش واحد";
            LoadParents(parentId, unitId.Value);
            ddlParent.SelectedValue = parentId.HasValue ? parentId.Value.ToString() : string.Empty;
            txtTitle.Text = (string)r["Title"];
            chkActive.Checked = (bool)r["IsActive"];
        }

        protected void rptUnits_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var arg = (string)e.CommandArgument;
            if (e.CommandName == "edit")
            {
                ShowEdit(int.Parse(arg));
            }
            else if (e.CommandName == "toggle")
            {
                var parts = arg.Split('|');
                OrgUnitService.SetActive(int.Parse(parts[0]), parts[1] == "1");
                Audit.Write(CurrentUser.UserId, "Update", "OrgUnit", int.Parse(parts[0]),
                            parts[1] == "1" ? "فعال‌سازی واحد" : "غیرفعال‌سازی واحد");
                litMessage.Text = Alert("وضعیت واحد تغییر کرد.", "alert-success");
                BindList();
            }
            else if (e.CommandName == "delete")
            {
                int unitId = int.Parse(arg);
                string error;
                if (OrgUnitService.Delete(unitId, out error))
                {
                    Audit.Write(CurrentUser.UserId, "Delete", "OrgUnit", unitId, "حذف واحد");
                    pnlEdit.Visible = false;   // اگر همین واحد در حال ویرایش بود، فرم بسته شود
                    litMessage.Text = Alert("واحد حذف شد.", "alert-success");
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
            int? id = string.IsNullOrEmpty(hfUnitId.Value) ? (int?)null : int.Parse(hfUnitId.Value);
            int? parentId = string.IsNullOrEmpty(ddlParent.SelectedValue) ? (int?)null : int.Parse(ddlParent.SelectedValue);

            int savedId;
            string error;
            if (!OrgUnitService.Save(id, txtTitle.Text, parentId, chkActive.Checked, out savedId, out error))
            {
                litMessage.Text = Alert(error, "alert-error");
                return;
            }

            Audit.Write(CurrentUser.UserId, id.HasValue ? "Update" : "Create", "OrgUnit", savedId, txtTitle.Text.Trim());
            pnlEdit.Visible = false;
            BindList();
            litMessage.Text = Alert("واحد ذخیره شد.", "alert-success");
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlEdit.Visible = false;
        }
    }
}
