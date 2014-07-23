
using System;

namespace ScrewTurn.Wiki {

	public partial class Admin : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			Response.Redirect("AdminHome.aspx");
		}

	}

}
