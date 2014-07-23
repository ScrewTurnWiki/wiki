
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

namespace ScrewTurn.Wiki {

	public partial class AttachmentViewer : System.Web.UI.UserControl {

		private PageInfo pageInfo;

		protected void Page_Load(object sender, EventArgs e) {
			if(!Page.IsPostBack) {
				rptItems.DataBind();
			}
		}

		/// <summary>
		/// Gets or sets the PageInfo object.
		/// </summary>
		/// <remarks>This property must be set at page load.</remarks>
		public PageInfo PageInfo {
			get { return pageInfo; }
			set { pageInfo = value; }
		}

		protected void rptItems_DataBinding(object sender, EventArgs e) {
			if(pageInfo == null) return;

			// Build a DataTable containing the proper information
			DataTable table = new DataTable("Items");

			table.Columns.Add("Name");
			table.Columns.Add("Size");
			table.Columns.Add("Link");

			foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
				string[] attachments = provider.ListPageAttachments(pageInfo);
				foreach(string s in attachments) {
					DataRow row = table.NewRow();
					row["Name"] = s;
					row["Size"] = Tools.BytesToString(provider.GetPageAttachmentDetails(pageInfo, s).Size);
					row["Link"] = "GetFile.aspx?File=" + Tools.UrlEncode(s).Replace("'", "&#39;") + "&amp;AsStreamAttachment=1&amp;Provider=" +
						provider.GetType().FullName + "&amp;IsPageAttachment=1&amp;Page=" + Tools.UrlEncode(pageInfo.FullName);
					table.Rows.Add(row);
				}
			}

			rptItems.DataSource = table;
		}

	}

}
