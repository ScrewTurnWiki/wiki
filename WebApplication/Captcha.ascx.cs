
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

	public partial class Captcha : UserControl {

		protected void Page_Load(object sender, EventArgs e) {
			if(!Page.IsPostBack) {
				rfvCaptcha.ErrorMessage = Properties.Messages.RequiredField;
				rfvCaptcha.ToolTip = Properties.Messages.RequiredField;
				cvCaptcha.ErrorMessage = Properties.Messages.WrongControlText;
				cvCaptcha.ToolTip = Properties.Messages.WrongControlText;
			}

			if(!Page.IsPostBack) {
				// Generate captcha string
				Random r = new Random();
				string c = "";
				c += (char)r.Next(49, 58); // 1 - 9 (not 0)
				c += (char)r.Next(65, 79); // A - N (not O)
				c += (char)r.Next(97, 111); // a - n (not o)
				c += (char)r.Next(49, 58); // 1 - 9 (not 0)
				c += (char)r.Next(80, 91); // P - Z
				c += (char)r.Next(112, 123); // p - z
				Session["__Captcha"] = c;
			}
		}

		protected void cvCaptcha_ServerValidate(object source, ServerValidateEventArgs args) {
			if(!Settings.DisableCaptchaControl) {
				args.IsValid = txtCaptcha.Text == (string)Session["__Captcha"];
			}
			else {
				args.IsValid = true;
			}
		}

	}

}
