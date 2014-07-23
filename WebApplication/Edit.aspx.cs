
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace ScrewTurn.Wiki {

	public partial class Edit : BasePage {

		private PageInfo currentPage = null;
		private PageContent currentContent = null;
		private bool isDraft = false;
		private int currentSection = -1;

		private bool canEdit = false;
		private bool canEditWithApproval = false;
		private bool canCreateNewPages = false;
		private bool canCreateNewCategories = false;
		private bool canManagePageCategories = false;
		private bool canDownloadAttachments = false;

		/// <summary>
		/// Detects the permissions for the current user.
		/// </summary>
		/// <remarks><b>currentPage</b> should be set before calling this method.</remarks>
		private void DetectPermissions() {
			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			if(currentPage != null) {
				Pages.CanEditPage(currentPage, currentUser, currentGroups, out canEdit, out canEditWithApproval);
				canCreateNewPages = false; // Least privilege
				canCreateNewCategories = AuthChecker.CheckActionForNamespace(Pages.FindNamespace(NameTools.GetNamespace(currentPage.FullName)),
					Actions.ForNamespaces.ManageCategories, currentUser, currentGroups);
				canManagePageCategories = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.ManageCategories, currentUser, currentGroups);
				canDownloadAttachments = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.DownloadAttachments, currentUser, currentGroups);
			}
			else {
				NamespaceInfo ns = DetectNamespaceInfo();
				canCreateNewPages = AuthChecker.CheckActionForNamespace(ns, Actions.ForNamespaces.CreatePages, currentUser, currentGroups);
				canCreateNewCategories = AuthChecker.CheckActionForNamespace(ns, Actions.ForNamespaces.ManageCategories, currentUser, currentGroups);
				canManagePageCategories = canCreateNewCategories;
				canDownloadAttachments = AuthChecker.CheckActionForNamespace(ns, Actions.ForNamespaces.DownloadAttachments, currentUser, currentGroups);
			}
		}

		protected void Page_Load(object sender, EventArgs e) {

			Page.Title = Properties.Messages.EditTitle + " - " + Settings.WikiTitle;

			lblEditNotice.Text = Formatter.FormatPhase3(Formatter.Format(Settings.Provider.GetMetaDataItem(
				MetaDataItem.EditNotice, DetectNamespace()), false, FormattingContext.Other, null), FormattingContext.Other, null);

			// Prepare page unload warning
			string ua = Request.UserAgent;
			if(!string.IsNullOrEmpty(ua)) {
				ua = ua.ToLowerInvariant();
				StringBuilder sbua = new StringBuilder(50);
				sbua.Append(@"<script type=""text/javascript"">");
				sbua.Append("\r\n<!--\r\n");
				if(ua.Contains("gecko")) {
					// Mozilla
					sbua.Append("addEventListener('beforeunload', __UnloadPage, true);");
				}
				else {
					// IE
					sbua.Append("window.attachEvent('onbeforeunload', __UnloadPage);");
				}
				sbua.Append("\r\n// -->\r\n");
				sbua.Append("</script>");
				lblUnloadPage.Text = sbua.ToString();
			}

			if(!Page.IsPostBack) {
				PopulateCategories(new CategoryInfo[0]);

				if(Settings.AutoGeneratePageNames) {
					pnlPageName.Visible = false;
					pnlManualName.Visible = true;
				}
			}

			// Load requested page, if any
			if(Request["Page"] != null || Page.IsPostBack) {
				string name = null;
				if(Request["Page"] != null) {
					name = Request["Page"];
				}
				else {
					name = txtName.Text;
				}

				currentPage = Pages.FindPage(name);

				// If page already exists, load the content and disable page name,
				// otherwise pre-fill page name
				if(currentPage != null) {
					// Look for a draft
					currentContent = Pages.GetDraft(currentPage);

					if(currentContent == null) {
						// No cache because the page will be probably modified in a few minutes
						currentContent = Content.GetPageContent(currentPage, false);
					}
					else isDraft = true;

					// Set current page for editor and attachment manager
					editor.CurrentPage = currentPage;
					attachmentManager.CurrentPage = currentPage;

					if(!int.TryParse(Request["Section"], out currentSection)) currentSection = -1;

					// Fill data, if not posted back
					if(!Page.IsPostBack) {
						// Set keywords, description
						SetKeywords(currentContent.Keywords);
						txtDescription.Text = currentContent.Description;

						txtName.Text = NameTools.GetLocalName(currentPage.FullName);
						txtName.Enabled = false;
						pnlPageName.Visible = false;
						pnlManualName.Visible = false;

						PopulateCategories(Pages.GetCategoriesForPage(currentPage));

						txtTitle.Text = currentContent.Title;

						// Manage section, if appropriate (disable if draft)
						if(!isDraft && currentSection != -1) {
							int startIndex, len;
							string dummy = "";
							ExtractSection(currentContent.Content, currentSection, out startIndex, out len, out dummy);
							editor.SetContent(currentContent.Content.Substring(startIndex, len), Settings.UseVisualEditorAsDefault);
						}
						else {
							// Select default editor view (WikiMarkup or Visual) and populate content
							editor.SetContent(currentContent.Content, Settings.UseVisualEditorAsDefault);
						}
					}
				}
				else {
					// Pre-fill name, if not posted back
					if(!Page.IsPostBack) {
						// Set both name and title, as the NAME was provided from the query-string and must be preserved
						pnlPageName.Visible = true;
						pnlManualName.Visible = false;
						txtName.Text = NameTools.GetLocalName(name);
						txtTitle.Text = txtName.Text;
						editor.SetContent(LoadTemplateIfAppropriate(), Settings.UseVisualEditorAsDefault);
					}
				}
			}
			else {
				if(!Page.IsPostBack) {
					chkMinorChange.Visible = false;
					chkSaveAsDraft.Visible = false;

					editor.SetContent(LoadTemplateIfAppropriate(), Settings.UseVisualEditorAsDefault);
				}
			}

			// Here is centralized all permissions-checking code
			DetectPermissions();

			// Verify the following permissions:
			// - if new page, check for page creation perms
			// - else, check for editing perms
			//    - full edit or edit with approval
			// - categories management
			// - attachment manager
			// - CAPTCHA if enabled and user is anonymous
			// ---> recheck every time an action is performed

			if(currentPage == null) {
				// Check permissions for creating new pages
				if(!canCreateNewPages) {
					if(SessionFacade.LoginKey == null) UrlTools.Redirect("Login.aspx?Redirect=" + Tools.UrlEncode(Tools.GetCurrentUrlFixed()));
					else UrlTools.Redirect("AccessDenied.aspx");
				}
			}
			else {
				// Check permissions for editing current page
				if(!canEdit && !canEditWithApproval) {
					if(SessionFacade.LoginKey == null) UrlTools.Redirect("Login.aspx?Redirect=" + Tools.UrlEncode(Tools.GetCurrentUrlFixed()));
					else UrlTools.Redirect("AccessDenied.aspx");
				}
			}

			if(!canEdit && canEditWithApproval) {
				// Hard-wire status of draft and minor change checkboxes
				chkMinorChange.Enabled = false;
				chkSaveAsDraft.Enabled = false;
				chkSaveAsDraft.Checked = true;
			}

			// Setup categories
			lstCategories.Enabled = canManagePageCategories;
			pnlCategoryCreation.Visible = canCreateNewCategories;

			// Setup attachment manager (require at least download permissions)
			attachmentManager.Visible = canDownloadAttachments;

			// CAPTCHA
			pnlCaptcha.Visible = SessionFacade.LoginKey == null && !Settings.DisableCaptchaControl;
			captcha.Visible = pnlCaptcha.Visible;

			// Moderation notice
			pnlApprovalRequired.Visible = !canEdit && canEditWithApproval;

			// Check and manage editing collisions
			ManageEditingCollisions();

			if(!Page.IsPostBack) {
				ManageTemplatesDisplay();

				// Display draft status
				ManageDraft();
			}

			// Setup session refresh iframe
			PrintSessionRefresh();
		}

		/// <summary>
		/// Manages the display of the template selection controls.
		/// </summary>
		private void ManageTemplatesDisplay() {
			// Hide templates selection if there aren't any or if the editor is not in WikiMarkup mode
			if(Templates.GetTemplates().Count == 0 || !editor.IsInWikiMarkup()) {
				btnTemplates.Visible = false;
				pnlTemplates.Visible = false;
			}
			else btnTemplates.Visible = true;
		}

		protected void editor_SelectedTabChanged(object sender, SelectedTabChangedEventArgs e) {
			ManageTemplatesDisplay();
		}

		/// <summary>
		/// Loads a content template when the query strings specifies it.
		/// </summary>
		/// <returns>The content of the selected template.</returns>
		private string LoadTemplateIfAppropriate() {
			if(string.IsNullOrEmpty(Request["Template"])) return "";
			ContentTemplate template = Templates.Find(Request["Template"]);
			if(template == null) return "";
			else {
				lblAutoTemplate.Text = lblAutoTemplate.Text.Replace("##TEMPLATE##", template.Name);
				pnlAutoTemplate.Visible = true;
				return template.Content;
			}
		}

		protected void btnAutoTemplateOK_Click(object sender, EventArgs e) {
			pnlAutoTemplate.Visible = false;
		}

		/// <summary>
		/// Prints the session refresh code in the page.
		/// </summary>
		public void PrintSessionRefresh() {
			StringBuilder sb = new StringBuilder(50);
			sb.Append(@"<iframe src=""");
			if(currentPage != null) sb.AppendFormat("SessionRefresh.aspx?Page={0}", Tools.UrlEncode(currentPage.FullName));
			else sb.Append("SessionRefresh.aspx");
			sb.Append(@""" style=""width: 1px; height: 1px; border: none;"" scrolling=""no""></iframe>");

			lblSessionRefresh.Text = sb.ToString();
		}

		/// <summary>
		/// Verifies for editing collisions, and if no collision is found, "locks" the page
		/// </summary>
		private void ManageEditingCollisions() {
			if(currentPage == null) return;

			lblRefreshLink.Text = @"<a href=""" +
				UrlTools.BuildUrl("Edit.aspx?Page=", Tools.UrlEncode(currentPage.FullName), (Request["Section"] != null ? "&amp;Section=" + currentSection.ToString() : "")) +
				@""">" + Properties.Messages.Refresh + " &raquo;</a>";

			string username = Request.UserHostAddress;
			if(SessionFacade.LoginKey != null) username = SessionFacade.CurrentUsername;

			if(Collisions.IsPageBeingEdited(currentPage, username)) {
				pnlCollisions.Visible = true;
				lblConcurrentEditingUsername.Text = "(" + Users.UserLink(Collisions.WhosEditing(currentPage)) + ")";
				if(Settings.DisableConcurrentEditing) {
					lblSaveDisabled.Visible = true;
					lblSaveDangerous.Visible = false;
					btnSave.Enabled = false;
					btnSaveAndContinue.Enabled = false;
				}
				else {
					lblSaveDisabled.Visible = false;
					lblSaveDangerous.Visible = true;
					btnSave.Enabled = true;
					btnSaveAndContinue.Enabled = true;
				}
			}
			else {
				pnlCollisions.Visible = false;
				btnSave.Enabled = true;
				btnSaveAndContinue.Enabled = true;
				Collisions.RenewEditingSession(currentPage, username);
			}
		}

		/// <summary>
		/// Manages the draft status display.
		/// </summary>
		private void ManageDraft() {
			if(isDraft) {
				chkSaveAsDraft.Checked = true;
				chkMinorChange.Enabled = false;
				pnlDraft.Visible = true;
				lblDraftInfo.Text = lblDraftInfo.Text.Replace("##USER##",
					Users.UserLink(currentContent.User, true)).Replace("##DATETIME##",
					Preferences.AlignWithTimezone(currentContent.LastModified).ToString(Settings.DateTimeFormat)).Replace("##VIEWCHANGES##",
					string.Format("<a href=\"{0}\" target=\"_blank\">{1}</a>", UrlTools.BuildUrl("Diff.aspx?Page=",
					Tools.UrlEncode(currentPage.FullName), "&amp;Rev1=Current&amp;Rev2=Draft"),
					Properties.Messages.ViewChanges));
			}
			else {
				pnlDraft.Visible = false;
			}
		}

		/// <summary>
		/// Populates the categories for the current namespace and provider, selecting the ones specified.
		/// </summary>
		/// <param name="toSelect">The categories to select.</param>
		private void PopulateCategories(CategoryInfo[] toSelect) {
			IPagesStorageProviderV30 provider = FindAppropriateProvider();
			List<CategoryInfo> cats = Pages.GetCategories(DetectNamespaceInfo());
			lstCategories.Items.Clear();
			foreach(CategoryInfo c in cats) {
				if(c.Provider == provider) {
					ListItem itm = new ListItem(NameTools.GetLocalName(c.FullName), c.FullName);
					if(Array.Find<CategoryInfo>(toSelect, delegate(CategoryInfo s) { return s.FullName == c.FullName; }) != null)
						itm.Selected = true;
					lstCategories.Items.Add(itm);
				}
			}
		}

		protected void btnManualName_Click(object sender, EventArgs e) {
			pnlPageName.Visible = true;
			pnlManualName.Visible = false;
			txtName.Text = GenerateAutoName(txtTitle.Text);
			pnlManualName.UpdateAfterCallBack = true;
			pnlPageName.UpdateAfterCallBack = true;
		}

		/// <summary>
		/// Generates an automatic page name.
		/// </summary>
		/// <param name="title">The page title.</param>
		/// <returns>The name.</returns>
		private static string GenerateAutoName(string title) {
			// Replace all non-alphanumeric characters with dashes
			if(title.Length == 0) return "";

			StringBuilder buffer = new StringBuilder(title.Length);

			foreach(char ch in title.Normalize(NormalizationForm.FormD).Replace("\"", "").Replace("'", "")) {
				var unicat = char.GetUnicodeCategory(ch);
				if(unicat == System.Globalization.UnicodeCategory.LowercaseLetter ||
					unicat == System.Globalization.UnicodeCategory.UppercaseLetter ||
					unicat == System.Globalization.UnicodeCategory.DecimalDigitNumber) {
					buffer.Append(ch);
				}
				else if(unicat != System.Globalization.UnicodeCategory.NonSpacingMark) buffer.Append("-");
			}

			while(buffer.ToString().IndexOf("--") >= 0) {
				buffer.Replace("--", "-");
			}

			return buffer.ToString().Trim('-');
		}

		// This regex is duplicated from Formatter.cs
		private static readonly Regex FullCodeRegex = new Regex(@"@@.+?@@", RegexOptions.Compiled | RegexOptions.Singleline);

		/// <summary>
		/// Finds the start and end positions of a section of the content.
		/// </summary>
		/// <param name="content">The content.</param>
		/// <param name="section">The section ID.</param>
		/// <param name="start">The index of the first character of the section.</param>
		/// <param name="len">The length of the section.</param>
		/// <param name="anchor">The anchor ID of the section.</param>
		private static void ExtractSection(string content, int section, out int start, out int len, out string anchor) {
			// HACK: @@...@@ escapes headers: must reproduce behavior here
			Match m = FullCodeRegex.Match(content);
			while(m.Success) {
				string newContent = m.Value.Replace("=", "$"); // Do not alter positions
				content = content.Remove(m.Index, m.Length);
				content = content.Insert(m.Index, newContent);
				m = FullCodeRegex.Match(content, m.Index + m.Length - 1);
			}

			List<HPosition> hPos = Formatter.DetectHeaders(content);
			start = 0;
			len = content.Length;
			anchor = "";
			int level = -1;
			bool found = false;
			for(int i = 0; i < hPos.Count; i++) {
				if(hPos[i].ID == section) {
					start = hPos[i].Index;
					len = len - start;
					level = hPos[i].Level; // Level is used to edit the current section AND all the subsections
					// Set the anchor value so that it's possible to redirect the user to the just edited section
					anchor = Formatter.BuildHAnchor(hPos[i].Text);
					found = true;
					break;
				}
			}
			if(found) {
				int diff = len;
				for(int i = 0; i < hPos.Count; i++) {
					if(hPos[i].Index > start && // Next section (Hx)
						hPos[i].Index - start < diff && // The nearest section
						hPos[i].Level <= level) { // Of the same level or higher
						len = hPos[i].Index - start - 1;
						diff = hPos[i].Index - start;
					}
				}
			}
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			if(currentPage == null && txtName.Visible) currentPage = Pages.FindPage(NameTools.GetFullName(DetectNamespace(), txtName.Text));
			if(currentPage != null) {
				// Try redirecting to proper section
				string anchor = null;
				if(currentSection != -1) {
					int start, len;
					ExtractSection(Content.GetPageContent(currentPage, true).Content, currentSection, out start, out len, out anchor);
				}

				UrlTools.Redirect(Tools.UrlEncode(currentPage.FullName) + Settings.PageExtension + (anchor != null ? ("#" + anchor + "_" + currentSection.ToString()) : ""));
			}
			else UrlTools.Redirect(UrlTools.BuildUrl("Default.aspx"));
		}

		protected void cvName1_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = !txtName.Enabled || Pages.IsValidName(txtName.Text);
		}

		protected void cvName2_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = !txtName.Enabled || Pages.FindPage(NameTools.GetFullName(DetectNamespace(), txtName.Text)) == null;
		}

		protected void chkMinorChange_CheckedChanged(object sender, EventArgs e) {
			if(chkMinorChange.Checked) {
				// Save as draft is not available
				chkSaveAsDraft.Checked = false;
				chkSaveAsDraft.Enabled = false;
			}
			else {
				chkSaveAsDraft.Enabled = true;
			}
		}

		protected void chkSaveAsDraft_CheckedChanged(object sender, EventArgs e) {
			if(chkSaveAsDraft.Checked) {
				// Minor change is not available
				chkMinorChange.Checked = false;
				chkMinorChange.Enabled = false;
			}
			else {
				chkMinorChange.Enabled = true;
			}
		}

		/// <summary>
		/// Finds the appropriate provider to use for operations.
		/// </summary>
		/// <returns>The provider.</returns>
		private IPagesStorageProviderV30 FindAppropriateProvider() {
			IPagesStorageProviderV30 provider = null;

			if(currentPage != null) provider = currentPage.Provider;
			else {
				NamespaceInfo currentNamespace = DetectNamespaceInfo();
				provider =
					currentNamespace == null ?
					Collectors.PagesProviderCollector.GetProvider(Settings.DefaultPagesProvider) :
					currentNamespace.Provider;
			}

			return provider;
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			bool wasVisible = pnlPageName.Visible;
			pnlPageName.Visible = true;

			if(!wasVisible && Settings.AutoGeneratePageNames && txtName.Enabled) {
				txtName.Text = GenerateAutoName(txtTitle.Text);
			}

			txtName.Text = txtName.Text.Trim();

			Page.Validate("nametitle");
			Page.Validate("captcha");
			if(!Page.IsValid) {
				if(!rfvTitle.IsValid || !rfvName.IsValid || !cvName1.IsValid || !cvName2.IsValid) {
					pnlPageName.Visible = true;
					pnlManualName.Visible = false;
					pnlPageName.UpdateAfterCallBack = true;
					pnlManualName.UpdateAfterCallBack = true;
				}

				return;
			}

			pnlPageName.Visible = wasVisible;

			// Check permissions
			if(currentPage == null) {
				// Check permissions for creating new pages
				if(!canCreateNewPages) UrlTools.Redirect("AccessDenied.aspx");
			}
			else {
				// Check permissions for editing current page
				if(!canEdit && !canEditWithApproval) UrlTools.Redirect("AccessDenied.aspx");
			}

			chkMinorChange.Visible = true;
			chkSaveAsDraft.Visible = true;

			// Verify edit with approval
			if(!canEdit && canEditWithApproval) {
				chkSaveAsDraft.Checked = true;
			}

			// Check for scripts (Administrators can always add SCRIPT tags)
			if(!SessionFacade.GetCurrentGroupNames().Contains(Settings.AdministratorsGroup) && !Settings.ScriptTagsAllowed) {
				Regex r = new Regex(@"\<script.*?\>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
				if(r.Match(editor.GetContent()).Success) {
					lblResult.Text = @"<span style=""color: #FF0000;"">" + Properties.Messages.ScriptDetected + "</span>";
					return;
				}
			}

			bool redirect = true;
			if(sender == btnSaveAndContinue) redirect = false;

			lblResult.Text = "";
			lblResult.CssClass = "";

			string username = "";
			if(SessionFacade.LoginKey == null) username = Request.UserHostAddress;
			else username = SessionFacade.CurrentUsername;

			IPagesStorageProviderV30 provider = FindAppropriateProvider();

			// Create list of selected categories
			List<CategoryInfo> categories = new List<CategoryInfo>();
			for(int i = 0; i < lstCategories.Items.Count; i++) {
				if(lstCategories.Items[i].Selected) {
					CategoryInfo cat = Pages.FindCategory(lstCategories.Items[i].Value);

					// Sanity check
					if(cat.Provider == provider) categories.Add(cat);
				}
			}

			txtComment.Text = txtComment.Text.Trim();
			txtDescription.Text = txtDescription.Text.Trim();

			SaveMode saveMode = SaveMode.Backup;
			if(chkSaveAsDraft.Checked) saveMode = SaveMode.Draft;
			if(chkMinorChange.Checked) saveMode = SaveMode.Normal;

			if(txtName.Enabled) {
				// Find page, if inexistent create it
				PageInfo pg = Pages.FindPage(NameTools.GetFullName(DetectNamespace(), txtName.Text), provider);
				if(pg == null) {
					Pages.CreatePage(DetectNamespaceInfo(), txtName.Text, provider);
					pg = Pages.FindPage(NameTools.GetFullName(DetectNamespace(), txtName.Text), provider);
					saveMode = SaveMode.Normal;
					attachmentManager.CurrentPage = pg;
				}
				Log.LogEntry("Page update requested for " + txtName.Text, EntryType.General, username);

				Pages.ModifyPage(pg, txtTitle.Text, username, DateTime.Now, txtComment.Text, editor.GetContent(),
					GetKeywords(), txtDescription.Text, saveMode);

				// Save categories binding
				Pages.Rebind(pg, categories.ToArray());

				// If not a draft, remove page draft
				if(saveMode != SaveMode.Draft) {
					Pages.DeleteDraft(currentPage);
					isDraft = false;
				}
				else isDraft = true;

				ManageDraft();

				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.PageSaved;

				// This is a new page, so only who has page management permissions can execute this code
				// No notification must be sent for drafts awaiting approval
				if(redirect) {
					Collisions.CancelEditingSession(pg, username);
					string target = UrlTools.BuildUrl(Tools.UrlEncode(txtName.Text), Settings.PageExtension, "?NoRedirect=1");
					UrlTools.Redirect(target);
				}
				else {
					// Disable PageName, because the name cannot be changed anymore
					txtName.Enabled = false;
					pnlManualName.Visible = false;
				}
			}
			else {
				// Used for redirecting to a specific section after editing it
				string anchor = "";

				if(currentPage == null) currentPage = Pages.FindPage(NameTools.GetFullName(DetectNamespace(), txtName.Text));

				// Save data
				Log.LogEntry("Page update requested for " + currentPage.FullName, EntryType.General, username);
				if(!isDraft && currentSection != -1) {
					PageContent cont = Content.GetPageContent(currentPage, false);
					StringBuilder sb = new StringBuilder(cont.Content.Length);
					int start, len;
					ExtractSection(cont.Content, currentSection, out start, out len, out anchor);
					if(start > 0) sb.Append(cont.Content.Substring(0, start));
					sb.Append(editor.GetContent());
					if(start + len < cont.Content.Length - 1) sb.Append(cont.Content.Substring(start + len));
					Pages.ModifyPage(currentPage, txtTitle.Text, username, DateTime.Now, txtComment.Text, sb.ToString(),
						GetKeywords(), txtDescription.Text, saveMode);
				}
				else {
					Pages.ModifyPage(currentPage, txtTitle.Text, username, DateTime.Now, txtComment.Text, editor.GetContent(),
						GetKeywords(), txtDescription.Text, saveMode);
				}
				
				// Save Categories binding
				Pages.Rebind(currentPage, categories.ToArray());

				// If not a draft, remove page draft
				if(saveMode != SaveMode.Draft) {
					Pages.DeleteDraft(currentPage);
					isDraft = false;
				}
				else isDraft = true;

				ManageDraft();

				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.PageSaved;

				// This code is executed every time the page is saved, even when "Save & Continue" is clicked
				// This causes a draft approval notification to be sent multiple times for the same page,
				// but this is the only solution because the user might navigate away from the page after
				// clicking "Save & Continue" but not "Save" or "Cancel" - in other words, it is necessary
				// to take every chance to send a notification because no more chances might be available
				if(!canEdit && canEditWithApproval) {
					Pages.SendEmailNotificationForDraft(currentPage, txtTitle.Text, txtComment.Text, username);
				}

				if(redirect) {
					Collisions.CancelEditingSession(currentPage, username);
					string target = UrlTools.BuildUrl(Tools.UrlEncode(currentPage.FullName), Settings.PageExtension, "?NoRedirect=1",
						(!string.IsNullOrEmpty(anchor) ? ("#" + anchor + "_" + currentSection.ToString()) : ""));
					UrlTools.Redirect(target);
				}
			}
		}

		/// <summary>
		/// Gets the keywords entered in the appropriate textbox.
		/// </summary>
		/// <returns>The keywords.</returns>
		private string[] GetKeywords() {
			var keywords = txtKeywords.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			keywords =
				(from k in keywords
				 select k.Trim())
				 .Distinct(StringComparer.OrdinalIgnoreCase)
				 .ToArray();

			return keywords;
		}

		/// <summary>
		/// Sets a set of keywords in the appropriate textbox.
		/// </summary>
		/// <param name="keywords">The keywords.</param>
		private void SetKeywords(string[] keywords) {
			if(keywords == null || keywords.Length == 0) txtKeywords.Text = "";

			StringBuilder sb = new StringBuilder(50);
			for(int i = 0; i < keywords.Length; i++) {
				sb.Append(keywords[i]);
				if(i != keywords.Length - 1) sb.Append(",");
			}
			txtKeywords.Text = sb.ToString();
		}

		protected void cvCategory1_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtCategory.Text);
		}

		protected void cvCategory2_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.FindCategory(NameTools.GetFullName(DetectNamespace(), txtCategory.Text)) == null;
		}

		protected void btnCreateCategory_Click(object sender, EventArgs e) {
			if(canManagePageCategories) {
				lblCategoryResult.Text = "";
				lblCategoryResult.CssClass = "";

				txtCategory.Text = txtCategory.Text.Trim();

				Page.Validate("category");
				if(!Page.IsValid) return;

				string fullName = NameTools.GetFullName(DetectNamespace(), txtCategory.Text);
				Pages.CreateCategory(DetectNamespaceInfo(), txtCategory.Text, FindAppropriateProvider());

				// Save selected categories
				List<CategoryInfo> selected = new List<CategoryInfo>();
				for(int i = 0; i < lstCategories.Items.Count; i++) {
					if(lstCategories.Items[i].Selected) {
						selected.Add(Pages.FindCategory(lstCategories.Items[i].Value));
					}
				}

				PopulateCategories(selected.ToArray());

				// Re-select previously selected categories
				for(int i = 0; i < lstCategories.Items.Count; i++) {
					if(selected.Find(delegate(CategoryInfo c) { return c.FullName == lstCategories.Items[i].Value; }) != null) {
						lstCategories.Items[i].Selected = true;
					}
					if(lstCategories.Items[i].Value == fullName) lstCategories.Items[i].Selected = true;
				}
				txtCategory.Text = "";
			}
		}

		protected void btnTemplates_Click(object sender, EventArgs e) {
			pnlTemplates.Visible = true;
			btnTemplates.Visible = false;
			pnlAutoTemplate.Visible = false;

			// Load templates
			lstTemplates.Items.Clear();
			lstTemplates.Items.Add(new ListItem(Properties.Messages.SelectTemplate, ""));
			foreach(ContentTemplate temp in Templates.GetTemplates()) {
				lstTemplates.Items.Add(new ListItem(temp.Name, temp.Name));
			}
			// Hide select button and preview text because the user hasn't selected a template yet
			btnUseTemplate.Visible = false;
			lblTemplatePreview.Text = "";
		}

		protected void lstTemplates_SelectedIndexChanged(object sender, EventArgs e) {
			ContentTemplate template = Templates.Find(lstTemplates.SelectedValue);

			if(template != null) {
				lblTemplatePreview.Text = template.Content;
				btnUseTemplate.Visible = true;
			}
			else {
				lblTemplatePreview.Text = "";
				btnUseTemplate.Visible = false;
			}
		}

		protected void btnUseTemplate_Click(object sender, EventArgs e) {
			ContentTemplate template = Templates.Find(lstTemplates.SelectedValue);

			editor.SetContent(template.Content, Settings.UseVisualEditorAsDefault);
			btnCancelTemplate_Click(sender, e);
			// If there's a category matching the selected template name, select it automatically
			for (int i = 0; i < lstCategories.Items.Count; i++)	{
				if(lstCategories.Items[i].Value.ToLowerInvariant().Trim() == lstTemplates.SelectedValue.ToLowerInvariant().Trim()) {
					lstCategories.Items[i].Selected = true;
				}
			}
		}

		protected void btnCancelTemplate_Click(object sender, EventArgs e) {
			pnlTemplates.Visible = false;
			btnTemplates.Visible = true;
		}

	}

}
