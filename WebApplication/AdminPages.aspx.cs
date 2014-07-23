
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminPages : BasePage {

		/// <summary>
		/// The numer of items in a page.
		/// </summary>
		public int PageSize = 50;

		private IList<PageInfo> currentPages = null;

		private int rangeBegin = 0;
		private int rangeEnd = 49;
		private int selectedPage = 0;

		private PageInfo externallySelectedPage = null;

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();
			PageSize = Settings.ListSize;
			rangeEnd = PageSize - 1;

			if(!Page.IsPostBack) {
				// Load namespaces

				// Add root namespace
				lstNamespace.Items.Add(new ListItem("<root>", ""));

				List<NamespaceInfo> namespaces = Pages.GetNamespaces();

				foreach(NamespaceInfo ns in namespaces) {
					lstNamespace.Items.Add(new ListItem(ns.Name, ns.Name));
				}

				bool loaded = LoadExternallySelectedPage();

				if(!loaded) {
					// Load pages
					ResetPageList();
					rptPages.DataBind();
				}
			}
		}

		/// <summary>
		/// Resets the page list.
		/// </summary>
		private void ResetPageList() {
			currentPages = GetPages();
			pageSelector.ItemCount = currentPages.Count;
			pageSelector.SelectPage(0);
		}

		/// <summary>
		/// Gets the pages in the selected namespace.
		/// </summary>
		/// <returns>The pages.</returns>
		private IList<PageInfo> GetPages() {
			NamespaceInfo nspace = Pages.FindNamespace(lstNamespace.SelectedValue);
			List<PageInfo> pages = Pages.GetPages(nspace);
			var orphanPages = Pages.GetOrphanedPages(pages);

			List<PageInfo> result = new List<PageInfo>(pages.Count);

			string filter = txtFilter.Text.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture);

			foreach(PageInfo page in pages) {
				if(NameTools.GetLocalName(page.FullName).ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains(filter)) {
					if(chkOrphansOnly.Checked && !orphanPages.Contains(page.FullName)) continue;
					else result.Add(page);
				}
			}

			result.Sort(new PageNameComparer());

			return result;
		}

		/// <summary>
		/// Loads the externally selected page.
		/// </summary>
		/// <returns><c>true</c> if a page was loaded, <c>false</c> otherwise.</returns>
		private bool LoadExternallySelectedPage() {
			if(!string.IsNullOrEmpty(Request["Rollback"])) {
				externallySelectedPage = Pages.FindPage(Request["Rollback"]);
				if(externallySelectedPage != null) {
					AutoRollback();
					return true;
				}
			}

			if(!string.IsNullOrEmpty(Request["Admin"])) {
				externallySelectedPage = Pages.FindPage(Request["Admin"]);
				if(externallySelectedPage != null) {
					txtCurrentPage.Value = externallySelectedPage.FullName;
					ActivatePageEditor();
					return true;
				}
			}

			if(!string.IsNullOrEmpty(Request["Perms"])) {
				externallySelectedPage = Pages.FindPage(Request["Perms"]);
				if(externallySelectedPage != null) {
					txtCurrentPage.Value = externallySelectedPage.FullName;
					ActivatePagePermissionsManager();
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Rolls back the externally selected page to the previous version.
		/// </summary>
		private void AutoRollback() {
			List<int> backups = Pages.GetBackups(externallySelectedPage);
			if(backups.Count > 0) {
				int targetRevision = backups[backups.Count - 1];

				Log.LogEntry("Page rollback requested for " + txtCurrentPage.Value + " to rev. " + targetRevision.ToString(), EntryType.General, SessionFacade.GetCurrentUsername());

				Pages.Rollback(externallySelectedPage, targetRevision);

				UrlTools.Redirect(externallySelectedPage.FullName + Settings.PageExtension);
			}
		}

		protected void btnNewPage_Click(object sender, EventArgs e) {
			// Redirect to the edit page, keeping the correct namespace
			string currentNamespace = lstNamespace.SelectedValue;
			if(!string.IsNullOrEmpty(currentNamespace)) currentNamespace += ".";
			Response.Redirect(currentNamespace + "Edit.aspx");
		}

		protected void pageSelector_SelectedPageChanged(object sender, SelectedPageChangedEventArgs e) {
			rangeBegin = e.SelectedPage * PageSize;
			rangeEnd = rangeBegin + e.ItemCount - 1;
			selectedPage = e.SelectedPage;

			rptPages.DataBind();
		}

		protected void lstNamespace_SelectedIndexChanged(object sender, EventArgs e) {
			currentPages = GetPages();
			pageSelector.ItemCount = currentPages.Count;
			pageSelector.SelectPage(0);

			rptPages.DataBind();

			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			bool canManageAllPages = AuthChecker.CheckActionForNamespace(
				Pages.FindNamespace(lstNamespace.SelectedValue),
				Actions.ForNamespaces.ManagePages, currentUser, currentGroups);

			btnBulkMigrate.Enabled = canManageAllPages;
		}

		protected void btnFilter_Click(object sender, EventArgs e) {
			currentPages = GetPages();
			pageSelector.ItemCount = currentPages.Count;
			pageSelector.SelectPage(0);

			rptPages.DataBind();
		}

		protected void rdoFilter_CheckedChanged(object sender, EventArgs e) {
			currentPages = GetPages();
			pageSelector.ItemCount = currentPages.Count;
			pageSelector.SelectPage(0);

			rptPages.DataBind();
		}

		protected void rptPages_DataBinding(object sender, EventArgs e) {
			if(currentPages == null) currentPages = GetPages();
			NamespaceInfo nspace = DetectNamespaceInfo();

			List<PageRow> result = new List<PageRow>(PageSize);

			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			bool canSetPermissions = AdminMaster.CanManagePermissions(currentUser, currentGroups);
			bool canDeletePages = AuthChecker.CheckActionForNamespace(nspace, Actions.ForNamespaces.DeletePages, currentUser, currentGroups);

			var orphanPages = Pages.GetOrphanedPages(currentPages);

			for(int i = rangeBegin; i <= rangeEnd; i++) {
				PageInfo page = currentPages[i];

				PageContent currentContent = Content.GetPageContent(page, false);

				// The page can be selected if the user can either manage or delete the page or manage the discussion
				// Repeat checks for enabling/disabling sections when a page is selected
				bool canEdit = AuthChecker.CheckActionForPage(page, Actions.ForPages.ModifyPage, currentUser, currentGroups);
				bool canManagePage = false;
				bool canManageDiscussion = false;
				if(!canDeletePages) canManagePage = AuthChecker.CheckActionForPage(page, Actions.ForPages.ManagePage, currentUser, currentGroups);
				if(!canDeletePages && !canManagePage) canManageDiscussion = AuthChecker.CheckActionForPage(page, Actions.ForPages.ManageDiscussion, currentUser, currentGroups);
				bool canSelect = canManagePage | canDeletePages | canManageDiscussion;

				PageContent firstContent = null;
				List<int> baks = Pages.GetBackups(page);
				if(baks.Count == 0) firstContent = currentContent;
				else firstContent = Pages.GetBackupContent(page, baks[0]);

				result.Add(new PageRow(page, currentContent, firstContent,
					Pages.GetMessageCount(page), baks.Count, orphanPages.Contains(page.FullName),
					canEdit, canSelect, canSetPermissions, txtCurrentPage.Value == page.FullName));
			}

			rptPages.DataSource = result;
		}

		protected void rptPages_ItemCommand(object sender, RepeaterCommandEventArgs e) {
			txtCurrentPage.Value = e.CommandArgument as string;

			if(e.CommandName == "Select") {
				// Permissions are checked in ActivatePageEditor()

				ActivatePageEditor();
			}
			else if(e.CommandName == "Perms") {
				if(!AdminMaster.CanManagePermissions(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

				ActivatePagePermissionsManager();
			}
		}

		protected void btnPublic_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);

			RemoveAllPermissions(page);

			// Set permissions
			AuthWriter.SetPermissionForPage(AuthStatus.Grant, page, Actions.ForPages.ModifyPage,
				Users.FindUserGroup(Settings.AnonymousGroup));
			AuthWriter.SetPermissionForPage(AuthStatus.Grant, page, Actions.ForPages.PostDiscussion,
				Users.FindUserGroup(Settings.AnonymousGroup));

			RefreshPermissionsManager();
		}

		protected void btnAsNamespace_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);

			RemoveAllPermissions(page);

			RefreshPermissionsManager();
		}

		protected void btnLocked_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);

			RemoveAllPermissions(page);

			// Set permissions
			AuthWriter.SetPermissionForPage(AuthStatus.Deny, page, Actions.ForPages.ModifyPage,
				Users.FindUserGroup(Settings.UsersGroup));
			AuthWriter.SetPermissionForPage(AuthStatus.Deny, page, Actions.ForPages.ModifyPage,
				Users.FindUserGroup(Settings.AnonymousGroup));

			RefreshPermissionsManager();
		}

		/// <summary>
		/// Refreshes the permissions manager.
		/// </summary>
		private void RefreshPermissionsManager() {
			string r = permissionsManager.CurrentResourceName;
			permissionsManager.CurrentResourceName = r;
		}

		/// <summary>
		/// Removes all the permissions for a page.
		/// </summary>
		/// <param name="page">The page.</param>
		private void RemoveAllPermissions(PageInfo page) {
			AuthWriter.RemoveEntriesForPage(Users.FindUserGroup(Settings.AnonymousGroup), page);
			AuthWriter.RemoveEntriesForPage(Users.FindUserGroup(Settings.UsersGroup), page);
			AuthWriter.RemoveEntriesForPage(Users.FindUserGroup(Settings.AdministratorsGroup), page);
		}

		/// <summary>
		/// Populates the namespaces list for migration.
		/// </summary>
		/// <param name="page">The selected page.</param>
		/// <returns><c>true</c> if there is at least one valid target namespace, <c>false</c> otherwise.</returns>
		private bool PopulateTargetNamespaces(PageInfo page) {
			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			lstTargetNamespace.Items.Clear();

			NamespaceInfo pageNamespace = Pages.FindNamespace(NameTools.GetNamespace(page.FullName));
			if(pageNamespace != null) {
				// Try adding Root as target namespace
				bool canManagePages = AuthChecker.CheckActionForNamespace(null, Actions.ForNamespaces.ManagePages, currentUser, currentGroups);
				if(canManagePages) lstTargetNamespace.Items.Add(new ListItem("<root>", ""));
			}

			// Try adding all other namespaces
			foreach(NamespaceInfo nspace in Pages.GetNamespaces().FindAll(n => n.Provider == page.Provider)) {
				if(pageNamespace == null || (pageNamespace != null && nspace.Name != pageNamespace.Name)) {
					bool canManagePages = AuthChecker.CheckActionForNamespace(nspace, Actions.ForNamespaces.ManagePages, currentUser, currentGroups);
					if(canManagePages) lstTargetNamespace.Items.Add(new ListItem(nspace.Name, nspace.Name));
				}
			}

			return lstTargetNamespace.Items.Count > 0;
		}

		/// <summary>
		/// Activates the page editor.
		/// </summary>
		private void ActivatePageEditor() {
			lblCurrentPage.Text = txtCurrentPage.Value;
			txtNewName.Text = NameTools.GetLocalName(txtCurrentPage.Value);

			// Enable/disable page sections
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			NamespaceInfo nspace = Pages.FindNamespace(NameTools.GetNamespace(page.FullName));
			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			bool canApproveReject = AdminMaster.CanApproveDraft(page, currentUser, currentGroups);
			bool canDeletePages = AuthChecker.CheckActionForNamespace(nspace, Actions.ForNamespaces.DeletePages, currentUser, currentGroups);
			bool canManageAllPages = AuthChecker.CheckActionForNamespace(nspace, Actions.ForNamespaces.ManagePages, currentUser, currentGroups);
			bool canManagePage = AuthChecker.CheckActionForPage(page, Actions.ForPages.ManagePage, currentUser, currentGroups);
			bool canManageDiscussion = AuthChecker.CheckActionForPage(page, Actions.ForPages.ManageDiscussion, currentUser, currentGroups);
			bool namespaceAvailable = PopulateTargetNamespaces(page);

			// Approve/reject
			// Rename
			// Migrate
			// Rollback
			// Delete Backups
			// Clear discussion
			// Delete

			pnlApproveRevision.Enabled = canApproveReject;
			pnlRename.Enabled = canDeletePages;
			pnlMigrate.Enabled = canManageAllPages && namespaceAvailable;
			pnlRollback.Enabled = canManagePage;
			pnlDeleteBackups.Enabled = canManagePage;
			pnlClearDiscussion.Enabled = canManageDiscussion;
			pnlDelete.Enabled = canDeletePages;

			// Disable rename, migrate, delete for default page
			NamespaceInfo currentNamespace = Pages.FindNamespace(lstNamespace.SelectedValue);
			string currentDefaultPage = currentNamespace != null ? currentNamespace.DefaultPage.FullName : Settings.DefaultPage;
			if(txtCurrentPage.Value == currentDefaultPage) {
				btnRename.Enabled = false;
				btnMigrate.Enabled = false;
				btnDeletePage.Enabled = false;
			}

			LoadDraft(txtCurrentPage.Value);

			LoadBackups(txtCurrentPage.Value);

			btnRollback.Enabled = lstRevision.Items.Count > 0;
			btnDeleteBackups.Enabled = lstBackup.Items.Count > 0;

			pnlList.Visible = false;
			pnlEditPage.Visible = true;

			ClearResultLabels();
		}

		/// <summary>
		/// Activates the permissions manager.
		/// </summary>
		private void ActivatePagePermissionsManager() {
			lblPageName.Text = txtCurrentPage.Value;

			permissionsManager.CurrentResourceName = txtCurrentPage.Value;

			pnlList.Visible = false;
			pnlPermissions.Visible = true;
		}

		/// <summary>
		/// Loads the draft information, if any.
		/// </summary>
		/// <param name="currentPage">The current page name.</param>
		private void LoadDraft(string currentPage) {
			PageInfo page = Pages.FindPage(currentPage);
			PageContent draft = Pages.GetDraft(page);

			if(draft != null) {
				pnlApproveRevision.Visible = true;
				lblDateTime.Text = Preferences.AlignWithTimezone(draft.LastModified).ToString(Settings.DateTimeFormat);
				lblUser.Text = Users.UserLink(draft.User, true);

				// Ampersands are escaped automatically
				lnkEdit.NavigateUrl = "Edit.aspx?Page=" + Tools.UrlEncode(currentPage);
				lnkDiff.NavigateUrl = "Diff.aspx?Page=" + Tools.UrlEncode(currentPage) + "&Rev1=Current&Rev2=Draft";
				lblDraftPreview.Text = FormattingPipeline.FormatWithPhase3(
					FormattingPipeline.FormatWithPhase1And2(draft.Content, false, FormattingContext.PageContent, page), FormattingContext.PageContent, page);
			}
			else {
				pnlApproveRevision.Visible = false;
			}
		}

		/// <summary>
		/// Loads the backups for a page and populates the backups and revisions drop-down lists.
		/// </summary>
		/// <param name="pageName">The page.</param>
		private void LoadBackups(string pageName) {
			PageInfo page = Pages.FindPage(pageName);
			List<int> backups = Pages.GetBackups(page);

			lstRevision.Items.Clear();
			lstBackup.Items.Clear();

			foreach(int bak in backups) {
				PageContent bakContent = Pages.GetBackupContent(page, bak);
				
				ListItem item = new ListItem(bak.ToString() + ": " +
					Preferences.AlignWithTimezone(bakContent.LastModified).ToString(Settings.DateTimeFormat) + 
					" " + Properties.Messages.By + " " + bakContent.User,
					bak.ToString());

				// Add in reverse order - newer revisions at the top
				lstRevision.Items.Insert(0, item);
				lstBackup.Items.Insert(0, item);
			}
		}

		/// <summary>
		/// Clears all the result labels.
		/// </summary>
		private void ClearResultLabels() {
			lblApproveResult.CssClass = "";
			lblApproveResult.Text = "";
			lblRenameResult.CssClass = "";
			lblRenameResult.Text = "";
			lblRollbackResult.CssClass = "";
			lblRollbackResult.Text = "";
			lblBackupResult.CssClass = "";
			lblBackupResult.Text = "";
			lblDiscussionResult.CssClass = "";
			lblDiscussionResult.Text = "";
			lblDeleteResult.CssClass = "";
			lblDeleteResult.Text = "";
		}

		protected void rdoBackup_CheckedChanged(object sender, EventArgs e) {
			lstBackup.Enabled = rdoUpTo.Checked;
		}

		protected void btnBack_Click(object sender, EventArgs e) {
			// If page was specified in query string, don't refresh list
			ReturnToList();
			RefreshList();
		}

		/// <summary>
		/// Refreshes the pages list.
		/// </summary>
		private void RefreshList() {
			rangeBegin = pageSelector.SelectedPage * PageSize;
			rangeEnd = rangeBegin + pageSelector.SelectedPageSize - 1;
			selectedPage = pageSelector.SelectedPage;

			txtCurrentPage.Value = "";
			ResetEditor();
			rptPages.DataBind();
		}

		/// <summary>
		/// Returns to the group list.
		/// </summary>
		private void ReturnToList() {
			LoadExternallySelectedPage();
			if(externallySelectedPage != null) {
				// Return to page
				UrlTools.Redirect(externallySelectedPage.FullName + Settings.PageExtension);
			}
			else {
				// Return to list
				pnlEditPage.Visible = false;
				pnlPermissions.Visible = false;
				pnlBulkMigrate.Visible = false;
				pnlList.Visible = true;
			}
		}

		/// <summary>
		/// Resets the editor.
		/// </summary>
		private void ResetEditor() {
			btnRename.Enabled = true;
			btnMigrate.Enabled = true;
			btnDeletePage.Enabled = true;
			btnRollback.Enabled = true;
			btnDeleteBackups.Enabled = true;
			rdoAllBackups.Checked = false;
			rdoUpTo.Checked = false;
			lstBackup.Enabled = false;
		}

		protected void cvNewName_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtNewName.Text);
		}

		protected void btnApprove_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			if(!AdminMaster.CanApproveDraft(page, SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

			PageContent draft = Pages.GetDraft(page);

			Log.LogEntry("Page draft approval requested for " + draft.PageInfo.FullName, EntryType.General, SessionFacade.CurrentUsername);

			bool done = Pages.ModifyPage(draft.PageInfo, draft.Title, draft.User, draft.LastModified, draft.Comment,
				draft.Content, draft.Keywords, draft.Description, SaveMode.Backup);

			if(done) {
				Pages.DeleteDraft(draft.PageInfo);

				lblApproveResult.CssClass = "resultok";
				lblApproveResult.Text = Properties.Messages.DraftApproved;
				lblDraftPreview.Text = "";

				ReturnToList();
			}
			else {
				lblApproveResult.CssClass = "resulterror";
				lblApproveResult.Text = Properties.Messages.CouldNotApproveDraft;
			}
		}

		protected void btnReject_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			if(!AdminMaster.CanApproveDraft(page, SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

			Log.LogEntry("Page draft reject requested for " + page.FullName, EntryType.General, SessionFacade.CurrentUsername);

			Pages.DeleteDraft(page);

			lblApproveResult.CssClass = "resultok";
			lblApproveResult.Text = Properties.Messages.DraftRejected;
			lblDraftPreview.Text = "";

			ReturnToList();
		}

		protected void btnRename_Click(object sender, EventArgs e) {
			// Check name for change, validity and existence of another page with same name
			// Perform rename
			// Create shadow page, if needed

			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			if(!AuthChecker.CheckActionForNamespace(Pages.FindNamespace(NameTools.GetNamespace(page.FullName)), Actions.ForNamespaces.DeletePages,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

			txtNewName.Text = txtNewName.Text.Trim();

			string currentNamespace = NameTools.GetNamespace(txtCurrentPage.Value);
			string currentPage = NameTools.GetLocalName(txtCurrentPage.Value);

			if(!Page.IsValid) return;

			if(txtNewName.Text.ToLowerInvariant() == currentPage.ToLowerInvariant()) {
				return;
			}

			if(Pages.FindPage(NameTools.GetFullName(currentNamespace, txtNewName.Text)) != null) {
				lblRenameResult.CssClass = "resulterror";
				lblRenameResult.Text = Properties.Messages.PageAlreadyExists;
				return;
			}

			Log.LogEntry("Page rename requested for " + txtCurrentPage.Value, EntryType.General, Log.SystemUsername);

			PageInfo oldPage = Pages.FindPage(txtCurrentPage.Value);
			PageContent oldContent = Content.GetPageContent(oldPage, false);

			bool done = Pages.RenamePage(oldPage, txtNewName.Text);

			if(done) {
				if(chkShadowPage.Checked) {
					done = Pages.CreatePage(currentNamespace, currentPage);

					if(done) {
						done = Pages.ModifyPage(Pages.FindPage(txtCurrentPage.Value),
							oldContent.Title, oldContent.User, oldContent.LastModified,
							oldContent.Comment, ">>> [" + txtNewName.Text + "]",
							new string[0], oldContent.Description, SaveMode.Normal);

						if(done) {
							ResetPageList();

							RefreshList();
							lblRenameResult.CssClass = "resultok";
							lblRenameResult.Text = Properties.Messages.PageRenamed;
							ReturnToList();
						}
						else {
							lblRenameResult.CssClass = "resulterror";
							lblRenameResult.Text = Properties.Messages.PageRenamedCouldNotSetShadowPageContent;
						}
					}
					else {
						lblRenameResult.CssClass = "resulterror";
						lblRenameResult.Text = Properties.Messages.PageRenamedCouldNotCreateShadowPage;
					}
				}
				else {
					RefreshList();
					lblRenameResult.CssClass = "resultok";
					lblRenameResult.Text = Properties.Messages.PageRenamed;
					ReturnToList();
				}
			}
			else {
				lblRenameResult.CssClass = "resulterror";
				lblRenameResult.Text = Properties.Messages.CouldNotRenamePage;
			}
		}

		protected void btnMigrate_Click(object sender, EventArgs e) {
			lblMigrateResult.CssClass = "";
			lblMigrateResult.Text = "";

			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			NamespaceInfo targetNamespace = Pages.FindNamespace(lstTargetNamespace.SelectedValue);
			bool canManageAllPages = AuthChecker.CheckActionForNamespace(Pages.FindNamespace(NameTools.GetNamespace(page.FullName)),
				Actions.ForNamespaces.ManagePages, currentUser, currentGroups);
			bool canManageAllPagesInTarget = AuthChecker.CheckActionForNamespace(targetNamespace,
				Actions.ForNamespaces.ManagePages, currentUser, currentGroups);

			if(canManageAllPages && canManageAllPagesInTarget) {
				bool done = Pages.MigratePage(page, targetNamespace, chkCopyCategories.Checked);
				if(done) {
					chkCopyCategories.Checked = false;

					ResetPageList();

					RefreshList();
					lblRenameResult.CssClass = "resultok";
					lblRenameResult.Text = Properties.Messages.PageRenamed;
					ReturnToList();
				}
				else {
					lblMigrateResult.CssClass = "resulterror";
					lblMigrateResult.Text = Properties.Messages.CouldNotMigratePage;
				}
			}
		}

		protected void btnRollback_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			if(!AuthChecker.CheckActionForPage(page, Actions.ForPages.ManagePage, SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

			int targetRevision = -1;

			// This should never occur
			if(!int.TryParse(lstRevision.SelectedValue, out targetRevision)) return;

			Log.LogEntry("Page rollback requested for " + txtCurrentPage.Value + " to rev. " + targetRevision.ToString(), EntryType.General, Log.SystemUsername);

			bool done = Pages.Rollback(page, targetRevision);

			if(done) {
				RefreshList();
				lblRollbackResult.CssClass = "resultok";
				lblRollbackResult.Text = Properties.Messages.PageRolledBack;
				ReturnToList();
			}
			else {
				lblRollbackResult.CssClass = "resulterror";
				lblRollbackResult.Text = Properties.Messages.CouldNotRollbackPage;
			}
		}

		protected void btnDeleteBackups_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			if(!AuthChecker.CheckActionForPage(page, Actions.ForPages.ManagePage, SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

			int targetRevision = -1;

			// This should never occur
			if(!int.TryParse(lstBackup.SelectedValue, out targetRevision)) return;

			Log.LogEntry("Page backup deletion requested for " + txtCurrentPage.Value, EntryType.General, Log.SystemUsername);

			bool done = false;
			if(rdoAllBackups.Checked) {
				done = Pages.DeleteBackups(page);
			}
			else {
				done = Pages.DeleteBackups(page, targetRevision);
			}

			if(done) {
				RefreshList();
				lblBackupResult.CssClass = "resultok";
				lblBackupResult.Text = Properties.Messages.PageBackupsDeleted;
				ReturnToList();
			}
			else {
				lblBackupResult.CssClass = "resulterror";
				lblBackupResult.Text = Properties.Messages.CouldNotDeletePageBackups;
			}
		}

		protected void btnClearDiscussion_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			if(!AuthChecker.CheckActionForPage(page, Actions.ForPages.ManageDiscussion, SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

			Log.LogEntry("Page discussion cleanup requested for " + txtCurrentPage.Value, EntryType.General, Log.SystemUsername);

			bool done = Pages.RemoveAllMessages(page);

			if(done) {
				RefreshList();
				lblDiscussionResult.CssClass = "resultok";
				lblDiscussionResult.Text = Properties.Messages.AllMessagesDeleted;
				ReturnToList();
			}
			else {
				lblDiscussionResult.CssClass = "resulterror";
				lblDiscussionResult.Text = Properties.Messages.CouldNotDeleteOneOrMoreMessages;
			}
		}

		protected void btnDeletePage_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(txtCurrentPage.Value);
			if(!AuthChecker.CheckActionForNamespace(Pages.FindNamespace(NameTools.GetNamespace(page.FullName)), Actions.ForNamespaces.DeletePages,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) return;

			Log.LogEntry("Page deletion requested for " + txtCurrentPage.Value, EntryType.General, Log.SystemUsername);

			bool done = Pages.DeletePage(page);

			if(done) {
				ResetPageList();

				RefreshList();
				lblDeleteResult.CssClass = "resultok";
				lblDeleteResult.Text = Properties.Messages.PageDeleted;
				ReturnToList();
			}
			else {
				lblDeleteResult.CssClass = "resulterror";
				lblDeleteResult.Text = Properties.Messages.CouldNotDeletePage;
			}
		}

		/// <summary>
		/// Resets the bulk management editor.
		/// </summary>
		private void ResetBulkEditor() {
			pageListBuilder.ResetControl();

			chkBulkMigrateCopyCategories.Checked = false;

			lblBulkMigrateResult.CssClass = "";
			lblBulkMigrateResult.Text = "";
		}

		/// <summary>
		/// Loads target namespaces for bulk migration.
		/// </summary>
		private void LoadTargetNamespaces() {
			// Load valid namespaces, filtering the current one
			lstBulkMigrateTargetNamespace.Items.Clear();

			bool canManageAllPages = false;
			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			if(!string.IsNullOrEmpty(lstNamespace.SelectedValue)) {
				// Root namespace
				canManageAllPages = AuthChecker.CheckActionForNamespace(null,
					Actions.ForNamespaces.ManagePages, currentUser, currentGroups);

				if(canManageAllPages) {
					lstBulkMigrateTargetNamespace.Items.Add(new ListItem("<root>", "."));
				}
			}

			foreach(NamespaceInfo ns in Pages.GetNamespaces().FindAll(n => n.Provider.GetType().FullName == providerSelector.SelectedProvider)) {
				// All sub-namespaces
				if(ns.Name != lstNamespace.SelectedValue) {
					canManageAllPages = AuthChecker.CheckActionForNamespace(ns,
						Actions.ForNamespaces.ManagePages, currentUser, currentGroups);

					if(canManageAllPages) {
						lstBulkMigrateTargetNamespace.Items.Add(new ListItem(ns.Name, ns.Name));
					}
				}
			}
		}

		protected void providerSelector_SelectedProviderChanged(object sender, EventArgs e) {
			pageListBuilder.CurrentProvider = providerSelector.SelectedProvider;
			LoadTargetNamespaces();
		}

		protected void btnBulkMigrateBack_Click(object sender, EventArgs e) {
			RefreshList();
			ReturnToList();
		}

		protected void btnBulkMigrate_Click(object sender, EventArgs e) {
			ResetBulkEditor();
			pageListBuilder.CurrentNamespace = lstNamespace.SelectedValue;
			pageListBuilder.CurrentProvider = providerSelector.SelectedProvider;
			pnlBulkMigrate.Visible = true;
			pnlList.Visible = false;

			LoadTargetNamespaces();

			btnBulkMigratePages.Enabled = lstBulkMigrateTargetNamespace.Items.Count > 0;
		}

		/// <summary>
		/// Determines whether a page is the default page of its namespace.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the page is the default namespace of its namespace, <c>false</c> otherwise.</returns>
		private bool IsDefaultPage(PageInfo page) {
			string localName, nspace;
			NameTools.ExpandFullName(page.FullName, out nspace, out localName);

			if(string.IsNullOrEmpty(nspace)) {
				return localName.ToLowerInvariant() == Settings.DefaultPage.ToLowerInvariant();
			}
			else {
				NamespaceInfo ns = Pages.FindNamespace(nspace);
				return ns.DefaultPage != null && ns.DefaultPage.FullName.ToLowerInvariant() == page.FullName.ToLowerInvariant();
			}
		}

		protected void btnBulkMigratePages_Click(object sender, EventArgs e) {
			lblBulkMigrateResult.CssClass = "";
			lblBulkMigrateResult.Text = "";

			List<PageInfo> selectedPages = new List<PageInfo>(20);
			foreach(string pg in pageListBuilder.SelectedPages) {
				PageInfo page = Pages.FindPage(pg);
				if(page != null && !IsDefaultPage(page)) selectedPages.Add(page);
			}

			if(selectedPages.Count == 0) {
				lblBulkMigrateResult.CssClass = "resulterror";
				lblBulkMigrateResult.Text = Properties.Messages.NoPagesToMigrate;
				return;
			}

			string nspaceName = lstBulkMigrateTargetNamespace.SelectedValue;
			if(nspaceName == ".") nspaceName = null;
			NamespaceInfo selectedNamespace = Pages.FindNamespace(nspaceName);

			Log.LogEntry("Bulk migration requested", EntryType.General, SessionFacade.CurrentUsername);

			bool allDone = true;
			foreach(PageInfo pg in selectedPages) {
				allDone &= Pages.MigratePage(pg, selectedNamespace, chkBulkMigrateCopyCategories.Checked);
			}

			if(allDone) {
				lblBulkMigrateResult.CssClass = "resultok";
				lblBulkMigrateResult.Text = Properties.Messages.BulkMigrationCompleted;
			}
			else {
				lblBulkMigrateResult.CssClass = "resulterror";
				lblBulkMigrateResult.Text = Properties.Messages.BulkMigrationCompletedWithErrors;
			}

			ResetPageList();
			rptPages.DataBind();

			pageListBuilder.ResetControl();
		}

	}

	/// <summary>
	/// Represents a Page for display purposes.
	/// </summary>
	public class PageRow {

		private string fullName, title, createdBy, createdOn, lastModifiedBy, lastModifiedOn, discussion, revisions, provider, additionalClass;
		private bool isOrphan, canEdit, canSelect, canSetPermissions;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageRow" /> class.
		/// </summary>
		/// <param name="page">The original page.</param>
		/// <param name="currentContent">The current content.</param>
		/// <param name="firstContent">The first revision content.</param>
		/// <param name="discussionCount">The number of messages in the discussion.</param>
		/// <param name="revisionCount">The number of revisions.</param>
		/// <param name="isOrphan">A value indicating whether the page is orphan.</param>
		/// <param name="canEdit">A value indicating whether the current user can edit the page.</param>
		/// <param name="canSelect">A value indicating whether the current user can select the page.</param>
		/// <param name="canSetPermissions">A value indicating whether the current user can set permissions for the page.</param>
		/// <param name="selected">A value indicating whether the page is selected.</param>
		public PageRow(PageInfo page, PageContent currentContent, PageContent firstContent, int discussionCount, int revisionCount,
			bool isOrphan, bool canEdit, bool canSelect, bool canSetPermissions, bool selected) {

			fullName = page.FullName;
			title = FormattingPipeline.PrepareTitle(currentContent.Title, false, FormattingContext.Other, page);
			createdBy = firstContent.User;
			createdOn = Preferences.AlignWithTimezone(page.CreationDateTime).ToString(Settings.DateTimeFormat);
			lastModifiedBy = currentContent.User;
			lastModifiedOn = Preferences.AlignWithTimezone(currentContent.LastModified).ToString(Settings.DateTimeFormat);
			discussion = discussionCount.ToString();
			revisions = revisionCount.ToString();
			provider = page.Provider.Information.Name;

			this.isOrphan = isOrphan;

			this.canEdit = canEdit;
			this.canSelect = canSelect;
			this.canSetPermissions = canSetPermissions;
			additionalClass = selected ? " selected" : "";
		}

		/// <summary>
		/// Gets the full name.
		/// </summary>
		public string FullName {
			get { return fullName; }
		}

		/// <summary>
		/// Gets the title.
		/// </summary>
		public string Title {
			get { return title; }
		}

		/// <summary>
		/// Gets the original page creator.
		/// </summary>
		public string CreatedBy {
			get { return createdBy; }
		}

		/// <summary>
		/// Gets the original creation date/time.
		/// </summary>
		public string CreatedOn {
			get { return createdOn; }
		}

		/// <summary>
		/// Gets the user who last modified the page.
		/// </summary>
		public string LastModifiedBy {
			get { return lastModifiedBy; }
		}

		/// <summary>
		/// Gets the last modification date/time.
		/// </summary>
		public string LastModifiedOn {
			get { return lastModifiedOn; }
		}

		/// <summary>
		/// Gets the discussion message count.
		/// </summary>
		public string Discussion {
			get { return discussion; }
		}

		/// <summary>
		/// Gets the number of revisions.
		/// </summary>
		public string Revisions {
			get { return revisions; }
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets a value indicating whether the page is orhpan.
		/// </summary>
		public bool IsOrphan {
			get { return isOrphan; }
		}

		/// <summary>
		/// Gets a value indicating whether the current user can edit the page.
		/// </summary>
		public bool CanEdit {
			get { return canEdit; }
		}

		/// <summary>
		/// Gets a value indicating whether the current user can select the page.
		/// </summary>
		public bool CanSelect {
			get { return canSelect; }
		}

		/// <summary>
		/// Gets a value indicating whether the current user can set permissions for the page.
		/// </summary>
		public bool CanSetPermissions {
			get { return canSetPermissions; }
		}

		/// <summary>
		/// Gets the additional CSS class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
