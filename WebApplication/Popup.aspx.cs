
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class Popup : BasePage {

		private PageInfo currentPage = null;

		protected void Page_Load(object sender, EventArgs e) {
			Literal l = new Literal();
			l.Text = Tools.GetIncludes(DetectNamespace());
			Page.Header.Controls.AddAt(0, l);

			if(string.IsNullOrEmpty(Request["Feature"])) return;

			// Get instance of Current Page, if any
			if(!string.IsNullOrEmpty(Request["CurrentPage"])) {
				currentPage = Pages.FindPage(Request["CurrentPage"]);
			}
			else currentPage = null;

			if(!Page.IsPostBack) {

				// Load FilesStorageProviders
				IFilesStorageProviderV30[] provs = Collectors.FilesProviderCollector.AllProviders;
				foreach(IFilesStorageProviderV30 p in provs) {
					lstProviderFiles.Items.Add(new ListItem(p.Information.Name, p.GetType().FullName));
					// Select the default files provider
					if (p.GetType().FullName == Settings.DefaultFilesProvider) {
						lstProviderFiles.Items[lstProviderFiles.Items.Count - 1].Selected = true;
					}
					lstProviderImages.Items.Add(new ListItem(p.Information.Name, p.GetType().FullName));
					// Select the default images provider
					if (p.GetType().FullName == Settings.DefaultFilesProvider) {
						lstProviderImages.Items[lstProviderImages.Items.Count - 1].Selected = true;
					}

				}

				// Load namespaces
				string currentNamespace = DetectNamespace();
				if(string.IsNullOrEmpty(currentNamespace)) currentNamespace = "";
				lstNamespace.Items.Clear();
				lstNamespace.Items.Add(new ListItem("<root>", ""));
				foreach(NamespaceInfo ns in Pages.GetNamespaces()) {
					lstNamespace.Items.Add(new ListItem(ns.Name, ns.Name));
				}
				foreach(ListItem itm in lstNamespace.Items) {
					if(itm.Value == currentNamespace) {
						itm.Selected = true;
						break;
					}
				}

				// Enable/disable page attachments feature
				chkFilesAttachments.Visible = currentPage != null;
				chkImageAttachments.Visible = currentPage != null;

				SetupFeature();
			}
		}

		private string GenerateWindowResizeCode(int width, int height) {
			return string.Format("window.resizeTo({0}, {1});\r\n", width, height);
		}

		private void SetupFeature() {
			int width = 250, height = 150;

			string temp = Request["Feature"];
			string feature = "", parms = "";
			if(temp.Contains("$")) {
				feature = temp.Substring(0, temp.IndexOf("$")).ToLowerInvariant();
				parms = temp.Substring(temp.IndexOf("$") + 1);
			}
			else {
				feature = temp.ToLowerInvariant();
			}

			switch(feature) {
				case "pagelink":
					mlvPopup.ActiveViewIndex = 0;
					ctPages.PopulateTree();
					width = 300;
					height = 470;
					break;
				case "filelink":
					mlvPopup.ActiveViewIndex = 1;
					ctFiles.PopulateTree();
					width = 300;
					height = 510;
					break;
				case "externallink":
					mlvPopup.ActiveViewIndex = 2;
					width = 310;
					height = 230;
					break;
				case "image":
					mlvPopup.ActiveViewIndex = 3;
					cibImages.PopulateBrowser();
					width = 640;
					height = 500;
					break;
				case "anchor":
					mlvPopup.ActiveViewIndex = 4;
					width = 310;
					height = 270;
					// Extract existing anchors from parms
					string[] anchors = parms.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
					lstExistingAnchors.Items.Clear();
					foreach(string a in anchors) {
						lstExistingAnchors.Items.Add(new ListItem(a, a));
					}
					break;
			}

			string currentNamespace = DetectNamespace();
			if(string.IsNullOrEmpty(currentNamespace)) currentNamespace = "";

			StringBuilder sb = new StringBuilder(100);
			sb.Append("<script type=\"text/javascript\">\r\n<!--\r\n");
			sb.Append(GenerateWindowResizeCode(width, height));
			sb.AppendFormat("var LinkUrl = \"{0}\";\r\n", Properties.Messages.LinkUrl);
			sb.AppendFormat("var LinkTitle = \"{0}\";\r\n", Properties.Messages.LinkTitleOptional);
			sb.AppendFormat("var AnchorID = \"{0}\";\r\n", Properties.Messages.AnchorId);
			//sb.AppendFormat("var PageLinkPrefix = \"{0}\";\r\n", lstNamespace.SelectedValue != currentNamespace ? "++" : "");
			sb.Append("// -->\r\n</script>\r\n");
			lblStrings.Text = sb.ToString();
		}

		protected void lstNamespace_SelectedIndexChanged(object sender, EventArgs e) {
			SetupFeature();
		}

		#region PageLink

		protected List<TreeElement> ctPages_Populate(object sender, PopulateEventArgs e) {
			string currentNamespace = DetectNamespace();
			if(string.IsNullOrEmpty(currentNamespace)) currentNamespace = null;

			List<TreeElement> result = new List<TreeElement>(100);
			foreach(PageInfo pi in Pages.GetPages(Pages.FindNamespace(lstNamespace.SelectedValue))) {
				string pageNamespace = NameTools.GetNamespace(pi.FullName);
				if(string.IsNullOrEmpty(pageNamespace)) pageNamespace = null;
				PageContent cont = Content.GetPageContent(pi, true);
				string formattedTitle = FormattingPipeline.PrepareTitle(cont.Title, false, FormattingContext.Other, pi);
				string onClickJavascript = "javascript:";
				// Populate the page title box if the title is different to the page name
				if (pi.FullName != cont.Title) {
					// Supply the page title to the Javascript that sets the page title on the page
					// We can safely escape the \ and ' characters, but the " character is interpreted by the browser even if it is escaped to Javascript, so we can't allow it.
					// Instead we replace it with an escaped single quote.
					// Similarly, < on it's own is fine, but causes problems when combined with text and > to form a tag.  Safest to remove < characters to prevent
					// breaking the drop-down.
					onClickJavascript += "SetValue('txtPageTitle', '" + cont.Title.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\'").Replace("<", "") + "');";
				}
				else {
					onClickJavascript += "SetValue('txtPageTitle', '');";
				}
				// Populate the page name
				onClickJavascript += "return SetValue('txtPageName', '" +
					((pageNamespace == currentNamespace ? NameTools.GetLocalName(pi.FullName) : ("++" + pi.FullName))).Replace("++++", "++") + "');";
				TreeElement item = new TreeElement(pi.FullName, formattedTitle, onClickJavascript);
				result.Add(item);
			}
			return result;
		}

		#endregion

		#region FileLink

		protected void chkFilesAttachments_CheckedChanged(object sender, EventArgs e) {
			ctFiles.PopulateTree();
		}

		protected List<TreeElement> ctFiles_Populate(object sender, PopulateEventArgs e) {
			IFilesStorageProviderV30 p = Collectors.FilesProviderCollector.GetProvider(lstProviderFiles.SelectedValue);
			return BuildFilesSubTree(p, "/");
		}

		private List<TreeElement> BuildFilesSubTree(IFilesStorageProviderV30 provider, string path) {
			string[] dirs = new string[0];
			string[] files = new string[0];

			if(chkFilesAttachments.Checked) {
				// Load page attachments
				files = provider.ListPageAttachments(currentPage);
			}
			else {
				// Load files
				dirs = provider.ListDirectories(path);
				files = provider.ListFiles(path);
			}

			List<TreeElement> result = new List<TreeElement>(100);

			foreach(string d in dirs) {
				TreeElement item = new TreeElement(d, Tools.ExtractDirectoryName(d),
					BuildFilesSubTree(provider, d));
				// Do not display empty folders to reduce "noise"
				if(item.SubItems.Count > 0) {
					result.Add(item);
				}
			}

			foreach(string f in files) {
				long size = chkFilesAttachments.Checked ? provider.GetPageAttachmentDetails(currentPage, f).Size : provider.GetFileDetails(f).Size;
				TreeElement item = new TreeElement(f, f.Substring(f.LastIndexOf("/") + 1) + " (" + Tools.BytesToString(size) + ")",
					"javascript:return SelectFile('" +
					(chkFilesAttachments.Checked ? "(" + currentPage.FullName + ")" : "") + "', '" + f.Replace("'", "\\'") + "');");
				result.Add(item);
			}

			return result;
		}

		protected void lstProviderFiles_SelectedIndexChanged(object sender, EventArgs e) {
			ctFiles.PopulateTree();
			txtFilePath.Text = "";
		}

		#endregion

		#region Image

		protected void chkImageAttachments_CheckedChanged(object sender, EventArgs e) {
			cibImages.PopulateBrowser();
		}

		protected List<TreeElement> cibImages_Populate(object sender, PopulateEventArgs e) {
			IFilesStorageProviderV30 p = Collectors.FilesProviderCollector.GetProvider(lstProviderImages.SelectedValue);
			return BuildImagesSubTree(p, "/");
		}

		private List<TreeElement> BuildImagesSubTree(IFilesStorageProviderV30 provider, string path) {
			string[] dirs = new string[0];
			string[] files = new string[0];

			if(chkImageAttachments.Checked) {
				// Load page attachments
				files = provider.ListPageAttachments(currentPage);
			}
			else {
				// Load files
				dirs = provider.ListDirectories(path);
				files = provider.ListFiles(path);
			}

			List<TreeElement> result = new List<TreeElement>(100);

			foreach(string d in dirs) {
				TreeElement item = new TreeElement(d, Tools.ExtractDirectoryName(d),
					BuildImagesSubTree(provider, d));
				// Do not display empty folders to reduce "noise"
				if(item.SubItems.Count > 0) {
					result.Add(item);
				}
			}

			foreach(string f in files) {
				if(IsImage(f)) {
					string name = provider.GetType().ToString() + "|" + f;
					TreeElement item = new TreeElement(name,
						@"<img src=""Thumb.aspx?Provider=" + provider.GetType().ToString() +
						@"&amp;Size=Small&amp;File=" + Tools.UrlEncode(f) +
						@"&amp;Page=" + (chkImageAttachments.Checked ? Tools.UrlEncode(currentPage.FullName) : "") +
						@""" alt=""" + name + @""" /><span class=""imageinfo"">" + f.Substring(f.LastIndexOf("/") + 1) + "</span>",
						"javascript:return SelectImage('" +
						(chkImageAttachments.Checked ? "(" + currentPage.FullName + ")" : "") + "', '" + f + "', '" +
						(chkImageAttachments.Checked ? currentPage.FullName : "") + "');");
					result.Add(item);
				}
			}

			return result;
		}

		private bool IsImage(string name) {
			string ext = System.IO.Path.GetExtension(name.ToLowerInvariant());
			return ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".png" || ext == ".tif" || ext == ".tiff" || ext == ".bmp";
		}

		protected void lstProviderImages_SelectedIndexChanged(object sender, EventArgs e) {
			cibImages.PopulateBrowser();
			txtImagePath.Text = "";
			txtImageLink.Text = "";
		}

		#endregion

		#region Anchor

		protected void rdoAnchor_CheckedChanged(object sender, EventArgs e) {
			if(rdoNewAnchor.Checked) {
				pnlNewAnchor.Visible = true;
				pnlAnchorLink.Visible = false;
			}
			else {
				pnlNewAnchor.Visible = false;
				pnlAnchorLink.Visible = true;
			}
		}

		#endregion

	}

}
