
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

	public partial class RandPage : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			List<PageInfo> pages = Pages.GetPages(Tools.DetectCurrentNamespaceInfo());
			Random r = new Random();
			UrlTools.Redirect(pages[r.Next(0, pages.Count)].FullName + Settings.PageExtension);
		}

	}

}
