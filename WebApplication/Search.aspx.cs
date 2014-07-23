
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;
using System.Data;

namespace ScrewTurn.Wiki {

	public partial class Search : BasePage {

		private const int MaxResults = 30;

		private readonly Dictionary<string, SearchOptions> searchModeMap =
			new Dictionary<string, SearchOptions>() { { "1", SearchOptions.AtLeastOneWord }, { "2", SearchOptions.AllWords }, { "3", SearchOptions.ExactPhrase } };

		protected void Page_Load(object sender, EventArgs e) {
			if(Request["OpenSearch"] != null) {
				GenerateOpenSearchDescription();
				return;
			}

			Page.Title = Properties.Messages.SearchTitle + " - " + Settings.WikiTitle;

			lblStrings.Text = string.Format("<script type=\"text/javascript\"><!--\r\nvar AllNamespacesCheckbox = '{0}';\r\n//-->\r\n</script>", chkAllNamespaces.ClientID);

			txtQuery.Focus();

			if(!Page.IsPostBack) {
				// Initialize all controls

				string[] queryStringCategories = null;
				if(Request["Categories"] != null) {
					queryStringCategories = Request["Categories"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					Array.Sort(queryStringCategories);
				}

				if(Request["SearchUncategorized"] != null) {
					chkUncategorizedPages.Checked = Request["SearchUncategorized"] == "1";
				}

				if(Request["Query"] != null) {
					chkAllNamespaces.Checked = Request["AllNamespaces"] == "1";
					chkFilesAndAttachments.Checked = Request["FilesAndAttachments"] == "1";
				}

				if(chkAllNamespaces.Checked) {
					lblHideCategoriesScript.Text = "<script type=\"text/javascript\"><!--\r\ndocument.getElementById('CategoryFilterDiv').style['display'] = 'none';\r\n//-->\r\n</script>";
				}

				List<CategoryInfo> allCategories = Pages.GetCategories(DetectNamespaceInfo());

				lstCategories.Items.Clear();

				List<string> selectedCategories = new List<string>(allCategories.Count);

				// Populate categories list and select specified categories if any
				foreach(CategoryInfo cat in allCategories) {
					ListItem item = new ListItem(NameTools.GetLocalName(cat.FullName), cat.FullName);
					if(queryStringCategories != null) {
						if(Array.Find(queryStringCategories, delegate(string c) { return c == cat.FullName; }) != null) {
							item.Selected = true;
							selectedCategories.Add(cat.FullName);
						}
					}
					else {
						item.Selected = true;
						selectedCategories.Add(cat.FullName);
					}
					lstCategories.Items.Add(item);
				}

				// Select mode if specified
				if(Request["Mode"] != null) {
					switch(Request["Mode"]) {
						case "1":
							rdoAtLeastOneWord.Checked = true;
							rdoAllWords.Checked = false;
							rdoExactPhrase.Checked = false;
							break;
						case "2":
							rdoAtLeastOneWord.Checked = false;
							rdoAllWords.Checked = true;
							rdoExactPhrase.Checked = false;
							break;
						default:
							rdoAtLeastOneWord.Checked = false;
							rdoAllWords.Checked = false;
							rdoExactPhrase.Checked = true;
							break;
					}
				}

				if(Request["Query"] != null) {
					txtQuery.Text = Request["Query"];

					btnGo_Click(sender, e);
				}
			}
		}

		protected void btnGo_Click(object sender, EventArgs e) {
			// Redirect firing the search

			//UrlTools.Redirect(UrlTools.BuildUrl("Search.aspx?Query=", Tools.UrlEncode(txtQuery.Text),
			//    "&SearchUncategorized=", chkUncategorizedPages.Checked ? "1" : "0",
			//    "&Categories=", GetCategories(),
			//    "&Mode=", GetMode(),
			//    chkAllNamespaces.Checked ? "&AllNamespaces=1" : "",
			//    chkFilesAndAttachments.Checked ? "&FilesAndAttachments=1" : ""));

			//if(Request["Query"] != null) txtQuery.Text = Request["Query"];

			string query = ScrewTurn.Wiki.SearchEngine.Tools.RemoveDiacriticsAndPunctuation(txtQuery.Text, false);

			PerformSearch(query, searchModeMap[GetMode()], GetSelectedCategories(), chkUncategorizedPages.Checked, chkAllNamespaces.Checked, chkFilesAndAttachments.Checked);
		}

		/// <summary>
		/// Gets the selected categories.
		/// </summary>
		/// <returns>The selected categories.</returns>
		private List<string> GetSelectedCategories() {
			List<string> categories = new List<string>();
			foreach(ListItem item in lstCategories.Items) {
				if(item.Selected) {
					categories.Add(item.Value);
				}
			}
			return categories;
		}

		/// <summary>
		/// Gets the search mode string.
		/// </summary>
		/// <returns>The search mode string.</returns>
		private string GetMode() {
			if(rdoAtLeastOneWord.Checked) return "1";
			else if(rdoAllWords.Checked) return "2";
			else return "3";
		}

		/// <summary>
		/// Performs a search.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="mode">The search mode.</param>
		/// <param name="selectedCategories">The selected categories.</param>
		/// <param name="searchUncategorized">A value indicating whether to search uncategorized pages.</param>
		/// <param name="searchInAllNamespacesAndCategories">A value indicating whether to search in all namespaces and categories.</param>
		/// <param name="searchFilesAndAttachments">A value indicating whether to search files and attachments.</param>
		private void PerformSearch(string query, SearchOptions mode, List<string> selectedCategories, bool searchUncategorized, bool searchInAllNamespacesAndCategories, bool searchFilesAndAttachments) {
			SearchResultCollection results = null;
			DateTime begin = DateTime.Now;
			try {
				results = SearchTools.Search(query, true, searchFilesAndAttachments, mode);
			}
			catch(ArgumentException ex) {
				Log.LogEntry("Search threw an exception\n" + ex.ToString(), EntryType.Warning, SessionFacade.CurrentUsername);
				results = new SearchResultCollection();
			}
			DateTime end = DateTime.Now;

			// Build a list of SearchResultRow for display in the repeater
			List<SearchResultRow> rows = new List<SearchResultRow>(Math.Min(results.Count, MaxResults));

			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			CategoryInfo[] pageCategories;
			int count = 0;
			foreach(SearchResult res in results) {
				// Filter by category
				PageInfo currentPage = null;
				pageCategories = new CategoryInfo[0];

				if(res.Document.TypeTag == PageDocument.StandardTypeTag) {
					currentPage = (res.Document as PageDocument).PageInfo;
					pageCategories = Pages.GetCategoriesForPage(currentPage);

					// Verify permissions
					bool canReadPage = AuthChecker.CheckActionForPage(currentPage,
						Actions.ForPages.ReadPage, currentUser, currentGroups);
					if(!canReadPage) continue; // Skip
				}
				else if(res.Document.TypeTag == MessageDocument.StandardTypeTag) {
					currentPage = (res.Document as MessageDocument).PageInfo;
					pageCategories = Pages.GetCategoriesForPage(currentPage);

					// Verify permissions
					bool canReadDiscussion = AuthChecker.CheckActionForPage(currentPage,
						Actions.ForPages.ReadDiscussion, currentUser, currentGroups);
					if(!canReadDiscussion) continue; // Skip
				}
				else if(res.Document.TypeTag == PageAttachmentDocument.StandardTypeTag) {
					currentPage = (res.Document as PageAttachmentDocument).Page;
					pageCategories = Pages.GetCategoriesForPage(currentPage);

					// Verify permissions
					bool canDownloadAttn = AuthChecker.CheckActionForPage(currentPage,
						Actions.ForPages.DownloadAttachments, currentUser, currentGroups);
					if(!canDownloadAttn) continue; // Skip
				}
				else if(res.Document.TypeTag == FileDocument.StandardTypeTag) {
					string[] fields = ((FileDocument)res.Document).Name.Split('|');
					IFilesStorageProviderV30 provider = Collectors.FilesProviderCollector.GetProvider(fields[0]);
					string directory = Tools.GetDirectoryName(fields[1]);

					// Verify permissions
					bool canDownloadFiles = AuthChecker.CheckActionForDirectory(provider, directory,
						Actions.ForDirectories.DownloadFiles, currentUser, currentGroups);
					if(!canDownloadFiles) continue; // Skip
				}

				string currentNamespace = DetectNamespace();
				if(string.IsNullOrEmpty(currentNamespace)) currentNamespace = null;

				if(currentPage != null) {
					// Check categories match, if page is set

					if(searchInAllNamespacesAndCategories ||
						Array.Find(pageCategories,
						delegate(CategoryInfo c) {
							return selectedCategories.Contains(c.FullName);
						}) != null || pageCategories.Length == 0 && searchUncategorized) {

						// ... then namespace
						if(searchInAllNamespacesAndCategories ||
							NameTools.GetNamespace(currentPage.FullName) == currentNamespace) {

							rows.Add(SearchResultRow.CreateInstance(res));
							count++;
						}
					}
				}
				else {
					// No associated page (-> file), add result
					rows.Add(SearchResultRow.CreateInstance(res));
					count++;
				}

				if(count >= MaxResults) break;
			}

			rptResults.DataSource = rows;
			rptResults.DataBind();

			PrintStats(end - begin, rows.Count);
		}

		/// <summary>
		/// Prints the search statistics.
		/// </summary>
		/// <param name="time">The time the search required.</param>
		/// <param name="results">The number of results.</param>
		private void PrintStats(TimeSpan time, int results) {
			int totalDocuments = 0;
			int totalWords = 0;
			long totalSize = 0;

			foreach(IPagesStorageProviderV30 prov in Collectors.PagesProviderCollector.AllProviders) {
				int dc, wc, oc;
				long s;
				prov.GetIndexStats(out dc, out wc, out oc, out s);
				totalDocuments += dc;
				totalWords += wc;
				totalSize += s;
			}

			lblStats.Text = string.Format(Properties.Messages.SearchStats,
				Tools.BytesToString(totalSize), totalDocuments, totalWords, time.TotalSeconds, results);
		}

		/// <summary>
		/// Generates the OpenSearch description XML document and renders it to output.
		/// </summary>
		private void GenerateOpenSearchDescription() {
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<OpenSearchDescription xmlns=""http://a9.com/-/spec/opensearch/1.1/"">
	<ShortName>{0}</ShortName>
	<Description>{1}</Description>
	<Url type=""text/html"" method=""get"" template=""{2}Search.aspx?AllNamespaces=1&amp;FilesAndAttachments=1&amp;Query={3}""/>
	<Image width=""16"" height=""16"" type=""image/x-icon"">{2}{4}</Image>
	<InputEncoding>UTF-8</InputEncoding>
	<SearchForm>{2}Search.aspx</SearchForm>
</OpenSearchDescription>";

			Response.Clear();
			Response.AddHeader("content-type", "application/opensearchdescription+xml");
			Response.AddHeader("content-disposition", "inline;filename=search.xml");
			Response.Write(
				string.Format(xml,
					Settings.WikiTitle,
					Settings.WikiTitle + " - Search",
					Settings.MainUrl,
					"{searchTerms}",
					"Images/SearchIcon.ico"));
			Response.End();
		}

	}

