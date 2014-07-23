
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
using System.Globalization;

namespace ScrewTurn.Wiki {

	public partial class AllPages : BasePage {

		/// <summary>
		/// The number of items in a page.
		/// </summary>
		public int PageSize = 50;

		private int selectedPage = 0;
		private int rangeBegin = 0;
		private int rangeEnd = 49;

		private IList<PageInfo> currentPages = null;

		protected void Page_Load(object sender, EventArgs e) {
			PageSize = Settings.ListSize;
			rangeEnd = PageSize - 1;

			LoginTools.VerifyReadPermissionsForCurrentNamespace();

			if(Request["Cat"] != null) {
				if(Request["Cat"].Equals("-")) {
					lblPages.Text = Properties.Messages.UncategorizedPages;
				}
				else {
					lblPages.Text = Properties.Messages.PagesOfCategory + " <i>" + Request["Cat"] + "</i>";
				}
			}

			if(!Page.IsPostBack) {
				lnkCategories.NavigateUrl = UrlTools.BuildUrl("Category.aspx");
				lnkSearch.NavigateUrl = UrlTools.BuildUrl("Search.aspx");

				currentPages = GetAllPages();
				pageSelector.ItemCount = currentPages.Count;
				pageSelector.PageSize = PageSize;

				string p = Request["Page"];
				if(!int.TryParse(p, out selectedPage)) selectedPage = 0;
				pageSelector.SelectPage(selectedPage);
			}

			Page.Title = Properties.Messages.AllPagesTitle + " (" + (rangeBegin + 1).ToString() + "-" + (rangeEnd + 1).ToString() + ") - " + Settings.WikiTitle;

			// Important note
			// This page cannot use a repeater because the page list has particular elements used for grouping pages

			PrintPages();
		}

		protected void pageSelector_SelectedPageChanged(object sender, SelectedPageChangedEventArgs e) {
			rangeBegin = e.SelectedPage * PageSize;
			rangeEnd = rangeBegin + e.ItemCount - 1;
			selectedPage = e.SelectedPage;

			PrintPages();
		}

		/// <summary>
		/// Gets the creator of a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The creator.</returns>
		private string GetCreator(PageInfo page) {
			List<int> baks = Pages.GetBackups(page);

			PageContent content = null;
			if(baks.Count > 0) {
				content = Pages.GetBackupContent(page, baks[0]);
			}
			else {
				content = Content.GetPageContent(page, false);
			}

			return content.User;
		}

		/// <summary>
		/// Gets all the pages in the namespace.
		/// </summary>
		/// <returns>The pages.</returns>
		private IList<PageInfo> GetAllPages() {
			IList<PageInfo> pages = null;

			// Categories Management
			if(Request["Cat"] != null) {
				if(Request["Cat"].Equals("-")) {
					pages = Pages.GetUncategorizedPages(DetectNamespaceInfo());
				}
				else {
					CategoryInfo cat = Pages.FindCategory(Request["Cat"]);
					if(cat != null) {
						pages = new PageInfo[cat.Pages.Length];
						for(int i = 0; i < cat.Pages.Length; i++) {
							pages[i] = Pages.FindPage(cat.Pages[i]);
						}
						Array.Sort(pages as PageInfo[], new PageNameComparer());
					}
					else {
						pages = new PageInfo[0];
					}
				}
			}
			else {
				pages = Pages.GetPages(DetectNamespaceInfo());
			}

			return pages;
		}

