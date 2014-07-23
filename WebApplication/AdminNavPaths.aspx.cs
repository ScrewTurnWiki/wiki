
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminNavPaths : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManagePages(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load namespaces

				// Add root namespace
				lstNamespace.Items.Add(new ListItem("<root>", ""));

				List<NamespaceInfo> namespaces = Pages.GetNamespaces();

				foreach(NamespaceInfo ns in namespaces) {
					lstNamespace.Items.Add(new ListItem(ns.Name, ns.Name));
				}

				// Load navigation paths
				rptNavPaths.DataBind();
			}

			btnNewNavPath.Enabled = CanManagePagesInCurrentNamespace();
		}

		protected void lstNamespace_SelectedIndexChanged(object sender, EventArgs e) {
			rptNavPaths.DataBind();
			btnNewNavPath.Enabled = CanManagePagesInCurrentNamespace();
		}

		/// <summary>
		/// Gets a value indicating whether the current user can manage pages in the selected namespace.
		/// </summary>
		/// <returns><c>true</c> if the user can manage pages, <c>false</c> otherwise.</returns>
		private bool CanManagePagesInCurrentNamespace() {
			NamespaceInfo nspace = Pages.FindNamespace(lstNamespace.SelectedValue);
			bool canManagePages = AuthChecker.CheckActionForNamespace(nspace, Actions.ForNamespaces.ManagePages,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			return canManagePages;
		}

		protected void rptNavPaths_DataBinding(object sender, EventArgs e) {
			bool canManagePages = CanManagePagesInCurrentNamespace();
			List<NavigationPath> paths = NavigationPaths.GetNavigationPaths(Pages.FindNamespace(lstNamespace.SelectedValue));

			List<NavigationPathRow> result = new List<NavigationPathRow>(paths.Count);

			foreach(NavigationPath path in paths) {
				result.Add(new NavigationPathRow(path, canManagePages, path.FullName == txtCurrentNavPath.Value));
			}

			rptNavPaths.DataSource = result;
		}

		protected void rptNavPaths_ItemCommand(object sender, CommandEventArgs e) {
			txtCurrentNavPath.Value = e.CommandArgument as string;

			if(e.CommandName == "Select") {
				if(!CanManagePagesInCurrentNamespace()) return;

				txtName.Text = txtCurrentNavPath.Value;
				txtName.Enabled = false;

				NavigationPath path = NavigationPaths.Find(txtCurrentNavPath.Value);
				foreach(string page in path.Pages) {
					PageInfo pageInfo = Pages.FindPage(page);
					if(pageInfo != null) {
						PageContent content = Content.GetPageContent(pageInfo, false);
						lstPages.Items.Add(new ListItem(FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.Other, pageInfo), pageInfo.FullName));
					}
				}

				cvName2.Enabled = false;
				btnCreate.Visible = false;
				btnSave.Visible = true;
				btnDelete.Visible = true;

				pnlList.Visible = false;
				pnlEditNavPath.Visible = true;

				lblResult.Text = "";
			}
		}

		protected void btnNewNavPath_Click(object sender, EventArgs e) {
			if(!CanManagePagesInCurrentNamespace()) return;

			pnlList.Visible = false;
			pnlEditNavPath.Visible = true;
			lblResult.Text = "";
		}

		protected void cvName1_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtName.Text);
		}

		protected void cvName2_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = NavigationPaths.Find(NameTools.GetFullName(lstNamespace.SelectedValue, txtName.Text)) == null;
		}

		/// <summary>
		/// Resets the account editor.
		/// </summary>
		private void ResetEditor() {
			txtName.Text = "";
			txtName.Enabled = true;

			txtPageName.Text = "";
			lstAvailablePage.Items.Clear();
			btnAdd.Enabled = false;
			lstPages.Items.Clear();
			lstPages_SelectedIndexChanged(this, null);

			cvName2.Enabled = true;
			btnCreate.Visible = true;
			btnSave.Visible = false;
			btnDelete.Visible = false;
			lblResult.Text = "";
		}

		/// <summary>
		/// Returns to the accounts list.
		/// </summary>
		private void ReturnToList() {
			pnlEditNavPath.Visible = false;
			pnlList.Visible = true;
		}

		/// <summary>
		/// Refreshes the users list.
		/// </summary>
		private void RefreshList() {
			txtCurrentNavPath.Value = "";
			ResetEditor();
			rptNavPaths.DataBind();
		}

		protected void btnSearch_Click(object sender, EventArgs e) {
			txtPageName.Text = txtPageName.Text.Trim();

			if(txtPageName.Text.Length == 0) {
				lstAvailablePage.Items.Clear();
				btnAdd.Enabled = false;
				return;
			}

			PageInfo[] pages = SearchTools.SearchSimilarPages(txtPageName.Text, lstNamespace.SelectedValue);

			lstAvailablePage.Items.Clear();

			foreach(PageInfo page in pages) {
				PageContent content = Content.GetPageContent(page, false);
				lstAvailablePage.Items.Add(new ListItem(FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.Other, page), page.FullName));
			}

			btnAdd.Enabled = pages.Length > 0;
		}

		protected void btnAdd_Click(object sender, EventArgs e) {
			PageInfo page = Pages.FindPage(lstAvailablePage.SelectedValue);
			PageContent content = Content.GetPageContent(page, false);

			lstPages.Items.Add(new ListItem(FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.Other, page), page.FullName));

			txtPageName.Text = "";
			lstAvailablePage.Items.Clear();
			btnAdd.Enabled = false;
		}

		protected void lstPages_SelectedIndexChanged(object sender, EventArgs e) {
			btnUp.Enabled = lstPages.SelectedIndex > 0;
			btnDown.Enabled = lstPages.SelectedIndex < lstPages.Items.Count - 1;
			btnRemove.Enabled = lstPages.SelectedIndex >= 0;;
		}

		protected void btnUp_Click(object sender, EventArgs e) {
			int index = lstPages.SelectedIndex;
			ListItem item = lstPages.Items[lstPages.SelectedIndex];
			lstPages.Items.RemoveAt(lstPages.SelectedIndex);
			lstPages.Items.Insert(index - 1, item);
			lstPages.SelectedIndex = index - 1;
			lstPages_SelectedIndexChanged(sender, e);
		}

		protected void btnDown_Click(object sender, EventArgs e) {
			int index = lstPages.SelectedIndex;
			ListItem item = lstPages.Items[lstPages.SelectedIndex];
			lstPages.Items.RemoveAt(lstPages.SelectedIndex);
			lstPages.Items.Insert(index + 1, item);
			lstPages.SelectedIndex = index + 1;
			lstPages_SelectedIndexChanged(sender, e);
		}

		protected void btnRemove_Click(object sender, EventArgs e) {
			lstPages.Items.RemoveAt(lstPages.SelectedIndex);
			lstPages.SelectedIndex = -1;
			lstPages_SelectedIndexChanged(sender, e);
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			RefreshList();
			ReturnToList();
		}

		protected void btnCreate_Click(object sender, EventArgs e) {
			if(!CanManagePagesInCurrentNamespace()) return;

			txtName.Text = txtName.Text.Trim();

			if(!Page.IsValid) return;

			if(lstPages.Items.Count == 0) {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.TheNavPathMustContain;
				return;
			}

			bool done = NavigationPaths.AddNavigationPath(Pages.FindNamespace(lstNamespace.SelectedValue),
				txtName.Text, GetSelectedPages(), null);

			if(done) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.NavPathCreated;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotCreateNavPath;
			}
		}

		protected void btnDelete_Click(object sender, EventArgs e) {
			if(!CanManagePagesInCurrentNamespace()) return;

			bool done = NavigationPaths.RemoveNavigationPath(txtCurrentNavPath.Value);

			if(done) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.NavPathDeleted;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotDeleteNavPath;
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			if(!CanManagePagesInCurrentNamespace()) return;

			bool done = NavigationPaths.ModifyNavigationPath(txtCurrentNavPath.Value, GetSelectedPages());

			if(done) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.NavPathSaved;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotSaveNavPath;
			}
		}

		/// <summary>
		/// Gets the selected pages for the navigation path.
		/// </summary>
		/// <returns>The selected pages.</returns>
		private List<PageInfo> GetSelectedPages() {
			List<PageInfo> result = new List<PageInfo>(lstPages.Items.Count);

			foreach(ListItem item in lstPages.Items) {
				PageInfo page = Pages.FindPage(item.Value);
				if(page != null) {
					result.Add(page);
				}
			}

			return result;
		}

	}

	/// <summary>
	/// Represents a navigation path for display purposes.
	/// </summary>
	public class NavigationPathRow {

		private string fullName, pages, provider, additionalClass;
		private bool canSelect;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NavigationPathRow" /> class.
		/// </summary>
		/// <param name="path">The original navigation path.</param>
		/// <param name="canSelect">A value indicating whether the path can be selected.</param>
		/// <param name="selected">A value indicating whether the navigation path is selected.</param>
		public NavigationPathRow(NavigationPath path, bool canSelect, bool selected) {
			fullName = path.FullName;
			pages = path.Pages.Length.ToString();
			provider = path.Provider.Information.Name;
			this.canSelect = canSelect;
			additionalClass = selected ? " selected" : "";
		}

		/// <summary>
		/// Gets the full name.
		/// </summary>
		public string FullName {
			get { return fullName; }
		}

		/// <summary>
		/// Gets the pages.
		/// </summary>
		public string Pages {
			get { return pages; }
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets a value indicating whether the path can be selected.
		/// </summary>
		public bool CanSelect {
			get { return canSelect; }
		}

		/// <summary>
		/// Gets the additional CSS class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