	/// <summary>
	/// Represents a search result in a format useful for screen display.
	/// </summary>
	public class SearchResultRow {

		public const string Page = "page";
		public const string Message = "message";
		public const string File = "file";
		public const string Attachment = "attachment";

		private string link;
		private string type;
		private string title;
		private float relevance;
		private string formattedExcerpt;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CompactSearchResult" /> class.
		/// </summary>
		/// <param name="link">The link.</param>
		/// <param name="type">The result type.</param>
		/// <param name="title">The title.</param>
		/// <param name="relevance">The relevance (%).</param>
		/// <param name="formattedExcerpt">The formatted page excerpt.</param>
		public SearchResultRow(string link, string type, string title, float relevance, string formattedExcerpt) {
			this.link = link;
			this.type = type;
			this.title = title;
			this.relevance = relevance;
			this.formattedExcerpt = formattedExcerpt;
		}

		/// <summary>
		/// Gets the page.
		/// </summary>
		public string Link {
			get { return link; }
		}

		/// <summary>
		/// Gets the type of the result.
		/// </summary>
		public string Type {
			get { return type; }
		}

		/// <summary>
		/// Gets the title.
		/// </summary>
		public string Title {
			get { return title; }
		}

		/// <summary>
		/// Gets the relevance.
		/// </summary>
		public float Relevance {
			get { return relevance; }
		}

