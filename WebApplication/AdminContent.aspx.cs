
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminContent : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageMetaFiles(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load namespaces

				// Add root namespace
				lstNamespace.Items.Add(new ListItem("<root>", ""));
				List<NamespaceInfo> namespaces = Pages.GetNamespaces();
				foreach(NamespaceInfo ns in namespaces) {
					lstNamespace.Items.Add(new ListItem(ns.Name, ns.Name));
				}
			}
		}

		private static readonly Dictionary<string, MetaDataItem> ButtonMetaDataItemMapping = new Dictionary<string, MetaDataItem>() {
			{ "btnHtmlHead", MetaDataItem.HtmlHead },
			{ "btnHeader", MetaDataItem.Header },
			{ "btnSidebar", MetaDataItem.Sidebar },
			{ "btnPageHeader", MetaDataItem.PageHeader },
			{ "btnPageFooter", MetaDataItem.PageFooter },
			{ "btnFooter", MetaDataItem.Footer },
			{ "btnAccountActivationMessage", MetaDataItem.AccountActivationMessage },
			{ "btnPasswordResetProcedureMessage", MetaDataItem.PasswordResetProcedureMessage },
			{ "btnEditingPageNotice", MetaDataItem.EditNotice },
			{ "btnLoginNotice", MetaDataItem.LoginNotice },
			{ "btnAccessDeniedNotice", MetaDataItem.AccessDeniedNotice },
			{ "btnRegisterNotice", MetaDataItem.RegisterNotice },
			{ "btnPageChangeMessage", MetaDataItem.PageChangeMessage },
			{ "btnDiscussionChangeMessage", MetaDataItem.DiscussionChangeMessage },
			{ "btnApproveDraftMessage", MetaDataItem.ApproveDraftMessage }
		};

		private static List<MetaDataItem> WikiMarkupOnlyItems = new List<MetaDataItem>() {
			MetaDataItem.AccountActivationMessage,
			MetaDataItem.ApproveDraftMessage,
			MetaDataItem.DiscussionChangeMessage,
			MetaDataItem.PageChangeMessage,
			MetaDataItem.PasswordResetProcedureMessage
		};

		protected void btn_Click(object sender, EventArgs e) {
			Control senderControl = sender as Control;
			txtCurrentButton.Value = senderControl.ID;

			MetaDataItem item = ButtonMetaDataItemMapping[senderControl.ID];

			bool markupOnly = WikiMarkupOnlyItems.Contains(item);

			string content = Settings.Provider.GetMetaDataItem(item, lstNamespace.SelectedValue);
			editor.SetContent(content, !markupOnly && Settings.UseVisualEditorAsDefault);

			editor.VisualVisible = !markupOnly;
			editor.PreviewVisible = !markupOnly;
			editor.ToolbarVisible = !markupOnly;

			pnlList.Visible = false;
			pnlEditor.Visible = true;

			// Load namespaces for content copying
			lstCopyFromNamespace.Items.Clear();
			string currentNamespace = lstNamespace.SelectedValue;
			if(!string.IsNullOrEmpty(currentNamespace)) lstCopyFromNamespace.Items.Add(new ListItem("<root>", ""));
			List<NamespaceInfo> namespaces = Pages.GetNamespaces();
			foreach(NamespaceInfo ns in namespaces) {
				if(currentNamespace != ns.Name) lstCopyFromNamespace.Items.Add(new ListItem(ns.Name, ns.Name));
			}
			pnlInlineTools.Visible = lstCopyFromNamespace.Items.Count > 0 && !Settings.IsMetaDataItemGlobal(item);
		}

		protected void btnCopyFrom_Click(object sender, EventArgs e) {
			MetaDataItem item = ButtonMetaDataItemMapping[txtCurrentButton.Value];

			if(Settings.IsMetaDataItemGlobal(item)) return;

			string newValue = Settings.Provider.GetMetaDataItem(item, lstCopyFromNamespace.SelectedValue);

			editor.SetContent(newValue, Settings.UseVisualEditorAsDefault);
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			MetaDataItem item = ButtonMetaDataItemMapping[txtCurrentButton.Value];

			string tag = null;
			// These elements are global, all others are are namespace-specific
			if(!Settings.IsMetaDataItemGlobal(item)) {
				tag = lstNamespace.SelectedValue;
			}

			Log.LogEntry("Metadata file change requested for " + item.ToString() +
				(tag != null ? ", ns: " + tag : "") + lstNamespace.SelectedValue, EntryType.General, SessionFacade.CurrentUsername);

			Settings.Provider.SetMetaDataItem(item, tag, editor.GetContent());
			Content.ClearPseudoCache();

			pnlEditor.Visible = false;
			pnlList.Visible = true;
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			pnlEditor.Visible = false;
			pnlList.Visible = true;
		}

	}

}
