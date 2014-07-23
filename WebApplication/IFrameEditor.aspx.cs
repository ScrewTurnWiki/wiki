
using System;
using System.Web.UI.WebControls;

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
