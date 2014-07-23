
using System;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AccessDenied : BasePage {

        protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.AccessDeniedTitle + " - " + Settings.WikiTitle;

			string n = Content.GetPseudoCacheValue("AccessDeniedNotice");
			if(n == null) {
				n = Settings.Provider.GetMetaDataItem(MetaDataItem.AccessDeniedNotice, null);
				if(!string.IsNullOrEmpty(n)) {
					n = FormattingPipeline.FormatWithPhase1And2(n, false, FormattingContext.Other, null);
					Content.SetPseudoCacheValue("AccessDeniedNotice", n);
				}
			}
			if(!string.IsNullOrEmpty(n)) lblDescription.Text = FormattingPipeline.FormatWithPhase3(n, FormattingContext.Other, null);
        }

    }

}