		/// <summary>
		/// Gets the formatted excerpt.
		/// </summary>
		public string FormattedExcerpt {
			get { return formattedExcerpt; }
		}

		/// <summary>
		/// Creates a new instance of the <see cref="T:SearchResultRow" /> class.
		/// </summary>
		/// <param name="result">The result to use.</param>
		/// <returns>The instance.</returns>
		public static SearchResultRow CreateInstance(SearchResult result) {
			string queryStringKeywords = "HL=" + GetKeywordsForQueryString(result.Matches);

			if(result.Document.TypeTag == PageDocument.StandardTypeTag) {
				PageDocument pageDoc = result.Document as PageDocument;

				return new SearchResultRow(pageDoc.PageInfo.FullName + Settings.PageExtension + "?" + queryStringKeywords, Page,
					FormattingPipeline.PrepareTitle(pageDoc.Title, false, FormattingContext.PageContent, pageDoc.PageInfo),
					result.Relevance.Value, GetExcerpt(pageDoc.PageInfo, result.Matches));
			}
			else if(result.Document.TypeTag == MessageDocument.StandardTypeTag) {
				MessageDocument msgDoc = result.Document as MessageDocument;

				PageContent content = Content.GetPageContent(msgDoc.PageInfo, true);

				return new SearchResultRow(msgDoc.PageInfo.FullName + Settings.PageExtension + "?" + queryStringKeywords +"&amp;Discuss=1#" + Tools.GetMessageIdForAnchor(msgDoc.DateTime), Message,
					FormattingPipeline.PrepareTitle(msgDoc.Title, false, FormattingContext.MessageBody, content.PageInfo) + " (" +
					FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.MessageBody, content.PageInfo) +
					")", result.Relevance.Value, GetExcerpt(msgDoc.PageInfo, msgDoc.MessageID, result.Matches));
			}
			else if(result.Document.TypeTag == FileDocument.StandardTypeTag) {
				FileDocument fileDoc = result.Document as FileDocument;

				return new SearchResultRow("GetFile.aspx?File=" + Tools.UrlEncode(fileDoc.Name.Substring(fileDoc.Provider.Length + 1)) +
					"&amp;Provider=" + Tools.UrlEncode(fileDoc.Provider),
					File, fileDoc.Title, result.Relevance.Value, "");
			}
			else if(result.Document.TypeTag == PageAttachmentDocument.StandardTypeTag) {
				PageAttachmentDocument attnDoc = result.Document as PageAttachmentDocument;
				PageContent content = Content.GetPageContent(attnDoc.Page, false);

				return new SearchResultRow(attnDoc.Page.FullName + Settings.PageExtension, Attachment,
					attnDoc.Title + " (" +
					FormattingPipeline.PrepareTitle(content.Title, false, FormattingContext.PageContent, content.PageInfo) +
					")", result.Relevance.Value, "");
			}
			else throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the formatted page excerpt.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="matches">The matches to highlight.</param>
		/// <returns>The excerpt.</returns>
		private static string GetExcerpt(PageInfo page, WordInfoCollection matches) {
			PageContent pageContent = Content.GetPageContent(page, true);
			string content = pageContent.Content;

			List<WordInfo> sortedMatches = new List<WordInfo>(matches);
			sortedMatches.RemoveAll(delegate(WordInfo wi) { return wi.Location != WordLocation.Content; });
			sortedMatches.Sort(delegate(WordInfo x, WordInfo y) { return x.FirstCharIndex.CompareTo(y.FirstCharIndex); });

			return BuildFormattedExcerpt(sortedMatches, Host.Instance.PrepareContentForIndexing(page, content));
		}

