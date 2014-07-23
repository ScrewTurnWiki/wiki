
using System;
using System.Collections.Generic;
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