		/// <summary>
		/// Prints the pages.
		/// </summary>
		public void PrintPages() {
			StringBuilder sb = new StringBuilder(65536);

			if(currentPages == null) currentPages = GetAllPages();

			// Prepare ExtendedPageInfo array
			ExtendedPageInfo[] tempPageList = new ExtendedPageInfo[rangeEnd - rangeBegin + 1];
			PageContent cnt;
			for(int i = 0; i < tempPageList.Length; i++) {
				cnt = Content.GetPageContent(currentPages[rangeBegin + i], true);
				tempPageList[i] = new ExtendedPageInfo(currentPages[rangeBegin + i], cnt.Title, cnt.LastModified, GetCreator(currentPages[rangeBegin + i]), cnt.User);
			}

			// Prepare for sorting
			bool reverse = false;
			SortingMethod sortBy = SortingMethod.Title;
			if(Request["SortBy"] != null) {
				try {
					sortBy = (SortingMethod)Enum.Parse(typeof(SortingMethod), Request["SortBy"], true);
				}
				catch {
					// Backwards compatibility
					if(Request["SortBy"].ToLowerInvariant() == "date") sortBy = SortingMethod.DateTime;
				}
				if(Request["Reverse"] != null) reverse = true;
			}

			SortedDictionary<SortingGroup, List<ExtendedPageInfo>> sortedPages = PageSortingTools.Sort(tempPageList, sortBy, reverse);

			sb.Append(@"<table id=""PageListTable"" class=""generic"" cellpadding=""0"" cellspacing=""0"">");
			sb.Append("<thead>");
			sb.Append(@"<tr class=""tableheader"">");
			
			// Page title
			sb.Append(@"<th><a rel=""nofollow"" href=""");
			UrlTools.BuildUrl(sb, "AllPages.aspx?SortBy=Title",
				(!reverse && sortBy == SortingMethod.Title ? "&amp;Reverse=1" : ""),
				(Request["Cat"] != null ? "&amp;Cat=" + Tools.UrlEncode(Request["Cat"]) : ""),
				"&amp;Page=", selectedPage.ToString());

			sb.Append(@""" title=""");
			sb.Append(Properties.Messages.SortByTitle);
			sb.Append(@""">");
			sb.Append(Properties.Messages.PageTitle);
			sb.Append((reverse && sortBy.Equals("title") ? " &uarr;" : ""));
			sb.Append((!reverse && sortBy.Equals("title") ? " &darr;" : ""));
			sb.Append("</a></th>");

			// Message count
			sb.Append(@"<th><img src=""Images/Comment.png"" alt=""Comments"" /></th>");

			// Creation date/time
			sb.Append(@"<th><a rel=""nofollow"" href=""");
			UrlTools.BuildUrl(sb, "AllPages.aspx?SortBy=Creation",
				(!reverse && sortBy == SortingMethod.Creation ? "&amp;Reverse=1" : ""),
				(Request["Cat"] != null ? "&amp;Cat=" + Tools.UrlEncode(Request["Cat"]) : ""),
				"&amp;Page=", selectedPage.ToString());

			sb.Append(@""" title=""");
			sb.Append(Properties.Messages.SortByDate);
			sb.Append(@""">");
			sb.Append(Properties.Messages.CreatedOn);
			sb.Append((reverse && sortBy.Equals("creation") ? " &uarr;" : ""));
			sb.Append((!reverse && sortBy.Equals("creation") ? " &darr;" : ""));
			sb.Append("</a></th>");

			// Mod. date/time
			sb.Append(@"<th><a rel=""nofollow"" href=""");
			UrlTools.BuildUrl(sb, "AllPages.aspx?SortBy=DateTime",
				(!reverse && sortBy == SortingMethod.DateTime ? "&amp;Reverse=1" : ""),
				(Request["Cat"] != null ? "&amp;Cat=" + Tools.UrlEncode(Request["Cat"]) : ""),
				"&amp;Page=", selectedPage.ToString());

			sb.Append(@""" title=""");
			sb.Append(Properties.Messages.SortByDate);
			sb.Append(@""">");
			sb.Append(Properties.Messages.ModifiedOn);
			sb.Append((reverse && sortBy.Equals("date") ? " &uarr;" : ""));
			sb.Append((!reverse && sortBy.Equals("date") ? " &darr;" : ""));
			sb.Append("</a></th>");

			// Creator
			sb.Append(@"<th><a rel=""nofollow"" href=""");
			UrlTools.BuildUrl(sb, "AllPages.aspx?SortBy=Creator",
				(!reverse && sortBy == SortingMethod.Creator ? "&amp;Reverse=1" : ""),
				(Request["Cat"] != null ? "&amp;Cat=" + Tools.UrlEncode(Request["Cat"]) : ""),
				"&amp;Page=", selectedPage.ToString());

			sb.Append(@""" title=""");
			sb.Append(Properties.Messages.SortByUser);
			sb.Append(@""">");
			sb.Append(Properties.Messages.CreatedBy);
			sb.Append((reverse && sortBy.Equals("creator") ? " &uarr;" : ""));
			sb.Append((!reverse && sortBy.Equals("creator") ? " &darr;" : ""));
			sb.Append("</a></th>");

			// Last author
			sb.Append(@"<th><a rel=""nofollow"" href=""");
			UrlTools.BuildUrl(sb, "AllPages.aspx?SortBy=User",
				(!reverse && sortBy == SortingMethod.User ? "&amp;Reverse=1" : ""),
				(Request["Cat"] != null ? "&amp;Cat=" + Tools.UrlEncode(Request["Cat"]) : ""),
				"&amp;Page=", selectedPage.ToString());

			sb.Append(@""" title=""");
			sb.Append(Properties.Messages.SortByUser);
			sb.Append(@""">");
			sb.Append(Properties.Messages.ModifiedBy);
			sb.Append((reverse && sortBy.Equals("user") ? " &uarr;" : ""));
			sb.Append((!reverse && sortBy.Equals("user") ? " &darr;" : ""));
			sb.Append("</a></th>");

			// Categories
			sb.Append("<th>");
			sb.Append(Properties.Messages.Categories);
			sb.Append("</th>");

			sb.Append("</tr>");
			sb.Append("</thead><tbody>");

			foreach(SortingGroup key in sortedPages.Keys) {
				List<ExtendedPageInfo> pageList = sortedPages[key];
				for(int i = 0; i < pageList.Count; i++) {
					if(i == 0) {
						// Add group header
						sb.Append(@"<tr class=""tablerow"">");
						if(sortBy == SortingMethod.Title) {
							sb.AppendFormat("<td colspan=\"7\"><b>{0}</b></td>", key.Label);
						}
						else if(sortBy == SortingMethod.Creation) {
							sb.AppendFormat("<td colspan=\"2\"></td><td colspan=\"5\"><b>{0}</b></td>", key.Label);
						}
						else if(sortBy == SortingMethod.DateTime) {
							sb.AppendFormat("<td colspan=\"3\"></td><td colspan=\"4\"><b>{0}</b></td>", key.Label);
						}
						else if(sortBy == SortingMethod.Creator) {
							sb.AppendFormat("<td colspan=\"4\"></td><td colspan=\"3\"><b>{0}</b></td>", key.Label);
						}
						else if(sortBy == SortingMethod.User) {
							sb.AppendFormat("<td colspan=\"5\"></td><td colspan=\"2\"><b>{0}</b></td>", key.Label);
						}
						sb.Append("</tr>");
					}

					sb.Append(@"<tr class=""tablerow");
					if((i + 1) % 2 == 0) sb.Append("alternate");
					sb.Append(@""">");

					// Page title
					sb.Append(@"<td>");
					sb.Append(@"<a href=""");
					UrlTools.BuildUrl(sb, Tools.UrlEncode(pageList[i].PageInfo.FullName), Settings.PageExtension);
					sb.Append(@""">");
					sb.Append(pageList[i].Title);
					sb.Append("</a>");
					sb.Append("</td>");

					// Message count
					sb.Append(@"<td>");
					int msg = pageList[i].MessageCount;
					if(msg > 0) {
						sb.Append(@"<a href=""");
						UrlTools.BuildUrl(sb, Tools.UrlEncode(pageList[i].PageInfo.FullName), Settings.PageExtension, "?Discuss=1");
						sb.Append(@""" title=""");
						sb.Append(Properties.Messages.Discuss);
						sb.Append(@""">");
						sb.Append(msg.ToString());
						sb.Append("</a>");
					}
					else sb.Append("&nbsp;");
					sb.Append("</td>");

					// Creation date/time
					sb.Append(@"<td>");
					sb.Append(Preferences.AlignWithTimezone(pageList[i].CreationDateTime).ToString(Settings.DateTimeFormat) + "&nbsp;");
					sb.Append("</td>");

					// Mod. date/time
					sb.Append(@"<td>");
					sb.Append(Preferences.AlignWithTimezone(pageList[i].ModificationDateTime).ToString(Settings.DateTimeFormat) + "&nbsp;");
					sb.Append("</td>");

					// Creator
					sb.Append(@"<td>");
					sb.Append(Users.UserLink(pageList[i].Creator));
					sb.Append("</td>");

					// Last author
					sb.Append(@"<td>");
					sb.Append(Users.UserLink(pageList[i].LastAuthor));
					sb.Append("</td>");

					// Categories
					CategoryInfo[] cats = Pages.GetCategoriesForPage(pageList[i].PageInfo);
					sb.Append(@"<td>");
					if(cats.Length == 0) {
						sb.Append(@"<a href=""");
						UrlTools.BuildUrl(sb, "AllPages.aspx?Cat=-");
						sb.Append(@""">");
						sb.Append(Properties.Messages.NC);
						sb.Append("</a>");
					}
					else {
						for(int k = 0; k < cats.Length; k++) {
							sb.Append(@"<a href=""");
							UrlTools.BuildUrl(sb, "AllPages.aspx?Cat=", Tools.UrlEncode(cats[k].FullName));
							sb.Append(@""">");
							sb.Append(NameTools.GetLocalName(cats[k].FullName));
							sb.Append("</a>");
							if(k != cats.Length - 1) sb.Append(", ");
						}
					}
					sb.Append("</td>");

					sb.Append("</tr>");
				}
			}
			sb.Append("</tbody>");
			sb.Append("</table>");

			Literal lbl = new Literal();
			lbl.Text = sb.ToString();
			pnlPageList.Controls.Clear();
			pnlPageList.Controls.Add(lbl);
		}

	}

}
