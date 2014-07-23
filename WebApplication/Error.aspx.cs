
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
using System.Security.Cryptography;
using System.Threading;

namespace ScrewTurn.Wiki {

	public partial class Error : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.ErrorTitle + " - " + Settings.WikiTitle;

			Exception ex = Session["LastError"] as Exception;
			if(ex != null && SessionFacade.LoginKey != null &&
				AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) {

				lblException.Text = ex.ToString();
			}
			else {
				pnlException.Visible = false;
			}
			Session["LastError"] = null;

			// Workaround for ASP.NET vulnerability
			// http://weblogs.asp.net/scottgu/archive/2010/09/18/important-asp-net-security-vulnerability.aspx
			byte[] delay = new byte[1];
			RandomNumberGenerator prng = new RNGCryptoServiceProvider();

			prng.GetBytes(delay);
			Thread.Sleep((int)delay[0]);

			IDisposable disposable = prng as IDisposable;
			if(disposable != null) { disposable.Dispose(); }

		}

	}

}
