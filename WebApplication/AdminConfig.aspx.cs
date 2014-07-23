
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminConfig : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");

			StringBuilder sb = new StringBuilder(200);
			sb.Append("<script type=\"text/javascript\">\r\n<!--\r\n");
			sb.AppendFormat("\tvar __DateTimeFormatTextBox = '{0}';\r\n", txtDateTimeFormat.ClientID);
			sb.Append("// -->\r\n</script>");
			lblStrings.Text = sb.ToString();

			if(!Page.IsPostBack) {
				// Setup validation regular expressions
				revMainUrl.ValidationExpression = Settings.MainUrlRegex;
				revWikiTitle.ValidationExpression = Settings.WikiTitleRegex;
				revContactEmail.ValidationExpression = Settings.EmailRegex;
				revSenderEmail.ValidationExpression = Settings.EmailRegex;
				revSmtpServer.ValidationExpression = Settings.SmtpServerRegex;

				// Load current values
				LoadGeneralConfig();
				LoadContentConfig();
				LoadSecurityConfig();
				LoadAdvancedConfig();
			}
		}

		/// <summary>
		/// Loads the general configuration.
		/// </summary>
		private void LoadGeneralConfig() {
			txtWikiTitle.Text = Settings.WikiTitle;
			txtMainUrl.Text = Settings.MainUrl;
			txtContactEmail.Text = Settings.ContactEmail;
			txtSenderEmail.Text = Settings.SenderEmail;
			txtErrorsEmails.Text = string.Join(", ", Settings.ErrorsEmails);
			txtSmtpServer.Text = Settings.SmtpServer;
			int port = Settings.SmtpPort;
			txtSmtpPort.Text = port != -1 ? port.ToString() : "";
			txtUsername.Text = Settings.SmtpUsername;
			txtPassword.Attributes.Add("value", Settings.SmtpPassword);
			chkEnableSslForSmtp.Checked = Settings.SmtpSsl;
		}

		/// <summary>
		/// Populates the Themes list selecting the current one.
		/// </summary>
		/// <param name="current">The current theme.</param>
		private void PopulateThemes(string current) {
			current = current.ToLowerInvariant();

			string[] themes = Tools.AvailableThemes;
			lstRootTheme.Items.Clear();
			foreach(string theme in themes) {
				lstRootTheme.Items.Add(new ListItem(theme, theme));
				if(theme.ToLowerInvariant() == current) lstRootTheme.Items[lstRootTheme.Items.Count - 1].Selected = true;
			}
		}

		/// <summary>
		/// Populates the main pages list selecting the current one.
		/// </summary>
		/// <param name="current">The current page.</param>
		private void PopulateMainPages(string current) {
			current = current.ToLowerInvariant();

			List<PageInfo> pages = Pages.GetPages(null);
			lstMainPage.Items.Clear();
			foreach(PageInfo page in pages) {
				lstMainPage.Items.Add(new ListItem(page.FullName, page.FullName));
				if(page.FullName.ToLowerInvariant() == current) {
					lstMainPage.SelectedIndex = -1;
					lstMainPage.Items[lstMainPage.Items.Count - 1].Selected = true;
				}
			}
		}

		/// <summary>
		/// Populates the languages list selecting the current one.
		/// </summary>
		/// <param name="current">The current language.</param>
		private void PopulateLanguages(string current) {
			current = current.ToLowerInvariant();

			string[] langs = Tools.AvailableCultures;
			lstDefaultLanguage.Items.Clear();
			foreach(string lang in langs) {
				string[] fields = lang.Split('|');
				lstDefaultLanguage.Items.Add(new ListItem(fields[1], fields[0]));
				if(fields[0].ToLowerInvariant() == current) lstDefaultLanguage.Items[lstDefaultLanguage.Items.Count - 1].Selected = true;
			}
		}

		/// <summary>
		/// Populates the time zones list selecting the current one.
		/// </summary>
		/// <param name="current">The current time zone.</param>
		private void PopulateTimeZones(string current) {
			for(int i = 0; i < lstDefaultTimeZone.Items.Count; i++) {
				if(lstDefaultTimeZone.Items[i].Value == current) lstDefaultTimeZone.Items[i].Selected = true;
				else lstDefaultTimeZone.Items[i].Selected = false;
			}
		}

		/// <summary>
		/// Populates the date/time format templates list.
		/// </summary>
		private void PopulateDateTimeFormats() {
			StringBuilder sb = new StringBuilder(500);
			DateTime test = DateTime.Now;
			sb.Append(@"<option value=""ddd', 'dd' 'MMM' 'yyyy' 'HH':'mm"">" + test.ToString("ddd, dd MMM yyyy HH':'mm") + "</option>");
			sb.Append(@"<option value=""dddd', 'dd' 'MMMM' 'yyyy' 'HH':'mm"">" + test.ToString("dddd, dd MMMM yyyy HH':'mm") + "</option>");
			sb.Append(@"<option value=""yyyy'/'MM'/'dd' 'HH':'mm"">" + test.ToString("yyyy'/'MM'/'dd' 'HH':'mm") + "</option>");
			sb.Append(@"<option value=""MM'/'dd'/'yyyy' 'HH':'mm"">" + test.ToString("MM'/'dd'/'yyyy' 'HH':'mm") + "</option>");
			sb.Append(@"<option value=""dd'/'MM'/'yyyy' 'HH':'mm"">" + test.ToString("dd'/'MM'/'yyyy' 'HH':'mm") + "</option>");

			sb.Append(@"<option value=""ddd', 'dd' 'MMM' 'yyyy' 'hh':'mm' 'tt"">" + test.ToString("ddd, dd MMM yyyy hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""dddd', 'dd' 'MMMM' 'yyyy' 'hh':'mm' 'tt"">" + test.ToString("dddd, dd MMMM yyyy hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""yyyy'/'MM'/'dd' 'hh':'mm' 'tt"">" + test.ToString("yyyy'/'MM'/'dd' 'hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""MM'/'dd'/'yyyy' 'hh':'mm' 'tt"">" + test.ToString("MM'/'dd'/'yyyy' 'hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""dd'/'MM'/'yyyy' 'hh':'mm' 'tt"">" + test.ToString("dd'/'MM'/'yyyy' 'hh':'mm' 'tt") + "</option>");

			lblDateTimeFormatTemplates.Text = sb.ToString();
		}

		/// <summary>
		/// Loads the content configuration.
		/// </summary>
		private void LoadContentConfig() {
			PopulateThemes(Settings.GetTheme(null));
			PopulateMainPages(Settings.DefaultPage);
			txtDateTimeFormat.Text = Settings.DateTimeFormat;
			PopulateDateTimeFormats();
			PopulateLanguages(Settings.DefaultLanguage);
			PopulateTimeZones(Settings.DefaultTimezone.ToString());
			txtMaxRecentChangesToDisplay.Text = Settings.MaxRecentChangesToDisplay.ToString();

			lstRssFeedsMode.SelectedIndex = -1;
			switch(Settings.RssFeedsMode) {
				case RssFeedsMode.FullText:
					lstRssFeedsMode.SelectedIndex = 0;
					break;
				case RssFeedsMode.Summary:
					lstRssFeedsMode.SelectedIndex = 1;
					break;
				case RssFeedsMode.Disabled:
					lstRssFeedsMode.SelectedIndex = 2;
					break;
			}

			chkEnableDoubleClickEditing.Checked = Settings.EnableDoubleClickEditing;
			chkEnableSectionEditing.Checked = Settings.EnableSectionEditing;
			chkEnableSectionAnchors.Checked = Settings.EnableSectionAnchors;
			chkEnablePageToolbar.Checked = Settings.EnablePageToolbar;
			chkEnableViewPageCode.Checked = Settings.EnableViewPageCodeFeature;
			chkEnablePageInfoDiv.Checked = Settings.EnablePageInfoDiv;
			chkEnableBreadcrumbsTrail.Checked = !Settings.DisableBreadcrumbsTrail;
			chkAutoGeneratePageNames.Checked = Settings.AutoGeneratePageNames;
			chkProcessSingleLineBreaks.Checked = Settings.ProcessSingleLineBreaks;
			chkUseVisualEditorAsDefault.Checked = Settings.UseVisualEditorAsDefault;
			if(Settings.KeptBackupNumber == -1) txtKeptBackupNumber.Text = "";
			else txtKeptBackupNumber.Text = Settings.KeptBackupNumber.ToString();
			chkDisplayGravatars.Checked = Settings.DisplayGravatars;
			txtListSize.Text = Settings.ListSize.ToString();
		}

		/// <summary>
		/// Populates the activation mode list selecting the current one.
		/// </summary>
		/// <param name="current">The current account activation mode.</param>
		private void PopulateAccountActivationMode(AccountActivationMode current) {
			if(current == AccountActivationMode.Email) lstAccountActivationMode.SelectedIndex = 0;
			else if(current == AccountActivationMode.Administrator) lstAccountActivationMode.SelectedIndex = 1;
			else lstAccountActivationMode.SelectedIndex = 2;
		}

		/// <summary>
		/// Populates the default groups lists, selecting the current ones.
		/// </summary>
		/// <param name="users">The current default users group.</param>
		/// <param name="admins">The current default administrators group.</param>
		/// <param name="anonymous">The current default anonymous users group.</param>
		private void PopulateDefaultGroups(string users, string admins, string anonymous) {
			users = users.ToLowerInvariant();
			admins = admins.ToLowerInvariant();
			anonymous = anonymous.ToLowerInvariant();

			lstDefaultUsersGroup.Items.Clear();
			lstDefaultAdministratorsGroup.Items.Clear();
			lstDefaultAnonymousGroup.Items.Clear();
			foreach(UserGroup group in Users.GetUserGroups()) {
				string lowerName = group.Name.ToLowerInvariant();

				lstDefaultUsersGroup.Items.Add(new ListItem(group.Name, group.Name));
				if(lowerName == users) {
					lstDefaultUsersGroup.SelectedIndex = -1;
					lstDefaultUsersGroup.Items[lstDefaultUsersGroup.Items.Count - 1].Selected = true;
				}

				lstDefaultAdministratorsGroup.Items.Add(new ListItem(group.Name, group.Name));
				if(lowerName == admins) {
					lstDefaultAdministratorsGroup.SelectedIndex = -1;
					lstDefaultAdministratorsGroup.Items[lstDefaultAdministratorsGroup.Items.Count - 1].Selected = true;
				}

				lstDefaultAnonymousGroup.Items.Add(new ListItem(group.Name, group.Name));
				if(lowerName == anonymous) {
					lstDefaultAnonymousGroup.SelectedIndex = -1;
					lstDefaultAnonymousGroup.Items[lstDefaultAnonymousGroup.Items.Count - 1].Selected = true;
				}
			}
		}

		/// <summary>
		/// Loads the security configuration.
		/// </summary>
		private void LoadSecurityConfig() {
			chkAllowUsersToRegister.Checked = Settings.UsersCanRegister;
			txtPasswordRegEx.Text = Settings.PasswordRegex;
			txtUsernameRegEx.Text = Settings.UsernameRegex;
			PopulateAccountActivationMode(Settings.AccountActivationMode);
			PopulateDefaultGroups(Settings.UsersGroup,
				Settings.AdministratorsGroup,
				Settings.AnonymousGroup);
			chkEnableCaptchaControl.Checked = !Settings.DisableCaptchaControl;
			chkPreventConcurrentEditing.Checked = Settings.DisableConcurrentEditing;

			switch(Settings.ChangeModerationMode) {
				case ChangeModerationMode.None:
					rdoNoModeration.Checked = true;
					break;
				case ChangeModerationMode.RequirePageViewingPermissions:
					rdoRequirePageViewingPermissions.Checked = true;
					break;
				case ChangeModerationMode.RequirePageEditingPermissions:
					rdoRequirePageEditingPermissions.Checked = true;
					break;
			}

			txtExtensionsAllowed.Text = string.Join(", ", Settings.AllowedFileTypes);

			lstFileDownloadCountFilterMode.SelectedIndex = -1;
			switch(Settings.FileDownloadCountFilterMode) {
				case FileDownloadCountFilterMode.CountAll:
					lstFileDownloadCountFilterMode.SelectedIndex = 0;
					txtFileDownloadCountFilter.Enabled = false;
					break;
				case FileDownloadCountFilterMode.CountSpecifiedExtensions:
					lstFileDownloadCountFilterMode.SelectedIndex = 1;
					txtFileDownloadCountFilter.Enabled = true;
					txtFileDownloadCountFilter.Text = string.Join(", ", Settings.FileDownloadCountFilter);
					break;
				case FileDownloadCountFilterMode.ExcludeSpecifiedExtensions:
					txtFileDownloadCountFilter.Text = string.Join(", ", Settings.FileDownloadCountFilter);
					txtFileDownloadCountFilter.Enabled = true;
					lstFileDownloadCountFilterMode.SelectedIndex = 2;
					break;
				default:
					throw new NotSupportedException();
			}

			txtMaxFileSize.Text = Settings.MaxFileSize.ToString();
			chkAllowScriptTags.Checked = Settings.ScriptTagsAllowed;
			txtMaxLogSize.Text = Settings.MaxLogSize.ToString();
			txtIpHostFilter.Text = Settings.IpHostFilter;
			switch(Settings.LoggingLevel) {
				case LoggingLevel.DisableLog:
					rdoDisableLog.Checked = true;
					break;
				case LoggingLevel.ErrorsOnly:
					rdoErrorsOnly.Checked = true;
					break;
				case LoggingLevel.WarningsAndErrors:
					rdoWarningsAndErrors.Checked = true;
					break;
				case LoggingLevel.AllMessages:
					rdoAllMessages.Checked = true;
					break;
			}
		}

		/// <summary>
		/// Loads the advanced configuration.
		/// </summary>
		private void LoadAdvancedConfig() {
			chkEnableAutomaticUpdateChecks.Checked = !Settings.DisableAutomaticVersionCheck;
			chkDisableCache.Checked = Settings.DisableCache;
			txtCacheSize.Text = Settings.CacheSize.ToString();
			txtCacheCutSize.Text = Settings.CacheCutSize.ToString();
			chkEnableViewStateCompression.Checked = Settings.EnableViewStateCompression;
			chkEnableHttpCompression.Checked = Settings.EnableHttpCompression;
		}

		protected void btnAutoWikiUrl_Click(object sender, EventArgs e) {
			string url = Tools.GetCurrentUrlFixed();
			// Assume the URL contains AdminConfig.aspx
			url = url.Substring(0, url.ToLowerInvariant().IndexOf("adminconfig.aspx"));
			txtMainUrl.Text = url;
		}

		protected void cvUsername_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = txtUsername.Text.Length == 0 ||
				(txtUsername.Text.Length > 0 && txtPassword.Text.Length > 0);
		}

		protected void cvPassword_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = txtPassword.Text.Length == 0 ||
				(txtUsername.Text.Length > 0 && txtPassword.Text.Length > 0);
		}

		/// <summary>
		/// Gets the errors emails, properly trimmed.
		/// </summary>
		/// <returns>The emails.</returns>
		private string[] GetErrorsEmails() {
			string[] emails = txtErrorsEmails.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			for(int i = 0; i < emails.Length; i++) {
				emails[i] = emails[i].Trim();
			}

			return emails;
		}

		protected void cvErrorsEmails_ServerValidate(object sender, ServerValidateEventArgs e) {
			string[] emails = GetErrorsEmails();

			Regex regex = new Regex(Settings.EmailRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

			foreach(string email in emails) {
				if(!regex.Match(email).Success) {
					e.IsValid = false;
					return;
				}
			}

			e.IsValid = true;
		}

		protected void cvUsernameRegEx_ServerValidate(object sender, ServerValidateEventArgs e) {
			try {
				var r = new Regex(txtUsernameRegEx.Text);
				r.IsMatch("Test String to validate Regular Expression");
				e.IsValid = true;
			}
			catch {
				e.IsValid = false;
			}
		}

		protected void cvPasswordRegEx_ServerValidate(object sender, ServerValidateEventArgs e) {
			try {
				var r = new Regex(txtPasswordRegEx.Text);
				r.IsMatch("Test String to validate Regular Expression");
				e.IsValid = true;
			} 
			catch {
				e.IsValid = false;
			}
		}

		protected void cvDateTimeFormat_ServerValidate(object sender, ServerValidateEventArgs e) {
			try {
				DateTime.Now.ToString(txtDateTimeFormat.Text);
				e.IsValid = true;
			}
			catch {
				e.IsValid = false;
			}
		}

		/// <summary>
		/// Gets the extensions allowed for upload from the input control.
		/// </summary>
		/// <returns>The extensions.</returns>
		private string[] GetAllowedFileExtensions() {
			return txtExtensionsAllowed.Text.Replace(" ", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}

		protected void cvExtensionsAllowed_ServerValidate(object sender, ServerValidateEventArgs e) {
			string[] allowed = GetAllowedFileExtensions();

			bool wildcardFound =
				(from s in allowed
				 where s == "*"
				 select s).Any();

			e.IsValid = allowed.Length <= 1 || allowed.Length > 1 && !wildcardFound;
		}

		protected void lstFileDownloadCountFilterMode_SelectedIndexChanged(object sender, EventArgs e) {
			if(lstFileDownloadCountFilterMode.SelectedValue == FileDownloadCountFilterMode.CountAll.ToString()) {
				txtFileDownloadCountFilter.Enabled = false;
			}
			else {
				txtFileDownloadCountFilter.Enabled = true;
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			lblResult.CssClass = "";
			lblResult.Text = "";

			Page.Validate();

			if(!Page.IsValid) return;

			Log.LogEntry("Wiki Configuration change requested", EntryType.General, SessionFacade.CurrentUsername);

			Settings.BeginBulkUpdate();

			// Save general configuration
			Settings.WikiTitle = txtWikiTitle.Text;
			Settings.MainUrl = txtMainUrl.Text;
			Settings.ContactEmail = txtContactEmail.Text;
			Settings.SenderEmail = txtSenderEmail.Text;
			Settings.ErrorsEmails = GetErrorsEmails();
			Settings.SmtpServer = txtSmtpServer.Text;
			
			txtSmtpPort.Text = txtSmtpPort.Text.Trim();
			if(txtSmtpPort.Text.Length > 0) Settings.SmtpPort = int.Parse(txtSmtpPort.Text);
			else Settings.SmtpPort = -1;
			if(txtUsername.Text.Length > 0) {
				Settings.SmtpUsername = txtUsername.Text;
				Settings.SmtpPassword = txtPassword.Text;
			}
			else {
				Settings.SmtpUsername = "";
				Settings.SmtpPassword = "";
			}
			Settings.SmtpSsl = chkEnableSslForSmtp.Checked;

			// Save content configuration
			Settings.SetTheme(null, lstRootTheme.SelectedValue);
			Settings.DefaultPage = lstMainPage.SelectedValue;
			Settings.DateTimeFormat = txtDateTimeFormat.Text;
			Settings.DefaultLanguage = lstDefaultLanguage.SelectedValue;
			Settings.DefaultTimezone = int.Parse(lstDefaultTimeZone.SelectedValue);
			Settings.MaxRecentChangesToDisplay = int.Parse(txtMaxRecentChangesToDisplay.Text);
			Settings.RssFeedsMode = (RssFeedsMode)Enum.Parse(typeof(RssFeedsMode), lstRssFeedsMode.SelectedValue);
			Settings.EnableDoubleClickEditing = chkEnableDoubleClickEditing.Checked;
			Settings.EnableSectionEditing = chkEnableSectionEditing.Checked;
			Settings.EnableSectionAnchors = chkEnableSectionAnchors.Checked;
			Settings.EnablePageToolbar = chkEnablePageToolbar.Checked;
			Settings.EnableViewPageCodeFeature = chkEnableViewPageCode.Checked;
			Settings.EnablePageInfoDiv = chkEnablePageInfoDiv.Checked;
			Settings.DisableBreadcrumbsTrail = !chkEnableBreadcrumbsTrail.Checked;
			Settings.AutoGeneratePageNames = chkAutoGeneratePageNames.Checked;
			Settings.ProcessSingleLineBreaks = chkProcessSingleLineBreaks.Checked;
			Settings.UseVisualEditorAsDefault = chkUseVisualEditorAsDefault.Checked;
			if(txtKeptBackupNumber.Text == "") Settings.KeptBackupNumber = -1;
			else Settings.KeptBackupNumber = int.Parse(txtKeptBackupNumber.Text);
			Settings.DisplayGravatars = chkDisplayGravatars.Checked;
			Settings.ListSize = int.Parse(txtListSize.Text);

			// Save security configuration
			Settings.UsersCanRegister = chkAllowUsersToRegister.Checked;
			Settings.UsernameRegex = txtUsernameRegEx.Text;
			Settings.PasswordRegex = txtPasswordRegEx.Text;
			AccountActivationMode mode = AccountActivationMode.Email;
			switch(lstAccountActivationMode.SelectedValue.ToLowerInvariant()) {
				case "email":
					mode = AccountActivationMode.Email;
					break;
				case "admin":
					mode = AccountActivationMode.Administrator;
					break;
				case "auto":
					mode = AccountActivationMode.Auto;
					break;
			}
			Settings.AccountActivationMode = mode;
			Settings.UsersGroup = lstDefaultUsersGroup.SelectedValue;
			Settings.AdministratorsGroup = lstDefaultAdministratorsGroup.SelectedValue;
			Settings.AnonymousGroup = lstDefaultAnonymousGroup.SelectedValue;
			Settings.DisableCaptchaControl = !chkEnableCaptchaControl.Checked;
			Settings.DisableConcurrentEditing = chkPreventConcurrentEditing.Checked;

			if(rdoNoModeration.Checked) Settings.ChangeModerationMode = ChangeModerationMode.None;
			else if(rdoRequirePageViewingPermissions.Checked) Settings.ChangeModerationMode = ChangeModerationMode.RequirePageViewingPermissions;
			else if(rdoRequirePageEditingPermissions.Checked) Settings.ChangeModerationMode = ChangeModerationMode.RequirePageEditingPermissions;

			Settings.AllowedFileTypes = GetAllowedFileExtensions();

			Settings.FileDownloadCountFilterMode = (FileDownloadCountFilterMode)Enum.Parse(typeof(FileDownloadCountFilterMode), lstFileDownloadCountFilterMode.SelectedValue);
			Settings.FileDownloadCountFilter = txtFileDownloadCountFilter.Text.Replace(" ", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			Settings.MaxFileSize = int.Parse(txtMaxFileSize.Text);
			Settings.ScriptTagsAllowed = chkAllowScriptTags.Checked;
			LoggingLevel level = LoggingLevel.AllMessages;
			if(rdoAllMessages.Checked) level = LoggingLevel.AllMessages;
			else if(rdoWarningsAndErrors.Checked) level = LoggingLevel.WarningsAndErrors;
			else if(rdoErrorsOnly.Checked) level = LoggingLevel.ErrorsOnly;
			else level = LoggingLevel.DisableLog;
			Settings.LoggingLevel = level;
			Settings.MaxLogSize = int.Parse(txtMaxLogSize.Text);
			Settings.IpHostFilter = txtIpHostFilter.Text;

			// Save advanced configuration
			Settings.DisableAutomaticVersionCheck = !chkEnableAutomaticUpdateChecks.Checked;
			Settings.DisableCache = chkDisableCache.Checked;
			Settings.CacheSize = int.Parse(txtCacheSize.Text);
			Settings.CacheCutSize = int.Parse(txtCacheCutSize.Text);
			Settings.EnableViewStateCompression = chkEnableViewStateCompression.Checked;
			Settings.EnableHttpCompression = chkEnableHttpCompression.Checked;

			Settings.EndBulkUpdate();

			Content.InvalidateAllPages();
			Content.ClearPseudoCache();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ConfigSaved;
		}

	}

}
