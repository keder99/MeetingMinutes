<%@ Page Title="پروفایل من" Language="C#" MasterPageFile="~/Pages/Site.Master" AutoEventWireup="true" CodeBehind="Profile.aspx.cs" Inherits="MeetingMinutes.Profile" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-head">
        <div>
            <h1>پروفایل من</h1>
            <p>اطلاعات فردی خودتان را کامل یا به‌روزرسانی کنید</p>
        </div>
        <div class="actions">
            <a runat="server" href="~/Pages/ChangePassword.aspx" class="btn"><i class="ti ti-key"></i>تغییر رمز عبور</a>
        </div>
    </div>

    <asp:Literal ID="litMessage" runat="server" />
    <asp:Panel ID="pnlIncomplete" runat="server" Visible="false">
        <div class="alert alert-info">
            هنوز اطلاعات فردی شما تکمیل نشده است. فرم زیر را پر و ذخیره کنید تا نامتان به‌جای نام کاربری در سامانه نمایش داده شود.
       
        </div>
    </asp:Panel>
    <div class="cflex">
        <div class="panel" style="width: 60%;min-width:600px;">
            <div class="panel-head">
                <h2>اطلاعات حساب</h2>
            </div>
            <div class="panel-body">
                <div class="form-grid">
                    <div class="field">
                        <label>نام کاربری</label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="ltr" ReadOnly="true" Enabled="false" />
                    </div>
                    <div class="field">
                        <label>نقش</label>
                        <asp:TextBox ID="txtRole" runat="server" ReadOnly="true" Enabled="false" />
                    </div>
                    <div class="field full">
                        <label>واحد سازمانی</label>
                        <asp:TextBox ID="txtUnit" runat="server" ReadOnly="true" Enabled="false" />
                        <div class="hint">برای تغییر واحد سازمانی، به مدیر یا دبیر سامانه اطلاع دهید.</div>
                    </div>
                </div>
            </div>
        </div>
        <div class="panel" style="width:39%;min-width:400px;">
            <div class="panel-head">
                <h2>عکس پروفایل</h2>
            </div>
            <div class="panel-body">
                <div class="avatar-box-body">
                <div id="avatarPreview" style="margin: -7px 0px 10px 0px;">
                    <asp:Literal ID="litAvatar" runat="server" />
                </div>
                <div style="display:grid" >
                <label class="btn" for="<%= fuPhoto.ClientID %>"><i class="ti ti-photo"></i>انتخاب عکس</label>
                <asp:FileUpload ID="fuPhoto" runat="server" accept="image/png,image/jpeg"
                    Style="display: none" onchange="previewAvatar(this)" />
                <asp:Button ID="btnPhoto" runat="server" Text="ذخیره عکس" CssClass="btn btn-primary"
                    OnClick="btnPhoto_Click" />
                <asp:Button ID="btnRemovePhoto" runat="server" Text="حذف عکس" CssClass="btn" 
                    OnClick="btnRemovePhoto_Click" OnClientClick="return confirm('عکس پروفایل حذف شود؟');" />
                </div>
                </div>
                <div class="field">
                    <div class="hint">JPG یا PNG، حداکثر ۲ مگابایت. عکس به‌صورت دایره‌ای نمایش داده می‌شود.</div>
                <asp:Panel ID="pnlNoPerson" runat="server" Visible="false" CssClass="hint">
                    برای گذاشتن عکس، ابتدا اطلاعات فردی را ذخیره کنید.
                </asp:Panel>
                </div>
            </div>
        </div>

        <div class="panel mb" style="width: 100%;">
            <div class="panel-head">
                <h2>اطلاعات فردی</h2>
            </div>
            <div class="panel-body">
                <datalist id="prefixList">
                    <option value="دکتر"></option>
                    <option value="مهندس"></option>
                    <option value="آقای"></option>
                    <option value="خانم"></option>
                    <option value="خانم دکتر"></option>
                    <option value="حجت‌الاسلام"></option>
                </datalist>
                <div class="form-grid">
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="txtPrefix" Text="عنوان (دکتر، مهندس، ...)" />
                        <asp:TextBox ID="txtPrefix" runat="server" MaxLength="30" list="prefixList" />
                    </div>
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="txtFirstName" Text="نام" />
                        <asp:TextBox ID="txtFirstName" runat="server" MaxLength="80" />
                    </div>
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="txtLastName" Text="نام خانوادگی *" />
                        <asp:TextBox ID="txtLastName" runat="server" MaxLength="80" />
                    </div>
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="txtPosition" Text="سمت" />
                        <asp:TextBox ID="txtPosition" runat="server" MaxLength="150" />
                    </div>
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="txtEmail" Text="ایمیل" />
                        <asp:TextBox ID="txtEmail" runat="server" CssClass="ltr" MaxLength="150" />
                    </div>
                    <div class="field">
                        <asp:Label runat="server" AssociatedControlID="txtMobile" Text="تلفن همراه *" />
                        <asp:TextBox ID="txtMobile" runat="server" CssClass="ltr" MaxLength="11" />
                    </div>
                </div>
                <div class="actions-row">
                    <asp:Button ID="btnSave" runat="server" Text="ذخیره" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                </div>
            </div>
        </div>
    </div>
    <script>
        function previewAvatar(input) {
            if (!input.files || !input.files[0]) return;
            var reader = new FileReader();
            reader.onload = function (e) {
                document.getElementById('avatarPreview').innerHTML =
                    '<img class="avatar avatar-xl" alt="" src="' + e.target.result + '" />';
            };
            reader.readAsDataURL(input.files[0]);
        }
    </script>
</asp:Content>
