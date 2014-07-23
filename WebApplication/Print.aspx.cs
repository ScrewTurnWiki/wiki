
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class Print : BasePage {

		PageInfo page = null;
		PageContent content = null;

		protected void Page_Load(object sender, EventArgs e) {
			page = Pages.FindPage(Request["Page"]);
			if(page == null) UrlTools.RedirectHome();

			// Check permissions
			bool canView = false;
			if(Request["Discuss"] == null) {
				canView = AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadPage,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			}
			else {
				canView = AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadDiscussion,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			}
			if(!canView) UrlTools.Redirect("AccessDenied.aspx");

			content = Content.GetPageContent(page, true);

			Literal canonical = new Literal();
			canonical.Text = Tools.GetCanonicalUrlTag(Request.Url.ToString(), page, Pages.FindNamespace(NameTools.GetNamespace(page.FullName)));
			Page.Header.Controls.Add(canonical);

			Page.Title = FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.PageContent, page) + " - " + Settings.WikiTitle;

			PrintContent();
		}

		/// <summary>
		/// Prints the content.
		/// </summary>
		public void PrintContent() {
			StringBuilder sb = new StringBuilder(5000);
			string title = FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.PageContent, page);

			if(Request["Discuss"] == null) {
				string[] categories =
					(from c in Pages.GetCategoriesForPage(page)
					 select NameTools.GetLocalName(c.FullName)).ToArray();

				UserInfo user = Users.FindUser(content.User);

				sb.Append(@"<h1 class=""pagetitle"">");
				sb.Append(title);
				sb.Append("</h1>");
				if(Settings.EnablePageInfoDiv) {
					sb.AppendFormat("<small>{0} {1} {2} {3} &mdash; {4}: {5}</small><br /><br />",
						Properties.Messages.ModifiedOn,
						Preferences.AlignWithTimezone(content.LastModified).ToString(Settings.DateTimeFormat),
						Properties.Messages.By,
						user != null ? Users.GetDisplayName(user) : content.User,
						Properties.Messages.CategorizedAs,
						categories.Length == 0 ? Properties.Messages.Uncategorized : string.Join(", ", categories));
				}
				sb.Append(Content.GetFormattedPageContent(page, true));
			}
			else {
				sb.Append(@"<h1 class=""pagetitle"">");
				sb.Append(title);
				sb.Append(" - Discussion</h1>");
				PrintDiscussion(sb);
			}
			lblContent.Text = sb.ToString();
		}

		/// <summary>
		/// Prints a discussion.
		/// </summary>
		/// <param name="sb">The output <see cref="T:StringBuilder" />.</param>
		private void PrintDiscussion(StringBuilder sb) {
			List<Message> messages = new List<Message>(Pages.GetPageMessages(page));
			if(messages.Count == 0) {
				sb.Append("<i>");
				sb.Append(Properties.Messages.NoMessages);
				sb.Append("</i>");
			}
			else PrintSubtree(messages, null, sb);
		}

		/// <summary>
		/// Prints a subtree of Messages depth-first.
		/// </summary>
		/// <param name="messages">The Messages.</param>
		private void PrintSubtree(IEnumerable<Message> messages, Message parent, StringBuilder sb) {
			foreach(Message msg in messages) {
				sb.Append(@"<div" + (parent != null ? @" class=""messagecontainer""" : @" class=""rootmessagecontainer""") + ">");
				PrintMessage(msg, parent, sb);
				PrintSubtree(msg.Replies, msg, sb);
				sb.Append("</div>");
			}
		}

		/// <summary>
		/// Prints a message.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="parent">The parent message.</param>
		/// <param name="sb">The output <see cref="T:StringBuilder" />.</param>
		private void PrintMessage(Message message, Message parent, StringBuilder sb) {
			// Header
			sb.Append(@"<div class=""messageheader"">");
			sb.Append(Preferences.AlignWithTimezone(message.DateTime).ToString(Settings.DateTimeFormat));
			sb.Append(" ");
			sb.Append(Properties.Messages.By);
			sb.Append(" ");
			sb.Append(Users.UserLink(message.Username));

			// Subject
			if(message.Subject.Length > 0) {
				sb.Append(@"<br /><span class=""messagesubject"">");
				sb.Append(FormattingPipeline.PrepareTitle(message.Subject, false, FormattingContext.MessageBody, page));
				sb.Append("</span>");
			}

			sb.Append("</div>");

			// Body
			sb.Append(@"<div class=""messagebody"">");
			sb.Append(FormattingPipeline.FormatWithPhase3(FormattingPipeline.FormatWithPhase1And2(message.Body, false, FormattingContext.MessageBody, page),
				FormattingContext.MessageBody, page));
			sb.Append("</div>");
		}

	}

}
