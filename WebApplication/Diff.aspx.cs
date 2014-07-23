
using System;
using System.Data;
using System.Configuration;
using System.Collections;
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

	public partial class Diff : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.DiffTitle + " - " + Settings.WikiTitle;

			PrintDiff();
		}

		public void PrintDiff() {
			if(Request["Page"] == null || Request["Rev1"] == null || Request["Rev2"] == null) {
				Redirect();
				return;
			}

			StringBuilder sb = new StringBuilder();

			PageInfo page = Pages.FindPage(Request["Page"]);
			if(page == null) {
				Redirect();
				return;
			}

			bool canView = AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadPage,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			if(!canView) UrlTools.Redirect("AccessDenied.aspx");

			int rev1 = -1;
			int rev2 = -1;
			string rev1Text = "";
			string rev2Text = "";

			PageContent rev1Content = null;
			PageContent rev2Content = null;
			bool draft = false;
			
			// Load rev1 content
			if(int.TryParse(Request["Rev1"], out rev1)) {
				rev1Content = Pages.GetBackupContent(page, rev1);
				rev1Text = rev1.ToString();
				if(rev1 >= 0 && rev1Content == null && Pages.GetBackupContent(page, rev1 - 1) != null) rev1Content = Content.GetPageContent(page, false);
				if(rev1Content == null) Redirect();
			}
			else {
				// Look for current
				if(Request["Rev1"].ToLowerInvariant() == "current") {
					rev1Content = Content.GetPageContent(page, false);
					rev1Text = Properties.Messages.Current;
				}
				else Redirect();
			}

			if(int.TryParse(Request["Rev2"], out rev2)) {
				rev2Content = Pages.GetBackupContent(page, rev2);
				rev2Text = rev2.ToString();
				if(rev2 >= 0 && rev2Content == null && Pages.GetBackupContent(page, rev2 - 1) != null) rev2Content = Content.GetPageContent(page, false);
				if(rev2Content == null) Redirect();
			}
			else {
				// Look for current or draft
				if(Request["Rev2"].ToLowerInvariant() == "current") {
					rev2Content = Content.GetPageContent(page, false);
					rev2Text = Properties.Messages.Current;
				}
				else if(Request["Rev2"].ToLowerInvariant() == "draft") {
					rev2Content = Pages.GetDraft(page);
					rev2Text = Properties.Messages.Draft;
					draft = true;
					if(rev2Content == null) Redirect();
				}
				else Redirect();
			}

			PageContent content = Content.GetPageContent(page, true);

			lblTitle.Text = Properties.Messages.DiffingPageTitle.Replace("##PAGETITLE##",
				FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.PageContent, page)).Replace("##REV1##", rev1Text).Replace("##REV2##", rev2Text);

			lblBack.Text = string.Format(@"<a href=""{0}"">&laquo; {1}</a>",
				UrlTools.BuildUrl("History.aspx?Page=", Tools.UrlEncode(Request["Page"]), "&amp;Rev1=", Request["Rev1"], "&amp;Rev2=", Request["Rev2"]),
				Properties.Messages.Back);
			lblBack.Visible = !draft;

			sb.Append(Properties.Messages.DiffColorKey);
			sb.Append("<br /><br />");

			string result = DiffTools.DiffRevisions(rev1Content.Content, rev2Content.Content);

			sb.Append(result);

			lblDiff.Text = sb.ToString();
		}

		private void Redirect() {
			UrlTools.RedirectHome();
		}

	}

}
