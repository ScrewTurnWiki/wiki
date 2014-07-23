
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class PageDiscussion : System.Web.UI.UserControl {

		private PageInfo currentPage;
		private bool canPostMessages = false;
		private bool canManageDiscussion = false;

		protected void Page_Load(object sender, EventArgs e) {
			if(!Page.IsPostBack) {
				RenderMessages();
			}
		}

		/// <summary>
		/// Gets or sets the current page.
		/// </summary>
		public PageInfo CurrentPage {
			get { return currentPage; }
			set { currentPage = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the current user can post messages.
		/// </summary>
		public bool CanPostMessages {
			get { return canPostMessages; }
			set { canPostMessages = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the current user can manage the discussion.
		/// </summary>
		public bool CanManageDiscussion {
			get { return canManageDiscussion; }
			set { canManageDiscussion = value; }
		}

		/// <summary>
		/// Renders the messages.
		/// </summary>
		private void RenderMessages() {
			if(currentPage == null) return;

			Message[] messages = Pages.GetPageMessages(currentPage);

			if(messages.Length == 0) {
				lblMessages.Text = "<i>" + Properties.Messages.NoMessages + "</i>";
				return;
			}
			else {
				lblMessages.Text = PrintDiscussion();
			}
		}

		/// <summary>
		/// Prints the discussion tree.
		/// </summary>
		/// <returns>The formatted page discussion.</returns>
		private string PrintDiscussion() {
			List<Message> messages = new List<Message>(Pages.GetPageMessages(currentPage));
			if(messages.Count == 0) {
				return "<i>" + Properties.Messages.NoMessages + "</i>";
			}
			else {
				StringBuilder sb = new StringBuilder(10000);
				PrintSubtree(messages, null, sb);
				return sb.ToString();
			}
		}

		/// <summary>
		/// Prints a subtree of Messages depth-first.
		/// </summary>
		/// <param name="messages">The Messages.</param>
		/// <param name="parent">The parent message, or <c>null</c>.</param>
		/// <param name="sb">The output <see cref="T:StringBuilder" />.</param>
		private void PrintSubtree(IEnumerable<Message> messages, Message parent, StringBuilder sb) {
			foreach(Message msg in messages) {
				sb.Append(@"<div");
				sb.Append(parent != null ? @" class=""messagecontainer""" : @" class=""rootmessagecontainer""");
				sb.Append(">");
				PrintMessage(msg, parent, sb);
				PrintSubtree(msg.Replies, msg, sb);
				sb.Append("</div>");
			}
		}

		/// <summary>
		/// Prints a message.
		/// </summary>
		/// <param name="message">The message to print.</param>
		/// <param name="parent">The parent message, or <c>null</c>.</param>
		/// <param name="sb">The output <see cref="T:StringBuilder" />.</param>
		private void PrintMessage(Message message, Message parent, StringBuilder sb) {
			// Print header
			sb.Append(@"<div class=""messageheader"">");
			//sb.AppendFormat(@"<a id=""MSG_{0}""></a>", message.ID);

			if(!currentPage.Provider.ReadOnly) {
				// Print reply/edit/delete buttons only if provider is not read-only
				sb.Append(@"<div class=""reply"">");

				if(canPostMessages) {
					sb.Append(@"<a class=""reply"" href=""");
					sb.Append(UrlTools.BuildUrl("Post.aspx?Page=", Tools.UrlEncode(currentPage.FullName), "&amp;Parent=", message.ID.ToString()));

					sb.Append(@""">");
					sb.Append(Properties.Messages.Reply);
					sb.Append("</a>");
				}

				// If current user is the author of the message or is an admin, print the edit hyperLink
				// A message can be edited only if the user is authenticated - anonymous users cannot edit their messages
				if(SessionFacade.LoginKey != null && ((message.Username == SessionFacade.CurrentUsername && canPostMessages) || canManageDiscussion)) {
					sb.Append(@" <a class=""edit"" href=""");
					sb.Append(UrlTools.BuildUrl("Post.aspx?Page=", Tools.UrlEncode(currentPage.FullName), "&amp;Edit=", message.ID.ToString()));

					sb.Append(@""">");
					sb.Append(Properties.Messages.Edit);
					sb.Append("</a>");
				}

				// If the current user is an admin, print the delete hyperLink
				if(SessionFacade.LoginKey != null && canManageDiscussion) {
					sb.Append(@" <a class=""delete"" href=""");
					sb.Append(UrlTools.BuildUrl("Operation.aspx?Operation=DeleteMessage&amp;Message=", message.ID.ToString(),
						"&amp;Page=", Tools.UrlEncode(currentPage.FullName)));

					sb.Append(@""">");
					sb.Append(Properties.Messages.Delete);
					sb.Append("</a>");
				}
				sb.Append("</div>");
			}

			sb.Append(@"<div>");
			sb.AppendFormat(@"<a id=""{0}"" href=""#{0}"" title=""Permalink"">&#0182;</a> ", Tools.GetMessageIdForAnchor(message.DateTime));

			// Print subject
			if(message.Subject.Length > 0) {
				sb.Append(@"<span class=""messagesubject"">");
				sb.Append(FormattingPipeline.PrepareTitle(message.Subject, false, FormattingContext.MessageBody, currentPage));
				sb.Append("</span>");
			}

			// Print message date/time
			sb.Append(@"<span class=""messagedatetime"">");
			sb.Append(Preferences.AlignWithTimezone(message.DateTime).ToString(Settings.DateTimeFormat));
			sb.Append(" ");
			sb.Append(Properties.Messages.By);
			sb.Append(" ");
			sb.Append(Users.UserLink(message.Username));
			sb.Append("</span>");

			sb.Append("</div>");

			sb.Append("</div>");

			// Print body
			sb.Append(@"<div class=""messagebody"">");
			sb.Append(FormattingPipeline.FormatWithPhase3(FormattingPipeline.FormatWithPhase1And2(message.Body, false, FormattingContext.MessageBody, currentPage),
				FormattingContext.MessageBody, currentPage));
			sb.Append("</div>");
		}

	}

}
