
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminCategories : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			bool canManageCategories = AdminMaster.CanManageCategories(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			if(!canManageCategories) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load namespaces

				// Add root namespace
				lstNamespace.Items.Add(new ListItem("<root>", ""));

				List<NamespaceInfo> namespaces = Pages.GetNamespaces();

				foreach(NamespaceInfo ns in namespaces) {
					lstNamespace.Items.Add(new ListItem(ns.Name, ns.Name));
				}

				// Load pages
				rptCategories.DataBind();
			}

			btnNewCategory.Enabled = CanManageCategoriesInCurrentNamespace();
			btnBulkManage.Enabled = btnNewCategory.Enabled;
		}

		protected void cvNewCategory_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtNewCategory.Text);
		}

		protected void btnNewCategory_Click(object sender, EventArgs e) {
			if(!CanManageCategoriesInCurrentNamespace()) return;

			lblNewCategoryResult.CssClass = "";
			lblNewCategoryResult.Text = "";

			txtNewCategory.Text = txtNewCategory.Text.Trim();

			Page.Validate("newcat");
			if(!Page.IsValid) return;

			txtNewCategory.Text = txtNewCategory.Text.Trim();
			if(txtNewCategory.Text.Length == 0) {
				return;
			}

			if(Pages.FindCategory(NameTools.GetFullName(lstNamespace.SelectedValue, txtNewCategory.Text)) != null) {
				lblNewCategoryResult.CssClass = "resulterror";
				lblNewCategoryResult.Text = Properties.Messages.CategoryAlreadyExists;
				return;
			}
			else {
				Log.LogEntry("Category creation requested for " + txtNewCategory.Text, EntryType.General, Log.SystemUsername);

				if(Pages.CreateCategory(lstNamespace.SelectedValue, txtNewCategory.Text)) {
					txtNewCategory.Text = "";
					lblNewCategoryResult.CssClass = "resultok";
					lblNewCategoryResult.Text = Properties.Messages.CategoryCreated;
					RefreshList();
				}
				else {
					lblNewCategoryResult.CssClass = "resulterror";
					lblNewCategoryResult.Text = Properties.Messages.CouldNotCreateCategory;
				}
			}
		}

		protected void lstNamespace_SelectedIndexChanged(object sender, EventArgs e) {
			rptCategories.DataBind();
			btnNewCategory.Enabled = CanManageCategoriesInCurrentNamespace();
		}

		/// <summary>
		/// Returns a value indicating whether the current user can manage categories in the selected namespace.
		/// </summary>
		/// <returns><c>true</c> if the user can manage categories, <c>false</c> otherwise.</returns>
		private bool CanManageCategoriesInCurrentNamespace() {
			NamespaceInfo nspace = Pages.FindNamespace(lstNamespace.SelectedValue);
			bool canManageCategories = AuthChecker.CheckActionForNamespace(nspace, Actions.ForNamespaces.ManageCategories,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			return canManageCategories;
		}

		protected void rptCategories_DataBinding(object sender, EventArgs e) {
			NamespaceInfo nspace = Pages.FindNamespace(lstNamespace.SelectedValue);
			bool canManageCategories = CanManageCategoriesInCurrentNamespace();
			List<CategoryInfo> categories = Pages.GetCategories(nspace);

			List<CategoryRow> result = new List<CategoryRow>(categories.Count);

			foreach(CategoryInfo cat in categories) {
				result.Add(new CategoryRow(cat, canManageCategories, txtCurrentCategory.Value == cat.FullName));
			}

			rptCategories.DataSource = result;
		}

		/// <summary>
		/// Refreshes the pages list.
		/// </summary>
		private void RefreshList() {
			txtCurrentCategory.Value = "";
			ResetEditor();
			ResetBulkEditor();
			rptCategories.DataBind();
		}

		/// <summary>
		/// Returns to the group list.
		/// </summary>
		private void ReturnToList() {
			pnlEditCategory.Visible = false;
			pnlBulkManage.Visible = false;
			pnlList.Visible = true;
		}

		/// <summary>
		/// Resets the editor.
		/// </summary>
		private void ResetEditor() {
			btnRename.Enabled = true;
			txtNewName.Text = "";
			lstDestinationCategory.Items.Clear();
			btnMerge.Enabled = true;
		}

		/// <summary>
		/// Clears all the result labels.
		/// </summary>
		private void ClearResultLabels() {
			lblNewCategoryResult.CssClass = "";
			lblNewCategoryResult.Text = "";
			lblRenameResult.CssClass = "";
			lblRenameResult.Text = "";
			lblMergeResult.CssClass = "";
			lblMergeResult.Text = "";
		}

		protected void rptCategories_ItemCommand(object sender, CommandEventArgs e) {
			if(e.CommandName == "Select") {
				if(!CanManageCategoriesInCurrentNamespace()) return;

				lblRenameResult.CssClass = "";
				lblRenameResult.Text = "";
				lblDeleteResult.CssClass = "";
				lblDeleteResult.Text = "";
				lblMergeResult.CssClass = "";
				lblMergeResult.Text = "";

				txtCurrentCategory.Value = e.CommandArgument as string;
				lblCurrentCategory.Text = txtCurrentCategory.Value;

				txtNewName.Text = NameTools.GetLocalName(txtCurrentCategory.Value);

				// Load target directories for merge function
				lstDestinationCategory.Items.Clear();
				List<CategoryInfo> categories = Pages.GetCategories(Pages.FindNamespace(lstNamespace.SelectedValue));
				foreach(CategoryInfo cat in categories) {
					if(cat.FullName != txtCurrentCategory.Value) {
						string name = NameTools.GetLocalName(cat.FullName);
						lstDestinationCategory.Items.Add(new ListItem(name, cat.FullName));
					}
				}
				btnMerge.Enabled = lstDestinationCategory.Items.Count > 0;

				pnlEditCategory.Visible = true;
				pnlList.Visible = false;

				ClearResultLabels();
			}
		}

		protected void cvNewName_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtNewName.Text);
		}

		protected void btnRename_Click(object sender, EventArgs e) {
			if(!CanManageCategoriesInCurrentNamespace()) return;

			lblRenameResult.CssClass = "";
			lblRenameResult.Text = "";

			if(!Page.IsValid) return;

			txtNewName.Text = txtNewName.Text.Trim();

			if(txtNewName.Text.ToLowerInvariant() == txtCurrentCategory.Value.ToLowerInvariant()) {
				return;
			}

			if(Pages.FindCategory(NameTools.GetFullName(lstNamespace.SelectedValue, txtNewName.Text)) != null) {
				lblRenameResult.CssClass = "resulterror";
				lblRenameResult.Text = Properties.Messages.CategoryAlreadyExists;
				return;
			}

			Log.LogEntry("Category rename requested for " + txtCurrentCategory.Value + " to " + txtNewName.Text, EntryType.General, Log.SystemUsername);

			if(Pages.RenameCategory(Pages.FindCategory(txtCurrentCategory.Value), txtNewName.Text)) {
				RefreshList();
				lblRenameResult.CssClass = "resultok";
				lblRenameResult.Text = Properties.Messages.CategoryRenamed;
				ReturnToList();
			}
			else {
				lblRenameResult.CssClass = "resulterror";
				lblRenameResult.Text = Properties.Messages.CouldNotRenameCategory;
			}
		}

		protected void btnMerge_Click(object sender, EventArgs e) {
			if(!CanManageCategoriesInCurrentNamespace()) return;

			CategoryInfo source = Pages.FindCategory(txtCurrentCategory.Value);
			CategoryInfo dest = Pages.FindCategory(lstDestinationCategory.SelectedValue);

			Log.LogEntry("Category merge requested for " + txtCurrentCategory.Value + " into " + lstDestinationCategory.SelectedValue, EntryType.General, Log.SystemUsername);

			if(Pages.MergeCategories(source, dest)) {
				RefreshList();
				lblMergeResult.CssClass = "resultok";
				lblMergeResult.Text = Properties.Messages.CategoriesMerged;
				ReturnToList();
			}
			else {
				lblMergeResult.CssClass = "resulterror";
				lblMergeResult.Text = Properties.Messages.CouldNotMergeCategories;
			}
		}

		protected void btnDelete_Click(object sender, EventArgs e) {
			if(!CanManageCategoriesInCurrentNamespace()) return;

			Log.LogEntry("Category deletion requested for " + txtCurrentCategory.Value, EntryType.General, Log.SystemUsername);

			if(Pages.RemoveCategory(Pages.FindCategory(txtCurrentCategory.Value))) {
				RefreshList();
				lblDeleteResult.CssClass = "resultok";
				lblDeleteResult.Text = Properties.Messages.CategoryDeleted;
				ReturnToList();
			}
			else {
				lblDeleteResult.CssClass = "resulterror";
				lblDeleteResult.Text = Properties.Messages.CouldNotDeleteCategory;
			}
		}

		protected void btnBack_Click(object sender, EventArgs e) {
			RefreshList();
			ReturnToList();
		}

		/// <summary>
		/// Resets the bulk management editor.
		/// </summary>
		private void ResetBulkEditor() {
			pageListBuilder.ResetControl();
			lstBulkCategories.Items.Clear();

			rdoBulkAdd.Checked = true;
			rdoBulkReplace.Checked = false;

			lblBulkResult.CssClass = "";
			lblBulkResult.Text = "";
		}

		protected void providerSelector_SelectedProviderChanged(object sender, EventArgs e) {
			pageListBuilder.CurrentProvider = providerSelector.SelectedProvider;
			RefreshBulkCategoryList();
		}

		protected void btnBulkBack_Click(object sender, EventArgs e) {
			RefreshList();
			ReturnToList();
		}

		protected void btnBulkManage_Click(object sender, EventArgs e) {
			ResetBulkEditor();
			pageListBuilder.CurrentNamespace = lstNamespace.SelectedValue;
			pageListBuilder.CurrentProvider = providerSelector.SelectedProvider;
			RefreshBulkCategoryList();
			pnlBulkManage.Visible = true;
			pnlList.Visible = false;
		}

		/// <summary>
		/// Refreshes the bulk category list.
		/// </summary>
		private void RefreshBulkCategoryList() {
			lstBulkCategories.Items.Clear();

			string cp = providerSelector.SelectedProvider;
			NamespaceInfo nspace = Pages.FindNamespace(lstNamespace.SelectedValue);
			var categories =
				from c in Pages.GetCategories(nspace)
				where c.Provider.GetType().FullName == cp
				select c;

			foreach(CategoryInfo cat in categories) {
				ListItem item = new ListItem(NameTools.GetLocalName(cat.FullName), cat.FullName);
				lstBulkCategories.Items.Add(item);
			}
		}

		protected void btnBulkSave_Click(object sender, EventArgs e) {
			lblBulkResult.CssClass = "";
			lblBulkResult.Text = "";

			List<PageInfo> selectedPages = new List<PageInfo>(20);
			foreach(string pg in pageListBuilder.SelectedPages) {
				PageInfo page = Pages.FindPage(pg);
				if(page != null) selectedPages.Add(page);
			}

			List<CategoryInfo> selectedCategories = new List<CategoryInfo>(lstBulkCategories.Items.Count);
			foreach(ListItem item in lstBulkCategories.Items) {
				if(item.Selected) {
					CategoryInfo cat = Pages.FindCategory(item.Value);
					if(cat != null) selectedCategories.Add(cat);
				}
			}

			if(selectedPages.Count == 0) {
				lblBulkResult.CssClass = "resulterror";
				lblBulkResult.Text = Properties.Messages.NoPages;
				return;
			}

			if(rdoBulkAdd.Checked && selectedCategories.Count == 0) {
				lblBulkResult.CssClass = "resulterror";
				lblBulkResult.Text = Properties.Messages.NoCategories;
				return;
			}

			Log.LogEntry("Bulk rebind requested", EntryType.General, SessionFacade.CurrentUsername);

			foreach(PageInfo page in selectedPages) {
				CategoryInfo[] cats = null;
				if(rdoBulkAdd.Checked) {
					// Merge selected categories with previous ones
					List<CategoryInfo> existing = new List<CategoryInfo>(Pages.GetCategoriesForPage(page));

					foreach(CategoryInfo newCat in selectedCategories) {
						if(existing.Find((c) => { return c.FullName == newCat.FullName; }) == null) {
							existing.Add(newCat);
						}
					}

					existing.Sort(new CategoryNameComparer());

					cats = existing.ToArray();
				}
				else {
					// Replace old binding
					cats = selectedCategories.ToArray();
				}

				Pages.Rebind(page, cats);
			}

			lblBulkResult.CssClass = "resultok";
			lblBulkResult.Text = Properties.Messages.BulkRebindCompleted;
		}

	}

	/// <summary>
	/// Represents a category for display purposes.
	/// </summary>
	public class CategoryRow {

		private string fullName, pageCount, provider, additionalClass;
		private bool canSelect;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CategoryRow" /> class.
		/// </summary>
		/// <param name="category">The original category.</param>
		/// <param name="canSelect">A value indicating whether the category can be selected.</param>
		/// <param name="selected">A value indicating whether the category is selected.</param>
		public CategoryRow(CategoryInfo category, bool canSelect, bool selected) {
			fullName = category.FullName;
			pageCount = category.Pages.Length.ToString();
			provider = category.Provider.Information.Name;
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
		/// Gets the page count.
		/// </summary>
		public string PageCount {
			get { return pageCount; }
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets a value indicating whether the current category can be selected.
		/// </summary>
		public bool CanSelect {
			get { return canSelect; }
		}

		/// <summary>
		/// Gets the CSS additional class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
