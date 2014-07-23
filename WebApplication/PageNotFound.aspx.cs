
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
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class PageNotFound : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.PageNotFoundTitle + " - " + Settings.WikiTitle;

			if(Request["Page"] != null) {
				lblDescription.Text = lblDescription.Text.Replace("##PAGENAME##", Request["Page"]);
			}
			else {
				UrlTools.Redirect(UrlTools.BuildUrl("Default.aspx"));
			}

			PrintSearchResults();
		}

		/// <summary>
		/// Prints the results of the automatic search.
		/// </summary>
		public void PrintSearchResults() {
			StringBuilder sb = new StringBuilder(1000);

			PageInfo[] results = SearchTools.SearchSimilarPages(Request["Page"], DetectNamespace());
			if(results.Length > 0) {
				sb.Append("<p>");
				sb.Append(Properties.Messages.WereYouLookingFor);
				sb.Append("</p>");
				sb.Append("<ul>");
				PageContent c;
				for(int i = 0; i < results.Length; i++) {
					c = Content.GetPageContent(results[i], true);
					sb.Append(@"<li><a href=""");
					UrlTools.BuildUrl(sb, Tools.UrlEncode(results[i].FullName), Settings.PageExtension);
					sb.Append(@""">");
					sb.Append(FormattingPipeline.PrepareTitle(c.Title, false, FormattingContext.PageContent, c.PageInfo));
					sb.Append("</a></li>");
				}
				sb.Append("</ul>");
			}
			else {
				sb.Append("<p>");
				sb.Append(Properties.Messages.NoSimilarPages);
				sb.Append("</p>");
			}
			sb.Append(@"<br /><p>");
			sb.Append(Properties.Messages.YouCanAlso);
			sb.Append(@" <a href=""");
			UrlTools.BuildUrl(sb, "Search.aspx?Query=", Tools.UrlEncode(Request["Page"]));
			sb.Append(@""">");
			sb.Append(Properties.Messages.PerformASearch);
			sb.Append("</a> ");
			sb.Append(Properties.Messages.Or);
			sb.Append(@" <a href=""");
			UrlTools.BuildUrl(sb, "Edit.aspx?Page=", Tools.UrlEncode(Request["Page"]));
			sb.Append(@"""><b>");
			sb.Append(Properties.Messages.CreateThePage);
			sb.Append("</b></a> (");
			sb.Append(Properties.Messages.CouldRequireLogin);
			sb.Append(").</p>");

			lblSearchResults.Text = sb.ToString();
		}

	}

}