		/// <summary>
		/// Gets the formatted message excerpt.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="messageID">The message ID.</param>
		/// <param name="matches">The matches to highlight.</param>
		/// <returns>The excerpt.</returns>
		private static string GetExcerpt(PageInfo page, int messageID, WordInfoCollection matches) {
			Message message = Pages.FindMessage(Pages.GetPageMessages(page), messageID);

			string content = message.Body;

			List<WordInfo> sortedMatches = new List<WordInfo>(matches);
			sortedMatches.RemoveAll(delegate(WordInfo wi) { return wi.Location != WordLocation.Content; });
			sortedMatches.Sort(delegate(WordInfo x, WordInfo y) { return x.FirstCharIndex.CompareTo(y.FirstCharIndex); });

			return BuildFormattedExcerpt(sortedMatches, Host.Instance.PrepareContentForIndexing(null, content));
		}

		/// <summary>
		/// Builds the formatted excerpt for a search match.
		/// </summary>
		/// <param name="matches">The regex matches.</param>
		/// <param name="input">The original input text.</param>
		/// <returns>The formatted excerpt.</returns>
		private static string BuildFormattedExcerpt(List<WordInfo> matches, string input) {
			// Highlight all the matches in the original string, then cut it

			int shift = 100;
			int maxLen = 600;
			string highlightOpen = "<b class=\"searchkeyword\">";
			string highlightClose = "</b>";

			StringBuilder sb = new StringBuilder(input);

			for(int i = 0; i < matches.Count; i++) {
				WordInfo match = matches[i];

				int openIndex = match.FirstCharIndex + i * (highlightOpen.Length + highlightClose.Length);
				bool openIndexOk = openIndex >= 0 && openIndex <= sb.Length;
				if(openIndexOk) sb.Insert(openIndex, highlightOpen);

				int closeIndex = match.FirstCharIndex + match.Text.Length + highlightOpen.Length + i * (highlightOpen.Length + highlightClose.Length);
				if(openIndexOk && closeIndex >= 0 && closeIndex <= sb.Length) sb.Insert(closeIndex, highlightClose);
				else if(openIndexOk) sb.Append(highlightClose); // Make sure an open tags is also closed

			}

			bool startsAtZero = false, endsAtEnd = false;

			string result = "";
			if(matches.Count > 0) {
				int start = matches[0].FirstCharIndex - shift;
				if(start < 0) {
					start = 0;
					startsAtZero = true;
				}
				int len = matches[matches.Count - 1].FirstCharIndex + matches[matches.Count - 1].Text.Length + shift - matches.Count * (highlightOpen.Length + highlightClose.Length) - start;
				if(start + len >= sb.Length) {
					len = sb.Length - start;
					endsAtEnd = true;
				}
				if(len <= 0) len = sb.Length; // HACK: This should never occur, but if it does it crashes the wiki, so set it to max len
				if(len > maxLen) len = maxLen;

				result = sb.ToString();

				// Cut string without breaking words
				while(start > 0 && result[start] != ' ') {
					start--;
					len++;
				}
				while(start + len < result.Length && result[start + len] != ' ') len++;

				result = sb.ToString().Substring(start, len);
			}
			else {
				// Extract an initial piece of the content (300 chars)
				startsAtZero = true;
				endsAtEnd = true;
				if(input.Length < 300) result = input;
				else {
					endsAtEnd = false;
					result = input.Substring(0, 300);
					// Cut string without breaking words (this will require just a few iterations)
					while(result.Length < input.Length && input[result.Length] != ' ') {
						result += input[result.Length];
					}
				}
			}

			if(!startsAtZero) result = "[...] " + result;
			if(!endsAtEnd) result += " [...]";

			return result;
		}

		/// <summary>
		/// Gets a list of keywords formatted for the query string.
		/// </summary>
		/// <param name="matches">The search keywords.</param>
		/// <returns>The formatted list, for example 'word1,word2,word3'.</returns>
		private static string GetKeywordsForQueryString(WordInfoCollection matches) {
			StringBuilder buffer = new StringBuilder(100);
			List<string> added = new List<string>(5);

			for(int i = 0; i < matches.Count; i++) {
				if(matches[i].Text.Length > 1 && !added.Contains(matches[i].Text)) {
					buffer.Append(Tools.UrlEncode(matches[i].Text));
					if(i != matches.Count - 1) buffer.Append(",");
					added.Add(matches[i].Text);
				}
			}

			return buffer.ToString().TrimEnd(',');
		}

	}

}
