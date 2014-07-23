
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class History : BasePage {

        private PageInfo page;
        private PageContent content;
		private bool canRollback;

        protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.HistoryTitle + " - " + Settings.WikiTitle;

			page = Pages.FindPage(Request["Page"]);

            if(page != null) {
				canRollback = AuthChecker.CheckActionForPage(page, Actions.ForPages.ManagePage,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());

                content = Content.GetPageContent(page, true);
				lblTitle.Text = Properties.Messages.PageHistory + ": " + FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.PageContent, page);

				bool canView = AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadPage,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
				if(!canView) UrlTools.Redirect("AccessDenied.aspx");
            }
            else {
                lblTitle.Text = Properties.Messages.PageNotFound;
				return;
            }

			if(!Page.IsPostBack && page != null) {
				List<int> revisions = Pages.GetBackups(page);
				revisions.Reverse();
				// Populate dropdown lists
				lstRev1.Items.Clear();
				lstRev2.Items.Clear();
				lstRev2.Items.Add(new ListItem(Properties.Messages.Current, "Current"));
				if(Request["Rev2"] != null && Request["Rev2"].Equals(lstRev2.Items[0].Value))
					lstRev2.SelectedIndex = 0;
				for(int i = 0; i < revisions.Count; i++) {
					lstRev1.Items.Add(new ListItem(revisions[i].ToString(), revisions[i].ToString()));
					lstRev2.Items.Add(new ListItem(revisions[i].ToString(), revisions[i].ToString()));
					if(Request["Rev1"] != null && Request["Rev1"].Equals(lstRev1.Items[i].Value))
						lstRev1.SelectedIndex = i;
					if(Request["Rev2"] != null && Request["Rev2"].Equals(lstRev2.Items[i + 1].Value))
						lstRev2.SelectedIndex = i + 1;
				}
				if(revisions.Count == 0) btnCompare.Enabled = false;
			}

			PrintHistory();
        }

		/// <summary>
		/// Prints the history.
		/// </summary>
        public void PrintHistory() {
            if(page == null) return;

			StringBuilder sb = new StringBuilder();

			if(Request["Revision"] == null) {
				// Show version list
				List<int> revisions = Pages.GetBackups(page);
				revisions.Reverse();

				List<RevisionRow> result = new List<RevisionRow>(revisions.Count + 1);

				result.Add(new RevisionRow(-1, Content.GetPageContent(page, false), false));

				foreach(int rev in revisions) {
					PageContent content = Pages.GetBackupContent(page, rev);

					result.Add(new RevisionRow(rev, content, canRollback));
				}

				rptHistory.DataSource = result;
				rptHistory.DataBind();
			}
			else {
				int rev = -1;
				if(!int.TryParse(Request["Revision"], out rev)) UrlTools.Redirect(page.FullName + Settings.PageExtension);
				
				List<int> backups = Pages.GetBackups(page);
				if(!backups.Contains(rev)) {
					UrlTools.Redirect(page.FullName + Settings.PageExtension);
					return;
				}
				PageContent revision = Pages.GetBackupContent(page, rev);
				sb.Append(@"<table class=""box"" cellpadding=""0"" cellspacing=""0""><tr><td>");
				sb.Append(@"<p style=""text-align: center;""><b>");
				if(rev > 0) {
					sb.Append(@"<a href=""");
					UrlTools.BuildUrl(sb, "History.aspx?Page=", Tools.UrlEncode(page.FullName),
						"&amp;Revision=", Tools.GetVersionString((int)(rev - 1)));

					sb.Append(@""">&laquo; ");
					sb.Append(Properties.Messages.OlderRevision);
					sb.Append("</a>");
				}
				else {
					sb.Append("&laquo; ");
					sb.Append(Properties.Messages.OlderRevision);
				}

				sb.Append(@" - <a href=""");
				UrlTools.BuildUrl(sb, "History.aspx?Page=", Tools.UrlEncode(page.FullName));
				sb.Append(@""">");
				sb.Append(Properties.Messages.BackToHistory);
				sb.Append("</a> - ");

				if(rev < backups.Count - 1) {
					sb.Append(@"<a href=""");
					UrlTools.BuildUrl(sb, "History.aspx?Page=", Tools.UrlEncode(page.FullName),
						"&amp;Revision=", Tools.GetVersionString((int)(rev + 1)));

					sb.Append(@""">");
					sb.Append(Properties.Messages.NewerRevision);
					sb.Append(" &raquo;</a>");
				}
				else {
					sb.Append(@"<a href=""");
					UrlTools.BuildUrl(sb, Tools.UrlEncode(page.FullName), Settings.PageExtension);
					sb.Append(@""">");
					sb.Append(Properties.Messages.CurrentRevision);
					sb.Append("</a>");
				}
				sb.Append("</b></p></td></tr></table><br />");

				sb.Append(@"<h3 class=""separator"">");
				sb.Append(Properties.Messages.PageRevision);
				sb.Append(": ");
				sb.Append(Preferences.AlignWithTimezone(revision.LastModified).ToString(Settings.DateTimeFormat));
				sb.Append("</h3><br />");

				sb.Append(FormattingPipeline.FormatWithPhase3(FormattingPipeline.FormatWithPhase1And2(revision.Content,
					false, FormattingContext.PageContent, page).Replace(Formatter.EditSectionPlaceHolder, ""), FormattingContext.PageContent, page));
			}

			lblHistory.Text = sb.ToString();
        }

		protected void rptHistory_ItemCommand(object sender, CommandEventArgs e) {
			if(e.CommandName == "Rollback") {
				if(!canRollback) return;
				int rev = int.Parse(e.CommandArgument as string);

				Log.LogEntry("Page rollback requested for " + page.FullName + " to rev. " + rev.ToString(), EntryType.General, SessionFacade.GetCurrentUsername());
				Pages.Rollback(page, rev);

				PrintHistory();
			}
		}

		protected void btnCompare_Click(object sender, EventArgs e) {
			UrlTools.Redirect(UrlTools.BuildUrl("Diff.aspx?Page=", Tools.UrlEncode(page.FullName), "&Rev1=", lstRev1.SelectedValue,
				"&Rev2=", lstRev2.SelectedValue));
		}

	}

	/// <summary>
	/// Represents a page revision for display purposes.
	/// </summary>
	public class RevisionRow {

		private string page, revision, title, savedOn, savedBy, comment;
		private bool canRollback;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:RevisionRow" /> class.
		/// </summary>
		/// <param name="revision">The revision (<b>-1</b> for current).</param>
		/// <param name="content">The original page content.</param>
		/// <param name="canRollback">A value indicating whether the current user can rollback the page.</param>
		public RevisionRow(int revision, PageContent content, bool canRollback) {
			this.page = content.PageInfo.FullName;
			if(revision == -1) this.revision = Properties.Messages.Current;
			else this.revision = revision.ToString();
			title = FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.PageContent, content.PageInfo);
			savedOn = Preferences.AlignWithTimezone(content.LastModified).ToString(Settings.DateTimeFormat);
			savedBy = Users.UserLink(content.User);
			comment = content.Comment;
			this.canRollback = canRollback;
		}

		/// <summary>
		/// Gets the page name.
		/// </summary>
		public string Page {
			get { return page; }
		}

		/// <summary>
		/// Gets the revision.
		/// </summary>
		public string Revision {
			get { return revision; }
		}

		/// <summary>
		/// Gets the title.
		/// </summary>
		public string Title {
			get { return title; }
		}

		/// <summary>
		/// Gets the save date/time.
		/// </summary>
		public string SavedOn {
			get { return savedOn; }
		}

		/// <summary>
		/// Gets the revision author.
		/// </summary>
		public string SavedBy {
			get { return savedBy; }
		}

		/// <summary>
		/// Gets the comment.
		/// </summary>
		public string Comment {
			get { return comment; }
		}

		/// <summary>
		/// Gets a value indicating whether the current user can rollback the page.
		/// </summary>
		public bool CanRollback {
			get { return canRollback; }
		}

	}

}
