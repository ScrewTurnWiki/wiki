
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text.RegularExpressions;

namespace ScrewTurn.Wiki {

	public partial class Post : BasePage {

		private PageInfo page;
		private PageContent content;

		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.PostTitle + " - " + Settings.WikiTitle;

			if(Request["Page"] == null) UrlTools.RedirectHome();
			page = Pages.FindPage(Request["Page"]);
			if(page == null) UrlTools.RedirectHome();
			editor.CurrentPage = page;

			if(page.Provider.ReadOnly) UrlTools.Redirect(UrlTools.BuildUrl(page.FullName, Settings.PageExtension));

			content = Content.GetPageContent(page, true);
			if(!Page.IsPostBack) lblTitle.Text += " - " + FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.MessageBody, page);

			// Verify permissions and setup captcha
			bool canPostMessage = AuthChecker.CheckActionForPage(page, Actions.ForPages.PostDiscussion,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			if(!canPostMessage) UrlTools.Redirect(UrlTools.BuildUrl(Tools.UrlEncode(page.FullName), Settings.PageExtension));
			captcha.Visible = SessionFacade.LoginKey == null && !Settings.DisableCaptchaControl;

			if(Page.IsPostBack) return;

			editor.SetContent("", Settings.UseVisualEditorAsDefault);

			string username = Request.UserHostAddress;
			if(SessionFacade.LoginKey != null) username = SessionFacade.CurrentUsername;

			bool edit = Request["Edit"] != null;

			if(!edit) {
				if(Request["Parent"] != null) {
					try {
						int.Parse(Request["Parent"]);
					}
					catch {
						UrlTools.RedirectHome();
					}
					Message[] messages = Pages.GetPageMessages(page);
					Message parent = Pages.FindMessage(messages, int.Parse(Request["Parent"]));

					if(parent != null) {
						txtSubject.Text = (!parent.Subject.ToLowerInvariant().StartsWith("re:") ? "Re: " : "") + parent.Subject;
					}
				}
			}
			else {
				try {
					int.Parse(Request["Edit"]);
				}
				catch {
					UrlTools.RedirectHome();
				}
				Message[] messages = Pages.GetPageMessages(page);
				Message msg = Pages.FindMessage(messages, int.Parse(Request["Edit"]));

				if(msg != null) {
					txtSubject.Text = msg.Subject;
					editor.SetContent(msg.Body, Settings.UseVisualEditorAsDefault);
				}
				else throw new Exception("Message not found (" + page.FullName + "." + Request["Edit"] + ").");
			}

		}

		protected void btnSend_Click(object sender, EventArgs e) {
			string content = editor.GetContent();

			Page.Validate();

			if(!Page.IsValid || content.Replace(" ", "").Length == 0 || txtSubject.Text.Replace(" ", "").Length == 0) {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.SubjectAndBodyNeeded;
				return;
			}

			Regex r = new Regex(@"\<script.*?\>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			if(r.Match(editor.GetContent()).Success) {
				lblResult.CssClass = "resulterror";
				lblResult.Text = @"<span style=""color: #FF0000;"">" + Properties.Messages.ScriptDetected + "</span>";
				return;
			}

			string username = Request.UserHostAddress;
			if(SessionFacade.LoginKey != null) username = SessionFacade.CurrentUsername;

			if(Request["Edit"] == null) {
				int parent = -1;
				try {
					parent = int.Parse(Request["Parent"]);
				}
				catch { }

				Pages.AddMessage(page, username, txtSubject.Text, DateTime.Now, content, parent);
			}
			else {
				Message[] messages = Pages.GetPageMessages(page);
				Message msg = Pages.FindMessage(messages, int.Parse(Request["Edit"]));
				Pages.ModifyMessage(page, int.Parse(Request["Edit"]), msg.Username, txtSubject.Text, DateTime.Now, content);
			}
			UrlTools.Redirect(page.FullName + Settings.PageExtension + "?Discuss=1&NoRedirect=1");
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			UrlTools.Redirect(page.FullName + Settings.PageExtension + "?Discuss=1&NoRedirect=1");
		}

	}

}
