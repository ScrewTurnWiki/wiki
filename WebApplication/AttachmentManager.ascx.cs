
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AttachmentManager : System.Web.UI.UserControl {

		private IFilesStorageProviderV30 provider;

		private bool canDownload = false;
		private bool canUpload = false;
		private bool canDelete = false;
		private bool isAdmin = false;

		protected void Page_Load(object sender, EventArgs e) {

			if(!Page.IsPostBack) {
				// Localized strings for JavaScript
				StringBuilder sb = new StringBuilder();
				sb.Append(@"<script type=""text/javascript"">" + "\r\n<!--\n");
				sb.Append("var ConfirmMessage = '");
				sb.Append(Properties.Messages.ConfirmOperation);
				sb.Append("';\r\n");
				sb.AppendFormat("var UploadControl = '{0}';\r\n", fileUpload.ClientID);
				//sb.AppendFormat("var RefreshCommandParameter = '{0}';\r\n", btnRefresh.UniqueID);
				sb.AppendFormat("var OverwriteControl = '{0}';\r\n", chkOverwrite.ClientID);
				sb.Append("// -->\n</script>\n");
				lblStrings.Text = sb.ToString();

				// Setup upload information (max file size, allowed file types)
				lblUploadFilesInfo.Text = lblUploadFilesInfo.Text.Replace("$1", Tools.BytesToString(Settings.MaxFileSize * 1024));
				sb = new StringBuilder();
				string[] aft = Settings.AllowedFileTypes;
				for(int i = 0; i < aft.Length; i++) {
					sb.Append(aft[i].ToUpper());
					if(i != aft.Length - 1) sb.Append(", ");
				}
				lblUploadFilesInfo.Text = lblUploadFilesInfo.Text.Replace("$2", sb.ToString());

				// Load Providers
				foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
					ListItem item = new ListItem(prov.Information.Name, prov.GetType().FullName);
					if(item.Value == Settings.DefaultFilesProvider) {
						item.Selected = true;
					}
					lstProviders.Items.Add(item);
				}

				if(CurrentPage == null) btnUpload.Enabled = false;
			}

			// Set provider
			provider = Collectors.FilesProviderCollector.GetProvider(lstProviders.SelectedValue);

			if(!Page.IsPostBack) {
				rptItems.DataBind();
			}

			DetectPermissions();
			SetupControls();
		}

		/// <summary>
		/// Detects the permissions of the current user.
		/// </summary>
		private void DetectPermissions() {
			if(CurrentPage != null) {
				string currentUser = SessionFacade.GetCurrentUsername();
				string[] currentGroups = SessionFacade.GetCurrentGroupNames();
				canDownload = AuthChecker.CheckActionForPage(CurrentPage, Actions.ForPages.DownloadAttachments, currentUser, currentGroups);
				canUpload = AuthChecker.CheckActionForPage(CurrentPage, Actions.ForPages.UploadAttachments, currentUser, currentGroups);
				canDelete = AuthChecker.CheckActionForPage(CurrentPage, Actions.ForPages.DeleteAttachments, currentUser, currentGroups);
				isAdmin = Array.Find(currentGroups, delegate(string g) { return g == Settings.AdministratorsGroup; }) != null;
			}
			else {
				canDownload = false;
				canUpload = false;
				canDelete = false;
				isAdmin = false;
			}
			lstProviders.Visible = isAdmin;
		}

		/// <summary>
		/// Sets up buttons and controls using the permissions.
		/// </summary>
		private void SetupControls() {
			if(!canUpload) {
				btnUpload.Enabled = false;
			}
			if(!canDelete) {
				chkOverwrite.Enabled = false;
			}
		}

		/// <summary>
		/// Gets or sets the PageInfo object.
		/// </summary>
		/// <remarks>This property must be set at page load.</remarks>
		public PageInfo CurrentPage {
			get { return Pages.FindPage(ViewState["CP"] as string); }
			set {
				if(value == null) ViewState["CP"] = null;
				else ViewState["CP"] = value.FullName;
				btnUpload.Enabled = value != null;
				lblNoUpload.Visible = !btnUpload.Enabled;
				DetectPermissions();
				SetupControls();
				rptItems.DataBind();
			}
		}

		protected void rptItems_DataBinding(object sender, EventArgs e) {
			provider = Collectors.FilesProviderCollector.GetProvider(lstProviders.SelectedValue);

			if(provider == null || CurrentPage == null) {
				return;
			}

			// Build a DataTable containing the proper information
			DataTable table = new DataTable("Items");

			table.Columns.Add("Name");
			table.Columns.Add("Size");
			table.Columns.Add("Editable", typeof(bool));
			table.Columns.Add("Page");
			table.Columns.Add("Link");
			table.Columns.Add("Downloads");
			table.Columns.Add("CanDelete", typeof(bool));
			table.Columns.Add("CanDownload", typeof(bool));

			string[] attachments = provider.ListPageAttachments(CurrentPage);
			foreach(string s in attachments) {
				FileDetails details = provider.GetPageAttachmentDetails(CurrentPage, s);

				DataRow row = table.NewRow();
				string ext = Path.GetExtension(s).ToLowerInvariant();
				row["Name"] = s;
				row["Size"] = Tools.BytesToString(details.Size);
				row["Editable"] = canUpload && canDelete && (ext == ".jpg" || ext == ".jpeg" || ext == ".png");
				row["Page"] = CurrentPage.FullName;
				if(canDownload) {
					row["Link"] = "GetFile.aspx?File=" + Tools.UrlEncode(s).Replace("'", "&#39;") + "&amp;AsStreamAttachment=1&amp;Provider=" +
						provider.GetType().FullName + "&amp;IsPageAttachment=1&amp;Page=" +
						Tools.UrlEncode(CurrentPage.FullName) + "&amp;NoHit=1";
				}
				else {
					row["Link"] = "";
				}
				row["Downloads"] = details.RetrievalCount.ToString();
				row["CanDelete"] = canDelete;
				row["CanDownload"] = canDownload;
				table.Rows.Add(row);
			}

			rptItems.DataSource = table;
		}

		protected void btnRefresh_Click(object sender, EventArgs e) {
			rptItems.DataBind();
		}

		protected void btnUpload_Click(object sender, EventArgs e) {
			if(canUpload) {
				lblUploadResult.Text = "";				
				if(fileUpload.HasFile) {
					if(fileUpload.FileBytes.Length > Settings.MaxFileSize * 1024) {
						lblUploadResult.Text = Properties.Messages.FileTooBig;
						lblUploadResult.CssClass = "resulterror";
					}
					else {
						// Check file extension
						string[] aft = Settings.AllowedFileTypes;
						bool allowed = false;

						if(aft.Length > 0 && aft[0] == "*") allowed = true;
						else {
							string ext = Path.GetExtension(fileUpload.FileName);
							if(ext == null) ext = "";
							if(ext.StartsWith(".")) ext = ext.Substring(1).ToLowerInvariant();
							foreach(string ft in aft) {
								if(ft == ext) {
									allowed = true;
									break;
								}
							}
						}

						if(!allowed) {
							lblUploadResult.Text = Properties.Messages.InvalidFileType;
							lblUploadResult.CssClass = "resulterror";
						}
						else {
							// Store attachment
							bool done = provider.StorePageAttachment(CurrentPage, fileUpload.FileName, fileUpload.FileContent, chkOverwrite.Checked);
							if(!done) {
								lblUploadResult.Text = Properties.Messages.CannotStoreFile;
								lblUploadResult.CssClass = "resulterror";
							}
							else {
								Host.Instance.OnAttachmentActivity(provider.GetType().FullName,
									fileUpload.FileName, CurrentPage.FullName, null, FileActivity.AttachmentUploaded);
							}
							rptItems.DataBind();
						}
					}
				}
				else {
					lblUploadResult.Text = Properties.Messages.FileVoid;
					lblUploadResult.CssClass = "resulterror";
				}
			}
		}

		protected void lstProviders_SelectedIndexChanged(object sender, EventArgs e) {
			provider = Collectors.FilesProviderCollector.GetProvider(lstProviders.SelectedValue);
			rptItems.DataBind();
		}

		protected void rptItems_ItemCommand(object sender, RepeaterCommandEventArgs e) {
			// Raised when a ButtonField is clicked

			switch(e.CommandName) {
				case "Rename":
					if(canDelete) {
						pnlRename.Visible = true;
						lblItem.Text = (string)e.CommandArgument;
						txtNewName.Text = (string)e.CommandArgument;
						rptItems.Visible = false;
					}
					break;
				case "Delete":
					if(canDelete) {
						// Delete Attachment
						bool d = provider.DeletePageAttachment(CurrentPage, (string)e.CommandArgument);

						if(d) {
							Host.Instance.OnAttachmentActivity(provider.GetType().FullName,
								(string)e.CommandArgument, CurrentPage.FullName, null, FileActivity.AttachmentDeleted);
						}

						rptItems.DataBind();
					}
					break;
			}
		}

		protected void btnRename_Click(object sender, EventArgs e) {
			if(canDelete) {
				lblRenameResult.Text = "";

				txtNewName.Text = txtNewName.Text.Trim();

				// Ensure that the extension is not changed (security)
				string previousExtension = Path.GetExtension(lblItem.Text);
				string newExtension = Path.GetExtension(txtNewName.Text);
				if(string.IsNullOrEmpty(newExtension)) {
					newExtension = previousExtension;
					txtNewName.Text += previousExtension;
				}

				if(newExtension.ToLowerInvariant() != previousExtension.ToLowerInvariant()) {
					txtNewName.Text += previousExtension;
				}

				txtNewName.Text = txtNewName.Text.Trim();

				bool done = true;
				if(txtNewName.Text.ToLowerInvariant() != lblItem.Text.ToLowerInvariant()) {
					done = provider.RenamePageAttachment(CurrentPage, lblItem.Text, txtNewName.Text);
				}

				if(done) {
					pnlRename.Visible = false;
					rptItems.Visible = true;
					rptItems.DataBind();

					Host.Instance.OnAttachmentActivity(provider.GetType().FullName,
						txtNewName.Text, CurrentPage.FullName, lblItem.Text, FileActivity.AttachmentRenamed);
				}
				else {
					lblRenameResult.Text = Properties.Messages.CannotRenameItem;
					lblRenameResult.CssClass = "resulterror";
				}
			}
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			pnlRename.Visible = false;
			rptItems.Visible = true;
			lblRenameResult.Text = "";
		}

	}

}
