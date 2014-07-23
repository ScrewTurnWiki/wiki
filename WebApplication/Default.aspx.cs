
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class DefaultPage : BasePage {

		private PageInfo currentPage = null;
		private PageContent currentContent = null;

		private bool discussMode = false;
		private bool viewCodeMode = false;

		protected void Page_Load(object sender, EventArgs e) {

			discussMode = Request["Discuss"] != null;
			viewCodeMode = Request["Code"] != null && !discussMode;
			if(!Settings.EnableViewPageCodeFeature) viewCodeMode = false;

			currentPage = DetectPageInfo(true);

			VerifyAndPerformRedirects();

			// The following actions are verified:
			// - View content (redirect to AccessDenied)
			// - Edit or Edit with Approval (for button display)
			// - Any Administrative activity (Rollback/Admin/Perms) (for button display)
			// - Download attachments (for button display - download permissions are also checked in GetFile)
			// - View discussion (for button display in content mode)
			// - Post discussion (for button display in discuss mode)

			string currentUsername = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			bool canView = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.ReadPage, currentUsername, currentGroups);
			bool canEdit = false;
			bool canEditWithApproval = false;
			Pages.CanEditPage(currentPage, currentUsername, currentGroups, out canEdit, out canEditWithApproval);
			if(canEditWithApproval && canEdit) canEditWithApproval = false;
			bool canDownloadAttachments = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.DownloadAttachments, currentUsername, currentGroups);
			bool canSetPerms = AuthChecker.CheckActionForGlobals(Actions.ForGlobals.ManagePermissions, currentUsername, currentGroups);
			bool canAdmin = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.ManagePage, currentUsername, currentGroups);
			bool canViewDiscussion = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.ReadDiscussion, currentUsername, currentGroups);
			bool canPostDiscussion = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.PostDiscussion, currentUsername, currentGroups);
			bool canManageDiscussion = AuthChecker.CheckActionForPage(currentPage, Actions.ForPages.ManageDiscussion, currentUsername, currentGroups);

			if(!canView) {
				if(SessionFacade.LoginKey == null) UrlTools.Redirect("Login.aspx?Redirect=" + Tools.UrlEncode(Tools.GetCurrentUrlFixed()));
				else UrlTools.Redirect(UrlTools.BuildUrl("AccessDenied.aspx"));
			}
			attachmentViewer.Visible = canDownloadAttachments;

			attachmentViewer.PageInfo = currentPage;
			currentContent = Content.GetPageContent(currentPage, true);

			pnlPageInfo.Visible = Settings.EnablePageInfoDiv;

			SetupTitles();

			SetupToolbarLinks(canEdit || canEditWithApproval, canViewDiscussion, canPostDiscussion, canDownloadAttachments, canAdmin, canAdmin, canSetPerms);

			SetupLabels();
			SetupPrintAndRssLinks();
			SetupMetaInformation();
			VerifyAndPerformPageRedirection();
			SetupRedirectionSource();
			SetupNavigationPaths();
			SetupAdjacentPages();

			SessionFacade.Breadcrumbs.AddPage(currentPage);
			SetupBreadcrumbsTrail();

			SetupDoubleClickHandler();

			SetupEmailNotification();

			SetupPageContent(canPostDiscussion, canManageDiscussion);

			if(currentPage != null) {
				Literal canonical = new Literal();
				canonical.Text = Tools.GetCanonicalUrlTag(Request.Url.ToString(), currentPage, Pages.FindNamespace(NameTools.GetNamespace(currentPage.FullName)));
				Page.Header.Controls.Add(canonical);
			}
		}

		/// <summary>
		/// Verifies the need for a redirect and performs it.
		/// </summary>
		private void VerifyAndPerformRedirects() {
			if(currentPage == null) {
				UrlTools.Redirect(UrlTools.BuildUrl("PageNotFound.aspx?Page=", Tools.UrlEncode(DetectFullName())));
			}
			if(Request["Edit"] == "1") {
				UrlTools.Redirect(UrlTools.BuildUrl("Edit.aspx?Page=", Tools.UrlEncode(currentPage.FullName)));
			}
			if(Request["History"] == "1") {
				UrlTools.Redirect(UrlTools.BuildUrl("History.aspx?Page=", Tools.UrlEncode(currentPage.FullName)));
			}
		}

		/// <summary>
		/// Sets the titles used in the page.
		/// </summary>
		private void SetupTitles() {
			string title = FormattingPipeline.PrepareTitle(currentContent.Title, false, FormattingContext.PageContent, currentPage);
			Page.Title = title + " - " + Settings.WikiTitle;
			lblPageTitle.Text = title;
		}

		/// <summary>
		/// Sets the content and visibility of all toolbar links.
		/// </summary>
		/// <param name="canEdit">A value indicating whether the current user can edit the page.</param>
		/// <param name="canViewDiscussion">A value indicating whether the current user can view the page discussion.</param>
		/// <param name="canPostMessages">A value indicating whether the current user can post messages in the page discussion.</param>
		/// <param name="canDownloadAttachments">A value indicating whether the current user can download attachments.</param>
		/// <param name="canRollback">A value indicating whether the current user can rollback the page.</param>
		/// <param name="canAdmin">A value indicating whether the current user can perform at least one administration task.</param>
		/// <param name="canSetPerms">A value indicating whether the current user can set page permissions.</param>
		private void SetupToolbarLinks(bool canEdit, bool canViewDiscussion, bool canPostMessages,
			bool canDownloadAttachments, bool canRollback, bool canAdmin, bool canSetPerms) {
			
			lblDiscussLink.Visible = !discussMode && !viewCodeMode && canViewDiscussion;
			if(lblDiscussLink.Visible) {
				lblDiscussLink.Text = string.Format(@"<a id=""DiscussLink"" title=""{0}"" href=""{3}?Discuss=1"">{1} ({2})</a>",
					Properties.Messages.Discuss, Properties.Messages.Discuss, Pages.GetMessageCount(currentPage),
					UrlTools.BuildUrl(NameTools.GetLocalName(currentPage.FullName), Settings.PageExtension));
			}

			lblEditLink.Visible = Settings.EnablePageToolbar && !discussMode && !viewCodeMode && canEdit;
			if(lblEditLink.Visible) {
				lblEditLink.Text = string.Format(@"<a id=""EditLink"" title=""{0}"" href=""{1}"">{2}</a>",
					Properties.Messages.EditThisPage,
					UrlTools.BuildUrl("Edit.aspx?Page=", Tools.UrlEncode(currentPage.FullName)),
					Properties.Messages.Edit);
			}

			if(Settings.EnablePageToolbar && Settings.EnableViewPageCodeFeature) {
				lblViewCodeLink.Visible = !discussMode && !viewCodeMode && !canEdit;
				if(lblViewCodeLink.Visible) {
					lblViewCodeLink.Text = string.Format(@"<a id=""ViewCodeLink"" title=""{0}"" href=""{2}?Code=1"">{1}</a>",
						Properties.Messages.ViewPageCode, Properties.Messages.ViewPageCode,
						UrlTools.BuildUrl(NameTools.GetLocalName(currentPage.FullName), Settings.PageExtension));
				}
			}
			else lblViewCodeLink.Visible = false;

			lblHistoryLink.Visible = Settings.EnablePageToolbar && !discussMode && !viewCodeMode;
			if(lblHistoryLink.Visible) {
				lblHistoryLink.Text = string.Format(@"<a id=""HistoryLink"" title=""{0}"" href=""{1}"">{2}</a>",
					Properties.Messages.ViewPageHistory,
					UrlTools.BuildUrl("History.aspx?Page=", Tools.UrlEncode(currentPage.FullName)),
					Properties.Messages.History);
			}

			int attachmentCount = GetAttachmentCount();
			lblAttachmentsLink.Visible = canDownloadAttachments && !discussMode && !viewCodeMode && attachmentCount > 0;
			if(lblAttachmentsLink.Visible) {
				lblAttachmentsLink.Text = string.Format(@"<a id=""PageAttachmentsLink"" title=""{0}"" href=""#"" onclick=""javascript:return __ToggleAttachmentsMenu(event.clientX, event.clientY);"">{1}</a>",
					Properties.Messages.Attachments, Properties.Messages.Attachments);
			}
			attachmentViewer.Visible = lblAttachmentsLink.Visible;

			int bakCount = GetBackupCount();
			lblAdminToolsLink.Visible = Settings.EnablePageToolbar && !discussMode && !viewCodeMode &&
				((canRollback && bakCount > 0)|| canAdmin || canSetPerms);
			if(lblAdminToolsLink.Visible) {
				lblAdminToolsLink.Text = string.Format(@"<a id=""AdminToolsLink"" title=""{0}"" href=""#"" onclick=""javascript:return __ToggleAdminToolsMenu(event.clientX, event.clientY);"">{1}</a>",
					Properties.Messages.AdminTools, Properties.Messages.Admin);

				if(canRollback && bakCount > 0) {
					lblRollbackPage.Text = string.Format(@"<a href=""AdminPages.aspx?Rollback={0}"" onclick=""javascript:return __RequestConfirm();"" title=""{1}"">{2}</a>",
						Tools.UrlEncode(currentPage.FullName),
						Properties.Messages.RollbackThisPage, Properties.Messages.Rollback);
				}
				else lblRollbackPage.Visible = false;

				if(canAdmin) {
					lblAdministratePage.Text = string.Format(@"<a href=""AdminPages.aspx?Admin={0}"" title=""{1}"">{2}</a>",
						Tools.UrlEncode(currentPage.FullName),
						Properties.Messages.AdministrateThisPage, Properties.Messages.Administrate);
				}
				else lblAdministratePage.Visible = false;

				if(canSetPerms) {
					lblSetPagePermissions.Text = string.Format(@"<a href=""AdminPages.aspx?Perms={0}"" title=""{1}"">{2}</a>",
						Tools.UrlEncode(currentPage.FullName),
						Properties.Messages.SetPermissionsForThisPage, Properties.Messages.Permissions);
				}
				else lblSetPagePermissions.Visible = false;
			}

			lblPostMessageLink.Visible = discussMode && !viewCodeMode && canPostMessages;
			if(lblPostMessageLink.Visible) {
				lblPostMessageLink.Text = string.Format(@"<a id=""PostReplyLink"" title=""{0}"" href=""{1}"">{2}</a>",
					Properties.Messages.PostMessage,
					UrlTools.BuildUrl("Post.aspx?Page=", Tools.UrlEncode(currentPage.FullName)),
					Properties.Messages.PostMessage);
			}

			lblBackLink.Visible = discussMode || viewCodeMode;
			if(lblBackLink.Visible) {
				lblBackLink.Text = string.Format(@"<a id=""BackLink"" title=""{0}"" href=""{1}"">{2}</a>",
					Properties.Messages.Back,
					UrlTools.BuildUrl(Tools.UrlEncode(currentPage.FullName), Settings.PageExtension, "?NoRedirect=1"),
					Properties.Messages.Back);
			}
		}

		/// <summary>
		/// Gets the number of backups for the current page.
		/// </summary>
		/// <returns>The number of backups.</returns>
		private int GetBackupCount() {
			return Pages.GetBackups(currentPage).Count;
		}

		/// <summary>
		/// Gets the number of attachments for the current page.
		/// </summary>
		/// <returns>The number of attachments.</returns>
		private int GetAttachmentCount() {
			int count = 0;
			foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
				count += prov.ListPageAttachments(currentPage).Length;
			}
			return count;
		}

		/// <summary>
		/// Sets the content and visibility of all labels used in the page.
		/// </summary>
		private void SetupLabels() {
			if(discussMode) {
				lblModified.Visible = false;
				lblModifiedDateTime.Visible = false;
				lblBy.Visible = false;
				lblAuthor.Visible = false;
				lblCategorizedAs.Visible = false;
				lblPageCategories.Visible = false;
				lblNavigationPaths.Visible = false;
				lblDiscussedPage.Text = "<b>" + FormattingPipeline.PrepareTitle(currentContent.Title, false, FormattingContext.PageContent, currentPage) + "</b>";
			}
			else {
				lblPageDiscussionFor.Visible = false;
				lblDiscussedPage.Visible = false;

				lblModifiedDateTime.Text =
					Preferences.AlignWithTimezone(currentContent.LastModified).ToString(Settings.DateTimeFormat);
				lblAuthor.Text = Users.UserLink(currentContent.User);
				lblPageCategories.Text = GetFormattedPageCategories();
			}
		}

		/// <summary>
		/// Sets the Print and RSS links.
		/// </summary>
		private void SetupPrintAndRssLinks() {
			if(!viewCodeMode) {
				lblPrintLink.Text = string.Format(@"<a id=""PrintLink"" href=""{0}"" title=""{1}"" target=""_blank"">{2}</a>",
					UrlTools.BuildUrl("Print.aspx?Page=", Tools.UrlEncode(currentPage.FullName), discussMode ? "&amp;Discuss=1" : ""),
					Properties.Messages.PrinterFriendlyVersion, Properties.Messages.Print);

				if(Settings.RssFeedsMode != RssFeedsMode.Disabled) {
					lblRssLink.Text = string.Format(@"<a id=""RssLink"" href=""{0}"" title=""{1}"" target=""_blank""{2}>RSS</a>",
						UrlTools.BuildUrl("RSS.aspx?Page=", Tools.UrlEncode(currentPage.FullName), discussMode ? "&amp;Discuss=1" : ""),
						discussMode ? Properties.Messages.RssForThisDiscussion : Properties.Messages.RssForThisPage,
						discussMode ? " class=\"discuss\"" : "");
				}
				else lblRssLink.Visible = false;
			}
			else {
				lblPrintLink.Visible = false;
				lblRssLink.Visible = false;
			}
		}

		/// <summary>
		/// Gets the categories for the current page, already formatted for display.
		/// </summary>
		/// <returns>The categories, formatted for display.</returns>
		private string GetFormattedPageCategories() {
			CategoryInfo[] categories = Pages.GetCategoriesForPage(currentPage);
			if(categories.Length == 0) {
				return string.Format(@"<i><a href=""{0}"" title=""{1}"">{2}</a></i>",
					GetCategoryLink("-"),
					Properties.Messages.Uncategorized, Properties.Messages.Uncategorized);
			}
			else {
				StringBuilder sb = new StringBuilder(categories.Length * 10);
				for(int i = 0; i < categories.Length; i++) {
					sb.AppendFormat(@"<a href=""{0}"" title=""{1}"">{2}</a>",
						GetCategoryLink(categories[i].FullName),
						NameTools.GetLocalName(categories[i].FullName),
						NameTools.GetLocalName(categories[i].FullName));
					if(i != categories.Length - 1) sb.Append(", ");
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets the link to a category.
		/// </summary>
		/// <param name="category">The full name of the category.</param>
		/// <returns>The link URL.</returns>
		private string GetCategoryLink(string category) {
			return UrlTools.BuildUrl("AllPages.aspx?Cat=", Tools.UrlEncode(category));
		}

		/// <summary>
		/// Sets the content of the META description and keywords for the current page.
		/// </summary>
		private void SetupMetaInformation() {
			// Set keywords and description
			if(currentContent.Keywords != null && currentContent.Keywords.Length > 0) {
				Literal lit = new Literal();
				lit.Text = string.Format("<meta name=\"keywords\" content=\"{0}\" />", PrintKeywords(currentContent.Keywords));
				Page.Header.Controls.Add(lit);
			}
			if(!string.IsNullOrEmpty(currentContent.Description)) {
				Literal lit = new Literal();
				lit.Text = string.Format("<meta name=\"description\" content=\"{0}\" />", currentContent.Description);
				Page.Header.Controls.Add(lit);
			}
		}

		/// <summary>
		/// Prints the keywords in a CSV list.
		/// </summary>
		/// <param name="keywords">The keywords.</param>
		/// <returns>The list.</returns>
		private string PrintKeywords(string[] keywords) {
			StringBuilder sb = new StringBuilder(50);
			for(int i = 0; i < keywords.Length; i++) {
				sb.Append(keywords[i]);
				if(i != keywords.Length - 1) sb.Append(", ");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Verifies the need for a page redirection, and performs it when appropriate.
		/// </summary>
		private void VerifyAndPerformPageRedirection() {
			if(currentPage == null) return;

			// Force formatting so that the destination can be detected
			Content.GetFormattedPageContent(currentPage, true);

			PageInfo dest = Redirections.GetDestination(currentPage);
			if(dest == null) return;

			if(dest != null) {
				if(Request["NoRedirect"] != "1") {
					UrlTools.Redirect(dest.FullName + Settings.PageExtension + "?From=" + currentPage.FullName, false);
				}
				else {
					// Write redirection hint
					StringBuilder sb = new StringBuilder();
					sb.Append(@"<div id=""RedirectionDiv"">");
					sb.Append(Properties.Messages.ThisPageRedirectsTo);
					sb.Append(": ");
					sb.Append(@"<a href=""");
					UrlTools.BuildUrl(sb, "++", Tools.UrlEncode(dest.FullName), Settings.PageExtension, "?From=", Tools.UrlEncode(currentPage.FullName));
					sb.Append(@""">");
					PageContent k = Content.GetPageContent(dest, true);
					sb.Append(FormattingPipeline.PrepareTitle(k.Title, false, FormattingContext.PageContent, currentPage));
					sb.Append("</a></div>");
					Literal literal = new Literal();
					literal.Text = sb.ToString();
					plhContent.Controls.Add(literal);
				}
			}
		}

		/// <summary>
		/// Sets the breadcrumbs trail, if appropriate.
		/// </summary>
		private void SetupBreadcrumbsTrail() {
			if(Settings.DisableBreadcrumbsTrail || discussMode || viewCodeMode) {
				lblBreadcrumbsTrail.Visible = false;
				return;
			}

			StringBuilder sb = new StringBuilder(1000);

			sb.Append(@"<div id=""BreadcrumbsDiv"">");

			PageInfo[] pageTrail = SessionFacade.Breadcrumbs.AllPages;
			int min = 3;
			if(pageTrail.Length < 3) min = pageTrail.Length;

			sb.Append(@"<div id=""BreadcrumbsDivMin"">");
			if(pageTrail.Length > 3) {
				// Write hyperLink
				sb.Append(@"<a href=""#"" onclick=""javascript:return __ShowAllTrail();"" title=""");
				sb.Append(Properties.Messages.ViewBreadcrumbsTrail);
				sb.Append(@""">(");
				sb.Append(pageTrail.Length.ToString());
				sb.Append(")</a> ");
			}

			for(int i = pageTrail.Length - min; i < pageTrail.Length; i++) {
				AppendBreadcrumb(sb, pageTrail[i], "s");
			}
			sb.Append("</div>");

			sb.Append(@"<div id=""BreadcrumbsDivAll"" style=""display: none;"">");
			// Write hyperLink
			sb.Append(@"<a href=""#"" onclick=""javascript:return __HideTrail();"" title=""");
			sb.Append(Properties.Messages.HideBreadcrumbsTrail);
			sb.Append(@""">[X]</a> ");
			for(int i = 0; i < pageTrail.Length; i++) {
				AppendBreadcrumb(sb, pageTrail[i], "f");
			}
			sb.Append("</div>");

			sb.Append("</div>");

			lblBreadcrumbsTrail.Text = sb.ToString();
		}

		/// <summary>
		/// Appends a breadbrumb trail element.
		/// </summary>
		/// <param name="sb">The destination <see cref="T:StringBuilder" />.</param>
		/// <param name="page">The page to append.</param>
		/// <param name="dpPrefix">The drop-down menu ID prefix.</param>
		private void AppendBreadcrumb(StringBuilder sb, PageInfo page, string dpPrefix) {
			PageNameComparer comp = new PageNameComparer();
			PageContent pc = Content.GetPageContent(page, true);

			string id = AppendBreadcrumbDropDown(sb, page, dpPrefix);

			string nspace = NameTools.GetNamespace(page.FullName);

			sb.Append("&raquo; ");
			if(comp.Compare(page, currentPage) == 0) sb.Append("<b>");
			sb.AppendFormat(@"<a href=""{0}"" title=""{1}""{2}{3}{4}>{1}</a>",
				Tools.UrlEncode(page.FullName) + Settings.PageExtension,
				FormattingPipeline.PrepareTitle(pc.Title, false, FormattingContext.PageContent, currentPage) + (string.IsNullOrEmpty(nspace) ? "" : (" (" + NameTools.GetNamespace(page.FullName) + ")")),
				(id != null ? @" onmouseover=""javascript:return __ShowDropDown(event, '" + id + @"', this);""" : ""),
				(id != null ? @" id=""lnk" + id + @"""" : ""),
				(id != null ? @" onmouseout=""javascript:return __HideDropDown('" + id + @"');""" : ""));
			if(comp.Compare(page, currentPage) == 0) sb.Append("</b>");
			sb.Append(" ");
		}

		/// <summary>
		/// Appends the drop-down menu DIV with outgoing links for a page.
		/// </summary>
		/// <param name="sb">The destination <see cref="T:StringBuilder" />.</param>
		/// <param name="page">The page.</param>
		/// <param name="dbPrefix">The drop-down menu DIV ID prefix.</param>
		/// <returns>The DIV ID, or <c>null</c> if no target pages were found.</returns>
		private string AppendBreadcrumbDropDown(StringBuilder sb, PageInfo page, string dbPrefix) {
			// Build outgoing links list
			// Generate list DIV
			// Return DIV's ID

			string[] outgoingLinks = Pages.GetPageOutgoingLinks(page);
			if(outgoingLinks == null || outgoingLinks.Length == 0) return null;

			string id = dbPrefix + Guid.NewGuid().ToString();

			StringBuilder buffer = new StringBuilder(300);

			buffer.AppendFormat(@"<div id=""{0}"" style=""display: none;"" class=""pageoutgoinglinksmenu"" onmouseover=""javascript:return __CancelHideTimer();"" onmouseout=""javascript:return __HideDropDown('{0}');"">", id);
			int count = 0;
			foreach(string link in outgoingLinks) {
				PageInfo target = Pages.FindPage(link);
				if(target != null) {
					count++;
					PageContent cont = Content.GetPageContent(target, true);

					string title = FormattingPipeline.PrepareTitle(cont.Title, false, FormattingContext.PageContent, currentPage);

					buffer.AppendFormat(@"<a href=""{0}{1}"" title=""{2}"">{2}</a>", link, Settings.PageExtension, title, title);
				}
				if(count >= 20) break;
			}
			buffer.Append("</div>");

			sb.Insert(0, buffer.ToString());

			if(count > 0) return id;
			else return null;
		}

		/// <summary>
		/// Sets the redirection source page link, if appropriate.
		/// </summary>
		private void SetupRedirectionSource() {
			if(Request["From"] != null) {

				PageInfo source = Pages.FindPage(Request["From"]);

				if(source != null) {
					StringBuilder sb = new StringBuilder(300);
					sb.Append(@"<div id=""RedirectionInfoDiv"">");
					sb.Append(Properties.Messages.RedirectedFrom);
					sb.Append(": ");
					sb.Append(@"<a href=""");
					sb.Append(UrlTools.BuildUrl("++", Tools.UrlEncode(source.FullName), Settings.PageExtension, "?NoRedirect=1"));
					sb.Append(@""">");
					PageContent w = Content.GetPageContent(source, true);
					sb.Append(FormattingPipeline.PrepareTitle(w.Title, false, FormattingContext.PageContent, currentPage));
					sb.Append("</a></div>");

					lblRedirectionSource.Text = sb.ToString();
				}
				else lblRedirectionSource.Visible = false;
			}
			else lblRedirectionSource.Visible = false;
		}

		/// <summary>
		/// Sets the navigation paths label.
		/// </summary>
		private void SetupNavigationPaths() {
			string[] paths = NavigationPaths.PathsPerPage(currentPage);

			string currentPath = Request["NavPath"];
			if(!string.IsNullOrEmpty(currentPath)) currentPath = currentPath.ToLowerInvariant();

			if(!discussMode && !viewCodeMode && paths.Length > 0) {
				StringBuilder sb = new StringBuilder(500);
				sb.Append(Properties.Messages.Paths);
				sb.Append(": ");
				for(int i = 0; i < paths.Length; i++) {
					NavigationPath path = NavigationPaths.Find(paths[i]);
					if(path != null) {
						if(currentPath != null && paths[i].ToLowerInvariant().Equals(currentPath)) sb.Append("<b>");

						sb.Append(@"<a href=""");
						sb.Append(UrlTools.BuildUrl("Default.aspx?Page=", Tools.UrlEncode(currentPage.FullName), "&amp;NavPath=", Tools.UrlEncode(paths[i])));
						sb.Append(@""" title=""");
						sb.Append(NameTools.GetLocalName(path.FullName));
						sb.Append(@""">");
						sb.Append(NameTools.GetLocalName(path.FullName));
						sb.Append("</a>");

						if(currentPath != null && paths[i].ToLowerInvariant().Equals(currentPath)) sb.Append("</b>");
						if(i != paths.Length - 1) sb.Append(", ");
					}
				}

				lblNavigationPaths.Text = sb.ToString();
			}
			else lblNavigationPaths.Visible = false;
		}

		/// <summary>
		/// Prepares the previous and next pages link for navigation paths.
		/// </summary>
		/// <param name="previousPageLink">The previous page link.</param>
		/// <param name="nextPageLink">The next page link.</param>
		private void SetupAdjacentPages() {
			StringBuilder prev = new StringBuilder(50), next = new StringBuilder(50);

			if(Request["NavPath"] != null) {
				NavigationPath path = NavigationPaths.Find(Request["NavPath"]);

				if(path != null) {
					int idx = Array.IndexOf(path.Pages, currentPage.FullName);
					if(idx != -1) {
						if(idx > 0) {
							PageInfo prevPage = Pages.FindPage(path.Pages[idx - 1]);
							prev.Append(@"<a href=""");
							UrlTools.BuildUrl(prev, "Default.aspx?Page=", Tools.UrlEncode(prevPage.FullName),
								"&amp;NavPath=", Tools.UrlEncode(path.FullName));

							prev.Append(@""" title=""");
							prev.Append(Properties.Messages.PrevPage);
							prev.Append(": ");
							prev.Append(FormattingPipeline.PrepareTitle(Content.GetPageContent(prevPage, true).Title, false, FormattingContext.PageContent, currentPage));
							prev.Append(@"""><b>&laquo;</b></a> ");
						}
						if(idx < path.Pages.Length - 1) {
							PageInfo nextPage = Pages.FindPage(path.Pages[idx + 1]);
							next.Append(@" <a href=""");
							UrlTools.BuildUrl(next, "Default.aspx?Page=", Tools.UrlEncode(nextPage.FullName),
								"&amp;NavPath=", Tools.UrlEncode(path.FullName));

							next.Append(@""" title=""");
							next.Append(Properties.Messages.NextPage);
							next.Append(": ");
							next.Append(FormattingPipeline.PrepareTitle(Content.GetPageContent(nextPage, true).Title, false, FormattingContext.PageContent, currentPage));
							next.Append(@"""><b>&raquo;</b></a>");
						}
					}
				}
			}

			if(prev.Length > 0) {
				lblPreviousPage.Text = prev.ToString();
			}
			else lblPreviousPage.Visible = false;

			if(next.Length > 0) {
				lblNextPage.Text = next.ToString();
			}
			else lblNextPage.Visible = false;
		}

		/// <summary>
		/// Sets the JavaScript double-click editing handler.
		/// </summary>
		private void SetupDoubleClickHandler() {
			if(Settings.EnableDoubleClickEditing && !discussMode && !viewCodeMode) {
				StringBuilder sb = new StringBuilder(200);
				sb.Append(@"<script type=""text/javascript"">" + "\n");
				sb.Append("<!--\n");
				sb.Append("document.ondblclick = function() {\n");
				sb.Append("document.location = '");
				sb.Append(UrlTools.BuildUrl("Edit.aspx?Page=", Tools.UrlEncode(currentPage.FullName)));
				sb.Append("';\n");
				sb.Append("}\n");
				sb.Append("// -->\n");
				sb.Append("</script>");

				lblDoubleClickHandler.Text = sb.ToString();
			}
			else lblDoubleClickHandler.Visible = false;
		}

		/// <summary>
		/// Sets the email notification button.
		/// </summary>
		private void SetupEmailNotification() {
			if(SessionFacade.LoginKey != null && SessionFacade.CurrentUsername != "admin") {
				bool pageChanges = false;
				bool discussionMessages = false;

				UserInfo user = SessionFacade.GetCurrentUser();
				if(user != null && user.Provider.UsersDataReadOnly) {
					btnEmailNotification.Visible = false;
					return;
				}

				if(user != null) {
					Users.GetEmailNotification(user, currentPage, out pageChanges, out discussionMessages);
				}

				bool active = false;
				if(discussMode) {
					active = discussionMessages;
				}
				else {
					active = pageChanges;
				}

				if(active) {
					btnEmailNotification.CssClass = "activenotification" + (discussMode ? " discuss" : "");
					btnEmailNotification.ToolTip = Properties.Messages.EmailNotificationsAreActive;
				}
				else {
					btnEmailNotification.CssClass = "inactivenotification" + (discussMode ? " discuss" : "");
					btnEmailNotification.ToolTip = Properties.Messages.ClickToEnableEmailNotifications;
				}
			}
			else btnEmailNotification.Visible = false;
		}

		protected void btnEmailNotification_Click(object sender, EventArgs e) {
			bool pageChanges = false;
			bool discussionMessages = false;

			UserInfo user = SessionFacade.GetCurrentUser();
			if(user != null) {
				Users.GetEmailNotification(user, currentPage, out pageChanges, out discussionMessages);
			}

			if(discussMode) {
				Users.SetEmailNotification(user, currentPage, pageChanges, !discussionMessages);
			}
			else {
				Users.SetEmailNotification(user, currentPage, !pageChanges, discussionMessages);
			}

			SetupEmailNotification();
		}

		/// <summary>
		/// Sets the actual page content, based on the current view mode (normal, discussion, view code).
		/// </summary>
		/// <param name="canPostMessages">A value indicating whether the current user can post messages.</param>
		/// <param name="canManageDiscussion">A value indicating whether the current user can manage the discussion.</param>
		private void SetupPageContent(bool canPostMessages, bool canManageDiscussion) {
			if(!discussMode && !viewCodeMode) {
				Literal literal = new Literal();
				literal.Text = Content.GetFormattedPageContent(currentPage, true);
				plhContent.Controls.Add(literal);
			}
			else if(!discussMode && viewCodeMode) {
				if(Settings.EnableViewPageCodeFeature) {
					Literal literal = new Literal();
					StringBuilder sb = new StringBuilder(currentContent.Content.Length + 100);
					sb.Append(@"<textarea style=""width: 98%; height: 500px;"" readonly=""true"">");
					sb.Append(Server.HtmlEncode(currentContent.Content));
					sb.Append("</textarea>");
					sb.Append("<br /><br />");
					sb.Append(Properties.Messages.MetaKeywords);
					sb.Append(": <b>");
					sb.Append(PrintKeywords(currentContent.Keywords));
					sb.Append("</b><br />");
					sb.Append(Properties.Messages.MetaDescription);
					sb.Append(": <b>");
					sb.Append(currentContent.Description);
					sb.Append("</b><br />");
					sb.Append(Properties.Messages.ChangeComment);
					sb.Append(": <b>");
					sb.Append(currentContent.Comment);
					sb.Append("</b>");
					literal.Text = sb.ToString();
					plhContent.Controls.Add(literal);
				}
			}
			else if(discussMode && !viewCodeMode) {
				PageDiscussion discussion = LoadControl("~/PageDiscussion.ascx") as PageDiscussion;
				discussion.CurrentPage = currentPage;
				discussion.CanPostMessages = canPostMessages;
				discussion.CanManageDiscussion = canManageDiscussion;
				plhContent.Controls.Add(discussion);
			}
		}

	}

}
