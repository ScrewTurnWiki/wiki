
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
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class SessionRefresh : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			// Manage the case when the application is for some reason restarted
			// during the editing of a Page or File

			if(Request["Page"] != null) {
				PageInfo page = Pages.FindPage(Request["Page"]);
				if(page == null) return;
				else {
					// The system already authenticates the user, if any, at the request level
					string username = Request.UserHostAddress;
					if(SessionFacade.LoginKey != null) username = SessionFacade.CurrentUsername;
					Collisions.RenewEditingSession(page, username);
				}
			}

		}

		public void PrintRefresh() {
			Response.Write(@"<meta http-equiv=""refresh"" content=""");
			Response.Write(Collisions.EditingSessionTimeout.ToString());
			Response.Write(@""" />");
		}

	}

}
