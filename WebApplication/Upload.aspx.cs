
using System;

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
