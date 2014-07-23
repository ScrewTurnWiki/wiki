
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

	public partial class NavPath : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.NavPathTitle + " - " + Settings.WikiTitle;

			LoginTools.VerifyReadPermissionsForCurrentNamespace();

			PrintNavPaths();
		}

		public void PrintNavPaths() {
			StringBuilder sb = new StringBuilder();
			sb.Append("<ul>");
			List<NavigationPath> paths = NavigationPaths.GetNavigationPaths(DetectNamespaceInfo());
			for(int i = 0; i < paths.Count; i++) {
				sb.Append(@"<li><a href=""");
				UrlTools.BuildUrl(sb, "Default.aspx?Page=", Tools.UrlEncode(paths[i].Pages[0]),
					"&amp;NavPath=", Tools.UrlEncode(paths[i].FullName));

				sb.Append(@""">");
				sb.Append(paths[i].FullName);
				sb.Append("</a></li>");
			}
			sb.Append("</ul>");

			lblNavPathList.Text = sb.ToString();
		}

	}

}
