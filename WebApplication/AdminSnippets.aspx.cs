
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminSnippets : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageSnippetsAndTemplates(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load snippets
				rptSnippetsTemplates.DataBind();
			}
		}

		protected void rptSnippetsTemplates_DataBinding(object sender, EventArgs e) {
			List<Snippet> snippets = Snippets.GetSnippets();
			List<ContentTemplate> templates = Templates.GetTemplates();

			List<SnippetTemplateRow> result = new List<SnippetTemplateRow>(snippets.Count + templates.Count);

			foreach(Snippet snip in snippets) {
				result.Add(new SnippetTemplateRow(snip, "S." + snip.Name == txtCurrentElement.Value));
			}
			foreach(ContentTemplate temp in templates) {
				result.Add(new SnippetTemplateRow(temp, "T." + temp.Name == txtCurrentElement.Value));
			}

			rptSnippetsTemplates.DataSource = result;
		}

		protected void rptSnippetsTemplates_ItemCommand(object sender, CommandEventArgs e) {
			if(e.CommandName == "Select") {
				txtCurrentElement.Value = e.CommandArgument as string;

				if(txtCurrentElement.Value.StartsWith("S.")) SelectSnippet(txtCurrentElement.Value.Substring(2));
				else SelectTemplate(txtCurrentElement.Value.Substring(2));

				providerSelector.Enabled = false;
				txtName.Text = txtCurrentElement.Value.Substring(2);
				txtName.Enabled = false;

				btnCreate.Visible = false;
				btnSave.Visible = true;
				btnDelete.Visible = true;
				pnlList.Visible = false;
				pnlEditElement.Visible = true;

				lblResult.CssClass = "";
				lblResult.Text = "";
			}
		}

		/// <summary>
		/// Sets the editing area title for a snippet.
		/// </summary>
		private void SetTitleForSnippet() {
			lblEditTitleSnippet.Visible = true;
			lblEditTitleTemplate.Visible = false;
		}

		/// <summary>
		/// Sets the editing area title for a template.
		/// </summary>
		private void SetTitleForTemplate() {
			lblEditTitleSnippet.Visible = false;
			lblEditTitleTemplate.Visible = true;
		}

		/// <summary>
		/// Selects a snippet for editing.
		/// </summary>
		/// <param name="name">The name of the snippet.</param>
		private void SelectSnippet(string name) {
			Snippet snippet = Snippets.Find(name);
			providerSelector.SelectedProvider = snippet.Provider.GetType().FullName;
			editor.SetContent(snippet.Content, Settings.UseVisualEditorAsDefault);
			lblEditSnippetWarning.Visible = true;
			SetTitleForSnippet();
		}

		/// <summary>
		/// Selects a template for editing.
		/// </summary>
		/// <param name="name">The name of the template.</param>
		private void SelectTemplate(string name) {
			ContentTemplate template = Templates.Find(name);
			providerSelector.SelectedProvider = template.Provider.GetType().FullName;
			editor.SetContent(template.Content, Settings.UseVisualEditorAsDefault);
			SetTitleForTemplate();
		}

		protected void btnNewSnippet_Click(object sender, EventArgs e) {
			pnlList.Visible = false;
			pnlEditElement.Visible = true;

			editor.SetContent("", Settings.UseVisualEditorAsDefault);
			SetTitleForSnippet();
			txtCurrentElement.Value = "S";

			lblResult.Text = "";
			lblResult.CssClass = "";
		}

		protected void btnNewTemplate_Click(object sender, EventArgs e) {
			pnlList.Visible = false;
			pnlEditElement.Visible = true;

			editor.SetContent("", Settings.UseVisualEditorAsDefault);
			SetTitleForTemplate();
			txtCurrentElement.Value = "T";

			lblResult.Text = "";
			lblResult.CssClass = "";
		}

		protected void cvName_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtName.Text);
		}

		protected void btnCreate_Click(object sender, EventArgs e) {
			lblResult.CssClass = "";
			lblResult.Text = "";

			if(!Page.IsValid) return;

			txtName.Text = txtName.Text.Trim();

			if(txtCurrentElement.Value == "S") CreateSnippet();
			else CreateTemplate();
		}

		/// <summary>
		/// Creates a snippet.
		/// </summary>
		private void CreateSnippet() {
			Log.LogEntry("Snippet creation requested for " + txtName.Text, EntryType.General, Log.SystemUsername);

			if(Snippets.AddSnippet(txtName.Text, editor.GetContent(),
				Collectors.PagesProviderCollector.GetProvider(providerSelector.SelectedProvider))) {

				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.SnippetCreated;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotCreateSnippet;
			}
		}

		/// <summary>
		/// Creates a template.
		/// </summary>
		private void CreateTemplate() {
			Log.LogEntry("Content Template creation requested for " + txtName.Text, EntryType.General, Log.SystemUsername);

			if(Templates.AddTemplate(txtName.Text, editor.GetContent(),
				Collectors.PagesProviderCollector.GetProvider(providerSelector.SelectedProvider))) {

				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.TemplateCreated;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotCreateTemplate;
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			lblResult.CssClass = "";
			lblResult.Text = "";

			if(txtCurrentElement.Value.StartsWith("S.")) SaveSnippet(txtCurrentElement.Value.Substring(2));
			else SaveTemplate(txtCurrentElement.Value.Substring(2));
		}

		/// <summary>
		/// Saves a snippet.
		/// </summary>
		/// <param name="name">The name of the snippet to save.</param>
		private void SaveSnippet(string name) {
			Snippet snippet = Snippets.Find(name);

			Log.LogEntry("Snippet modification requested for " + name, EntryType.General, Log.SystemUsername);

			if(Snippets.ModifySnippet(snippet, editor.GetContent())) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.SnippetSaved;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotSaveSnippet;
			}
		}

		/// <summary>
		/// Saves a template.
		/// </summary>
		/// <param name="name">The name of the template to save.</param>
		private void SaveTemplate(string name) {
			ContentTemplate template = Templates.Find(name);

			Log.LogEntry("Content Template modification requested for " + name, EntryType.General, Log.SystemUsername);

			if(Templates.ModifyTemplate(template, editor.GetContent())) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.TemplateSaved;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotSaveTemplate;
			}
		}

		protected void btnDelete_Click(object sender, EventArgs e) {
			lblResult.CssClass = "";
			lblResult.Text = "";

			if(txtCurrentElement.Value.StartsWith("S.")) DeleteSnippet(txtCurrentElement.Value.Substring(2));
			else DeleteTemplate(txtCurrentElement.Value.Substring(2));
		}

		/// <summary>
		/// Deletes a snippet.
		/// </summary>
		/// <param name="name">The name of the snippet to delete.</param>
		private void DeleteSnippet(string name) {
			Snippet snippet = Snippets.Find(name);

			Log.LogEntry("Snippet deletion requested for " + name, EntryType.General, Log.SystemUsername);

			if(Snippets.RemoveSnippet(snippet)) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.SnippetDeleted;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotDeleteSnippet;
			}
		}

		/// <summary>
		/// Deletes a template.
		/// </summary>
		/// <param name="name">The name of the template to delete.</param>
		private void DeleteTemplate(string name) {
			ContentTemplate snippet = Templates.Find(name);

			Log.LogEntry("Content Template deletion requested for " + name, EntryType.General, Log.SystemUsername);

			if(Templates.RemoveTemplate(snippet)) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.TemplateDeleted;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotDeleteTemplate;
			}
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			RefreshList();
			ReturnToList();
		}

		/// <summary>
		/// Returns to the accounts list.
		/// </summary>
		private void ReturnToList() {
			pnlEditElement.Visible = false;
			pnlList.Visible = true;
		}

		/// <summary>
		/// Refreshes the users list.
		/// </summary>
		private void RefreshList() {
			txtCurrentElement.Value = "";
			ResetEditor();
			rptSnippetsTemplates.DataBind();
		}

		/// <summary>
		/// Resets the account editor.
		/// </summary>
		private void ResetEditor() {
			providerSelector.Enabled = true;
			txtName.Text = "";
			txtName.Enabled = true;
			editor.SetContent("", Settings.UseVisualEditorAsDefault);

			btnCreate.Visible = true;
			btnSave.Visible = false;
			btnDelete.Visible = false;
			lblResult.Text = "";
			lblResult.CssClass = "";

			lblEditSnippetWarning.Visible = false;
		}

	}

	/// <summary>
	/// Represents a snippet for display purposes.
	/// </summary>
	public class SnippetTemplateRow {

		private string type, name, distinguishedName, parameterCount, provider, additionalClass;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SnippetTemplateRow" /> class.
		/// </summary>
		/// <param name="snippet">The original snippet.</param>
		/// <param name="selected">A value indicating whether the snippet is selected.</param>
		public SnippetTemplateRow(Snippet snippet, bool selected) {
			type = Properties.Messages.Snippet;
			name = snippet.Name;
			distinguishedName = "S." + snippet.Name;
			parameterCount = Snippets.CountParameters(snippet).ToString();
			provider = snippet.Provider.Information.Name;
			additionalClass = selected ? " selected" : "";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SnippetTemplateRow" /> class.
		/// </summary>
		/// <param name="template">The original template.</param>
		/// <param name="selected">A value indicating whether the template is selected.</param>
		public SnippetTemplateRow(ContentTemplate template, bool selected) {
			type = Properties.Messages.Template;
			name = template.Name;
			distinguishedName = "T." + template.Name;
			parameterCount = "";
			provider = template.Provider.Information.Name;
			additionalClass = selected ? " selected" : "";
		}

		/// <summary>
		/// Gets the type identifier.
		/// </summary>
		public string Type {
			get { return type; }
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the distinguished name.
		/// </summary>
		public string DistinguishedName {
			get { return distinguishedName; }
		}

		/// <summary>
		/// Gets the parameter count.
		/// </summary>
		public string ParameterCount {
			get { return parameterCount; }
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets the additional CSS class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
