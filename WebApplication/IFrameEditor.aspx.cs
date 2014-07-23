
using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace ScrewTurn.Wiki {

	public partial class IFrameEditor : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			// Inject the proper stylesheet in page head
			Literal l = new Literal();
			l.Text = Tools.GetIncludes(DetectNamespace());
			Page.Header.Controls.Add(l);
		}

	}

}
