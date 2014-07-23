
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class Upload : BasePage {

        protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.UploadTitle + " - " + Settings.WikiTitle;

			if(!Page.IsPostBack) {
				string targetDir = Request["Dir"];
				string provider = Request["Provider"];

				if(!string.IsNullOrEmpty(targetDir)) {
					if(string.IsNullOrEmpty(provider)) provider = Settings.DefaultFilesProvider;

					fileManager.TryEnterDirectory(provider, targetDir);
				}
			}
        }

    }

}
