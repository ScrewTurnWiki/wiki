using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace ScrewTurn.Wiki {

	public partial class Preview : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			if(Request["Text"] != null) {
				// Context should be available by parsing the UrlReferrer, which
				// might not be available depending on the browser (this page is invoked via XmlHttpRequest)
				Response.Write(FormattingPipeline.FormatWithPhase3(FormattingPipeline.FormatWithPhase1And2(Request["Text"],
					false, ScrewTurn.Wiki.PluginFramework.FormattingContext.Unknown, null), ScrewTurn.Wiki.PluginFramework.FormattingContext.Unknown, null));
				//Response.Write(Formatter.Format(Request["Text"], null));
			}
			else {
				Response.Write("No input text.");
			}
		}

	}

}
