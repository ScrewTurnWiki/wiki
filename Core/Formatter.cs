
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Performs all the text formatting and parsing operations.
	/// </summary>
	public static class Formatter {

		private static readonly Regex NoWikiRegex = new Regex(@"\<nowiki\>(.|\n|\r)+?\<\/nowiki\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex NoSingleBr = new Regex(@"\<nobr\>(.|\n|\r)+?\<\/nobr\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex LinkRegex = new Regex(@"(\[\[.+?\]\])|(\[.+?\])", RegexOptions.Compiled);
		private static readonly Regex RedirectionRegex = new Regex(@"^\ *\>\>\>\ *.+\ *$", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex H1Regex = new Regex(@"^==.+?==\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex H2Regex = new Regex(@"^===.+?===\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex H3Regex = new Regex(@"^====.+?====\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex H4Regex = new Regex(@"^=====.+?=====\n?", RegexOptions.Compiled | RegexOptions.Multiline);
		private static readonly Regex BoldRegex = new Regex(@"'''.+?'''", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex ItalicRegex = new Regex(@"''.+?''", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex BoldItalicRegex = new Regex(@"'''''.+?'''''", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex ApexRegex = new Regex(@"\&lt;sup\&gt(.+?)\&lt;/sup\&gt;", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex SubscribeRegex = new Regex(@"\&lt;sub\&gt(.+?)\&lt;/sub\&gt;",RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex UnderlinedRegex = new Regex(@"__.+?__", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex StrikedRegex = new Regex(@"(?<!(\<\!|\&lt;))(\-\-(?!\>).+?\-\-)(?!(\>|\&gt;))", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex CodeRegex = new Regex(@"\{\{.+?\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex PreRegex = new Regex(@"\{\{\{\{.+?\}\}\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex BoxRegex = new Regex(@"\(\(\(.+?\)\)\)", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex ExtendedUpRegex = new Regex(@"\{up((\:|\().+?)?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex SpecialTagRegex = new Regex(@"\{(wikititle|wikiversion|mainurl|rsspage|themepath|clear|top|searchbox|pagecount|pagecount\(\*\)|categories|cloud|orphans|wanted|namespacelist)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex SpecialTagBRRegex = new Regex(@"\{(br)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);		
		private static readonly Regex Phase3SpecialTagRegex = new Regex(@"\{(username|pagename|loginlogout|namespace|namespacedropdown|incoming|outgoing)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex RecentChangesRegex = new Regex(@"\{recentchanges(\(\*\))?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex ListRegex = new Regex(@"(?<=(\n|^))((\*|\#)+(\ )?.+?\n)+((?=\n)|\z)", RegexOptions.Compiled | RegexOptions.Singleline); // Singleline to matche list elements on multiple lines
		private static readonly Regex TocRegex = new Regex(@"\{toc\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex TransclusionRegex = new Regex(@"\{T(\:|\|).+?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex HRRegex = new Regex(@"(?<=(\n|^))(\ )*----(\ )*\n", RegexOptions.Compiled);
		private static readonly Regex SnippetRegex = new Regex(@"\{s\:(.+?)(\|.*?)*\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
		private static readonly Regex ClassicSnippetVerifier = new Regex(@"\|\ *[\w\d]+\ *\=", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex TableRegex = new Regex(@"\{\|(\ [^\n]*)?\n.+?\|\}", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex IndentRegex = new Regex(@"(?<=(\n|^))\:+(\ )?.+?\n", RegexOptions.Compiled);
		private static readonly Regex EscRegex = new Regex(@"\<esc\>(.|\n|\r)*?\<\/esc\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
		private static readonly Regex SignRegex = new Regex(@"§§\(.+?\)§§", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		// This regex is duplicated in Edit.aspx.cs
		private static readonly Regex FullCodeRegex = new Regex(@"@@.+?@@", RegexOptions.Compiled | RegexOptions.Singleline);
		private static readonly Regex JavascriptRegex = new Regex(@"\<script.*?\>.*?\<\/script\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
		private static readonly Regex CommentRegex = new Regex(@"(?<!(\<script.*?\>[\s\n]*))\<\!\-\-.*?\-\-\>(?!([\s\n]*\<\/script\>))", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

		/// <summary>
		/// The section editing button placeholder.
		/// </summary>
		public const string EditSectionPlaceHolder = "%%%%EditSectionPlaceHolder%%%%"; // This string is also used in History.aspx.cs
		private const string TocTitlePlaceHolder = "%%%%TocTitlePlaceHolder%%%%";
		private const string UpReplacement = "GetFile.aspx?File=";
		private const string ExtendedUpReplacement = "GetFile.aspx?$File=";
		private const string ExtendedUpReplacementForAttachment = "GetFile.aspx?$Page=@&File=";
		private const string SingleBrPlaceHolder = "%%%%SingleBrPlaceHolder%%%%";
		private const string SectionLinkTextPlaceHolder = "%%%%SectionLinkTextPlaceHolder%%%%";

		/// <summary>
		/// Detects the current namespace.
		/// </summary>
		/// <param name="currentPage">The current page, if any.</param>
		/// <returns>The current namespace (<c>null</c> for the root).</returns>
		private static NamespaceInfo DetectNamespaceInfo(PageInfo currentPage) {
			if(currentPage == null) {
				return Tools.DetectCurrentNamespaceInfo();
			}
			else {
				string ns = NameTools.GetNamespace(currentPage.FullName);
				return Pages.FindNamespace(ns);
			}
		}

		/// <summary>
		/// Formats WikiMarkup, converting it into XHTML.
		/// </summary>
		/// <param name="raw">The raw WikiMarkup text.</param>
		/// <param name="forIndexing">A value indicating whether the formatting is being done for content indexing.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="current">The current Page (can be null).</param>
		/// <returns>The formatted text.</returns>
		public static string Format(string raw, bool forIndexing, FormattingContext context, PageInfo current) {
			string[] tempLinks;
			return Format(raw, forIndexing, context, current, out tempLinks);
		}

		/// <summary>
		/// Formats WikiMarkup, converting it into XHTML.
		/// </summary>
		/// <param name="raw">The raw WikiMarkup text.</param>
		/// <param name="forIndexing">A value indicating whether the formatting is being done for content indexing.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="current">The current Page (can be null).</param>
		/// <param name="linkedPages">The linked pages, both existent and inexistent.</param>
		/// <returns>The formatted text.</returns>
		public static string Format(string raw, bool forIndexing, FormattingContext context, PageInfo current, out string[] linkedPages) {
			return Format(raw, forIndexing, context, current, out linkedPages, false);
		}

		/// <summary>
		/// Formats WikiMarkup, converting it into XHTML.
		/// </summary>
		/// <param name="raw">The raw WikiMarkup text.</param>
		/// <param name="forIndexing">A value indicating whether the formatting is being done for content indexing.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="current">The current Page (can be null).</param>
		/// <param name="linkedPages">The linked pages, both existent and inexistent.</param>
		/// <param name="bareBones">A value indicating whether to format in bare-bones mode (for WYSIWYG editor).</param>
		/// <returns>The formatted text.</returns>
		public static string Format(string raw, bool forIndexing, FormattingContext context, PageInfo current, out string[] linkedPages, bool bareBones) {
			// Bare Bones: Advanced tags, such as tables, toc, special tags, etc. are not formatted - used for Visual editor display

			linkedPages = new string[0];
			List<string> tempLinkedPages = new List<string>(10);

			StringBuilder sb = new StringBuilder(raw);
			Match match;
			string tmp, a, n, url, title, bigUrl;
			StringBuilder dummy; // Used for temporary string manipulation inside formatting cycles
			bool done = false;
			List<int> noWikiBegin = new List<int>(), noWikiEnd = new List<int>();
			int end = 0;
			List<HPosition> hPos = new List<HPosition>();

			sb.Replace("\r", "");
			bool addedNewLineAtEnd = false;
			if(!sb.ToString().EndsWith("\n")) {
				sb.Append("\n"); // Very important to make Regular Expressions work!
				addedNewLineAtEnd = true;
			}

			// Remove all double- or single-LF in JavaScript tags
			bool singleLine = Settings.ProcessSingleLineBreaks;
			match = JavascriptRegex.Match(sb.ToString());
			while(match.Success) {
				sb.Remove(match.Index, match.Length);
				if(singleLine) sb.Insert(match.Index, match.Value.Replace("\n", ""));
				else sb.Insert(match.Index, match.Value.Replace("\n\n", "\n"));
				match = JavascriptRegex.Match(sb.ToString(), match.Index + 1);
			}

			// Remove empty NoWiki and NoBr tags
			sb.Replace("<nowiki></nowiki>", "");
			sb.Replace("<nobr></nobr>", "");

			ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

			// Before Producing HTML
			match = FullCodeRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					string content = match.Value.Substring(2, match.Length - 4);
					dummy = new StringBuilder();
					dummy.Append("<pre><nobr>");
					// IE needs \r\n for line breaks
					dummy.Append(EscapeWikiMarkup(content).Replace("\n", "\r\n"));
					dummy.Append("</nobr></pre>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = FullCodeRegex.Match(sb.ToString(), end);
			}

			if(current != null) {
				// Check redirection
				match = RedirectionRegex.Match(sb.ToString());
				if(match.Success) {
					if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
						sb.Remove(match.Index, match.Length);
						string destination = match.Value.Trim().Substring(4).Trim();
						while(destination.StartsWith("[") && destination.EndsWith("]")) {
							destination = destination.Substring(1, destination.Length - 2);
						}
						while(sb[match.Index] == '\n' && match.Index < sb.Length - 1) sb.Remove(match.Index, 1);

						if(!destination.StartsWith("++") && !destination.Contains(".") && current.FullName.Contains(".")) {
							// Adjust namespace
							destination = NameTools.GetFullName(NameTools.GetNamespace(current.FullName), destination);
						}

						destination = destination.Trim('+');

						PageInfo dest = Pages.FindPage(destination);
						if(dest != null) {
							Redirections.AddRedirection(current, dest);
						}
					}
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
			}

			// No more needed (Striked Regex modified)
			// Temporarily "escape" comments
			//sb.Replace("<!--", "($_^)");
			//sb.Replace("-->", "(^_$)");

			ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

			// Before Producing HTML
			if(!bareBones) {
				match = EscRegex.Match(sb.ToString());
				while(match.Success) {
					if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, match.Value.Substring(5, match.Length - 11).Replace("<", "&lt;").Replace(">", "&gt;"));
					}
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = EscRegex.Match(sb.ToString(), end);
				}
			}

			// Snippets and tables processing was here

			match = IndentRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, BuildIndent(match.Value) + "\n");
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = IndentRegex.Match(sb.ToString(), end);
			}

			// Process extended UP before standard UP
			match = ExtendedUpRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					// Encode filename only if it's used inside a link,
					// i.e. check if {UP} is used just after a '['
					// This works because links are processed afterwards
					string sbString = sb.ToString();
					if(match.Index > 0 && (sbString[match.Index - 1] == '[' || sbString[match.Index - 1] == '^')) {
						EncodeFilename(sb, match.Index + match.Length);
					}

					sb.Remove(match.Index, match.Length);
					string prov = match.Groups[1].Value.StartsWith(":") ? match.Value.Substring(4, match.Value.Length - 5) : match.Value.Substring(3, match.Value.Length - 4);
					string page = null;
					// prov - Full.Provider.Type.Name(PageName)
					// (PageName) is optional, but it can contain brackets, for example (Page(WithBrackets))
					if(prov.EndsWith(")") && prov.Contains("(")) {
						page = prov.Substring(prov.IndexOf("(") + 1);
						page = page.Substring(0, page.Length - 1);
						page = Tools.UrlEncode(page);
						prov = prov.Substring(0, prov.IndexOf("("));
					}
					if(page == null) {
						// Normal file
						sb.Insert(match.Index, ExtendedUpReplacement.Replace("$", (prov != "") ? "Provider=" + prov + "&" : ""));
					}
					else {
						// Page attachment
						sb.Insert(match.Index,
							ExtendedUpReplacementForAttachment.Replace("$", (prov != "")? "Provider=" + prov + "&" : "").Replace("@", page));
					}
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = ExtendedUpRegex.Match(sb.ToString(), end);
			}

			match = SpecialTagBRRegex.Match(sb.ToString()); // solved by introducing a new regex call SpecialTagBR
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					if(!forIndexing) {
						switch(match.Value.Substring(1, match.Value.Length - 2).ToUpperInvariant()) {
							case "BR":
								sb.Insert(match.Index, "<br />");
								break;
						}
					}
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = SpecialTagBRRegex.Match(sb.ToString(), end);
			}

			if(!bareBones) {
				NamespaceInfo ns = DetectNamespaceInfo(current);
				match = SpecialTagRegex.Match(sb.ToString());
				while(match.Success) {
					if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
						sb.Remove(match.Index, match.Length);
						if(!forIndexing) {
							switch(match.Value.Substring(1, match.Value.Length - 2).ToUpperInvariant()) {
								case "WIKITITLE":
									sb.Insert(match.Index, Settings.WikiTitle);
									break;
								case "WIKIVERSION":
									sb.Insert(match.Index, Settings.WikiVersion);
									break;
								case "MAINURL":
									sb.Insert(match.Index, Settings.MainUrl);
									break;
								case "RSSPAGE":
									if(current != null) {
										sb.Insert(match.Index, @"<a href=""" +
											UrlTools.BuildUrl("RSS.aspx?Page=", Tools.UrlEncode(current.FullName)) +
											@""" title=""" + Exchanger.ResourceExchanger.GetResource("RssForThisPage") + @"""><img src=""" +
											Settings.GetThemePath(Tools.DetectCurrentNamespace()) + @"Images/RSS.png"" alt=""RSS"" /></a>");
									}
									break;
								case "THEMEPATH":
									sb.Insert(match.Index, Settings.GetThemePath(Tools.DetectCurrentNamespace()));
									break;
								case "CLEAR":
									sb.Insert(match.Index, @"<div style=""clear: both;""></div>");
									break;
								case "TOP":
									sb.Insert(match.Index, @"<a href=""#PageTop"">" + Exchanger.ResourceExchanger.GetResource("Top") + "</a>");
									break;
								case "SEARCHBOX":
									string textBoxId = "SB" + Guid.NewGuid().ToString("N");

									string nsstring = ns != null ? NameTools.GetFullName(ns.Name, "Search") + ".aspx" : "Search.aspx";
									string doSearchFunction = "<nowiki><nobr><script type=\"text/javascript\"><!--\r\n" + @"function _DoSearch_" + textBoxId + "() { document.location = '" + nsstring + @"?AllNamespaces=1&FilesAndAttachments=1&Query=' + encodeURI(document.getElementById('" + textBoxId + "').value); }" + "\r\n// -->\r\n</script>";
									sb.Insert(match.Index, doSearchFunction +
										@"<input class=""txtsearchbox"" type=""text"" id=""" + textBoxId + @""" onkeydown=""javascript:var keycode; if(window.event) keycode = event.keyCode; else keycode = event.which; if(keycode == 10 || keycode == 13) { _DoSearch_" + textBoxId + @"(); return false; }"" /> <big><a href=""#"" onclick=""javascript:_DoSearch_" + textBoxId + @"(); return false;"">&raquo;</a></big></nowiki></nobr>");
									break;
								case "CATEGORIES":
									List<CategoryInfo> cats = Pages.GetCategories(ns);
									string pageName = ns != null ? NameTools.GetFullName(ns.Name, "AllPages") + ".aspx" : "AllPages.aspx";
									pageName += "?Cat=";
									string categories = "<ul><li>" + string.Join("</li><li>",
										(from c in cats
										 select "<a href=\"" + pageName + Tools.UrlEncode(c.FullName) + "\">" + NameTools.GetLocalName(c.FullName) + "</a>").ToArray()) + "</li></ul>";
									sb.Insert(match.Index, categories);
									break;
								case "CLOUD":
									string cloud = BuildCloud(DetectNamespaceInfo(current));
									sb.Insert(match.Index, cloud);
									break;
								case "PAGECOUNT":
									sb.Insert(match.Index, Pages.GetPages(DetectNamespaceInfo(current)).Count.ToString());
									break;
								case "PAGECOUNT(*)":
									sb.Insert(match.Index, Pages.GetGlobalPageCount().ToString());
									break;
								case "ORPHANS":
									sb.Insert(match.Index, BuildOrphanedPagesList(DetectNamespaceInfo(current), context, current));
									break;
								case "WANTED":
									sb.Insert(match.Index, BuildWantedPagesList(DetectNamespaceInfo(current)));
									break;
								case "NAMESPACELIST":
									sb.Insert(match.Index, BuildNamespaceList());
									break;
							}
						}
					}
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = SpecialTagRegex.Match(sb.ToString(), end);
				}
			}

			match = ListRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					int d = 0;
					try {
						string[] lines = match.Value.Split('\n');
						// Inline multi-line list elements
						List<string> tempLines = new List<string>(lines);
						for(int i = tempLines.Count - 1; i >= 1; i--) { // Skip first line
							string trimmedLine = tempLines[i].Trim();
							if(!trimmedLine.StartsWith("*") && !trimmedLine.StartsWith("#")) {
								//if(i != tempLines.Count - 1 && tempLines[i].Length > 0) {
									trimmedLine = "<br />" + trimmedLine;
									tempLines[i - 1] += trimmedLine;
								//}
								tempLines.RemoveAt(i);
							}
						}
						lines = tempLines.ToArray();

						sb.Insert(match.Index, GenerateList(lines, 0, 0, ref d) + "\n");
					}
					catch {
						if(!bareBones) {
							sb.Insert(match.Index, @"<b style=""color: #FF0000;"">FORMATTER ERROR (Malformed List)</b><br />");
						}
						else {
							sb.Insert(match.Index, match);
							end = match.Index + match.Length;
						}
					}
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = ListRegex.Match(sb.ToString(), end);
			}

			match = HRRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, @"<h1 class=""separator""> </h1>" + "\n");
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = HRRegex.Match(sb.ToString(), end);
			}

			// Replace \n with BR was here

			ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

			List<string> attachments = new List<string>();

			// Links and images
			match = LinkRegex.Match(sb.ToString());
			while(match.Success) {
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					match = LinkRegex.Match(sb.ToString(), end);
					continue;
				}

				// [], [[]] and [[] can occur when empty links are processed
				if(match.Value.Equals("[]") || match.Value.Equals("[[]]") || match.Value.Equals("[[]")) {
					sb.Remove(match.Index, match.Length);
					match = LinkRegex.Match(sb.ToString(), end);
					continue; // Prevents formatting emtpy links
				}

				done = false;
				if(match.Value.StartsWith("[[")) tmp = match.Value.Substring(2, match.Length - 4).Trim();
				else tmp = match.Value.Substring(1, match.Length - 2).Trim();
				sb.Remove(match.Index, match.Length);
				a = "";
				n = "";
				if(tmp.IndexOf("|") != -1) {
					// There are some fields
					string[] fields = tmp.Split('|');
					if(fields.Length == 2) {
						// Link with title
						a = fields[0];
						n = fields[1];
					}
					else {
						done = true;
						StringBuilder img = new StringBuilder();
						// Image
						if(fields[0].ToLowerInvariant().Equals("imageleft") || fields[0].ToLowerInvariant().Equals("imageright") || fields[0].ToLowerInvariant().Equals("imageauto")) {
							string c = "";
							switch(fields[0].ToLowerInvariant()) {
								case "imageleft":
									c = "imageleft";
									break;
								case "imageright":
									c = "imageright";
									break;
								case "imageauto":
									c = "imageauto";
									break;
								default:
									c = "image";
									break;
							}
							title = fields[1];
							url = fields[2];
							if(fields.Length == 4) bigUrl = fields[3];
							else bigUrl = "";
							url = EscapeUrl(url);
							// bigUrl = EscapeUrl(bigUrl); The url is already escaped by BuildUrl
							if(c.Equals("imageauto")) {
								img.Append(@"<table class=""imageauto"" cellpadding=""0"" cellspacing=""0""><tr><td>");
							}
							else {
								img.Append(@"<div class=""");
								img.Append(c);
								img.Append(@""">");
							}
							if(bigUrl.Length > 0) {
								dummy = new StringBuilder(200);
								dummy.Append(@"<img class=""image"" src=""");
								dummy.Append(url);
								dummy.Append(@""" alt=""");
								if(title.Length > 0) dummy.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								else dummy.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								dummy.Append(@""" />");
								img.Append(BuildLink(bigUrl, dummy.ToString(), true, title, forIndexing, bareBones, context, null, tempLinkedPages));
							}
							else {
								img.Append(@"<img class=""image"" src=""");
								img.Append(url);
								img.Append(@""" alt=""");
								if(title.Length > 0) img.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								else img.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								img.Append(@""" />");
							}
							if(title.Length > 0 && !title.StartsWith("#")) {
								img.Append(@"<p class=""imagedescription"">");
								img.Append(title);
								img.Append("</p>");
							}
							if(c.Equals("imageauto")) {
								img.Append("</td></tr></table>");
							}
							else {
								img.Append("</div>");
							}
							sb.Insert(match.Index, img);
						}
						else if(fields[0].ToLowerInvariant().Equals("image")) {
							title = fields[1];
							url = fields[2];
							if(fields.Length == 4) bigUrl = fields[3];
							else bigUrl = "";
							url = EscapeUrl(url);
							// bigUrl = EscapeUrl(bigUrl); The url is already escaped by BuildUrl
							if(bigUrl.Length > 0) {
								dummy = new StringBuilder();
								dummy.Append(@"<img src=""");
								dummy.Append(url);
								dummy.Append(@""" alt=""");
								if(title.Length > 0) dummy.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								else dummy.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								dummy.Append(@""" />");
								img.Append(BuildLink(bigUrl, dummy.ToString(), true, title, forIndexing, bareBones, context, null, tempLinkedPages));
							}
							else {
								img.Append(@"<img src=""");
								img.Append(url);
								img.Append(@""" alt=""");
								if(title.Length > 0) img.Append(StripWikiMarkup(StripHtml(title.TrimStart('#'))));
								else img.Append(Exchanger.ResourceExchanger.GetResource("Image"));
								img.Append(@""" />");
							}
							sb.Insert(match.Index, img.ToString());
						}
						else {
							if(!bareBones) sb.Insert(match.Index, @"<b style=""color: #FF0000;"">FORMATTER ERROR (Malformed Image Tag)</b>");
							else {
								sb.Insert(match.Index, match);
								end = match.Index + match.Length;
							}
							done = true;
						}
					}
				}
				else if(tmp.ToLowerInvariant().StartsWith("attachment:")) {
					// This is an attachment
					done = true;
					string f = tmp.Substring("attachment:".Length);
					if(f.StartsWith("{up}")) f = f.Substring(4);
					if(f.ToLowerInvariant().StartsWith(UpReplacement.ToLowerInvariant())) f = f.Substring(UpReplacement.Length);
					attachments.Add(HttpContext.Current.Server.UrlDecode(f));
					// Remove all trailing \n, so that attachments have no effect on the output in any case
					while(sb[match.Index] == '\n' && match.Index < sb.Length - 1) {
						sb.Remove(match.Index, 1);
					}
				}
				else {
					a = tmp;
					n = "";
				}
				if(!done) {
					sb.Insert(match.Index, BuildLink(a, n, false, "", forIndexing, bareBones, context, current, tempLinkedPages));
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = LinkRegex.Match(sb.ToString(), end);
			}

			match = BoldItalicRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<b><i>");
					dummy.Append(match.Value.Substring(5, match.Value.Length - 10));
					dummy.Append("</i></b>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = BoldItalicRegex.Match(sb.ToString(), end);
			}

			match = BoldRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<b>");
					dummy.Append(match.Value.Substring(3, match.Value.Length - 6));
					dummy.Append("</b>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = BoldRegex.Match(sb.ToString(), end);
			}

			match = ItalicRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<i>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</i>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = ItalicRegex.Match(sb.ToString(), end);
			}

			match = UnderlinedRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<u>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</u>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = UnderlinedRegex.Match(sb.ToString(), end);
			}

			match = ApexRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiBegin, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<sup>");
					dummy.Append(match.Value.Substring(11, match.Value.Length - 23));
					dummy.Append("</sup>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = ApexRegex.Match(sb.ToString(), end);
			}

			match = SubscribeRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiBegin, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<sub>");
					dummy.Append(match.Value.Substring(11, match.Value.Length - 23));
					dummy.Append("</sub>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = SubscribeRegex.Match(sb.ToString(), end);
			}

			match = StrikedRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<strike>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</strike>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = StrikedRegex.Match(sb.ToString(), end);
			}

			match = PreRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<pre>");
					// IE needs \r\n for line breaks
					dummy.Append(match.Value.Substring(4, match.Value.Length - 8).Replace("\n", "\r\n"));
					dummy.Append("</pre>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = PreRegex.Match(sb.ToString(), end);
			}

			match = CodeRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder("<code>");
					dummy.Append(match.Value.Substring(2, match.Value.Length - 4));
					dummy.Append("</code>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = CodeRegex.Match(sb.ToString(), end);
			}

			string h;

			// Hx: detection pass (used for the TOC generation and section editing)
			hPos = DetectHeaders(sb.ToString());

			// Hx: formatting pass

			int count = 0;

			match = H4Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(5, match.Value.Length - 10 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder(200);
					dummy.Append(@"<h4 class=""separator"">");
					dummy.Append(h);
					if(!bareBones && !forIndexing) {
						string id = BuildHAnchor(h, count.ToString());
						BuildHeaderAnchor(dummy, id);
					}
					dummy.Append("</h4>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H4Regex.Match(sb.ToString(), end);
			}

			match = H3Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(4, match.Value.Length - 8 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder(200);
					if(current != null && !bareBones && !forIndexing) dummy.Append(BuildEditSectionLink(count, current.FullName));
					dummy.Append(@"<h3 class=""separator"">");
					dummy.Append(h);
					if(!bareBones && !forIndexing) {
						string id = BuildHAnchor(h, count.ToString());
						BuildHeaderAnchor(dummy, id);
					}
					dummy.Append("</h3>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H3Regex.Match(sb.ToString(), end);
			}

			match = H2Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(3, match.Value.Length - 6 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder(200);
					if(current != null && !bareBones && !forIndexing) dummy.Append(BuildEditSectionLink(count, current.FullName));
					dummy.Append(@"<h2 class=""separator"">");
					dummy.Append(h);
					if(!bareBones && !forIndexing) {
						string id = BuildHAnchor(h, count.ToString());
						BuildHeaderAnchor(dummy, id);
					}
					dummy.Append("</h2>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H2Regex.Match(sb.ToString(), end);
			}

			match = H1Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					h = match.Value.Substring(2, match.Value.Length - 4 - (match.Value.EndsWith("\n") ? 1 : 0));
					dummy = new StringBuilder(200);
					if(current != null && !bareBones && !forIndexing) dummy.Append(BuildEditSectionLink(count, current.FullName));
					dummy.Append(@"<h1 class=""separator"">");
					dummy.Append(h);
					if(!bareBones && !forIndexing) {
						string id = BuildHAnchor(h, count.ToString());
						BuildHeaderAnchor(dummy, id);
					}
					dummy.Append("</h1>");
					sb.Insert(match.Index, dummy.ToString());
					count++;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H1Regex.Match(sb.ToString(), end);
			}

			match = BoxRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					dummy = new StringBuilder(@"<div class=""box"">");
					dummy.Append(match.Value.Substring(3, match.Value.Length - 6));
					dummy.Append("</div>");
					sb.Insert(match.Index, dummy.ToString());
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = BoxRegex.Match(sb.ToString(), end);
			}

			string tocString = BuildToc(hPos);

			if(!bareBones && current != null) {
				match = TocRegex.Match(sb.ToString());
				while(match.Success) {
					if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
						sb.Remove(match.Index, match.Length);
						if(!forIndexing) sb.Insert(match.Index, tocString);
					}
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = TocRegex.Match(sb.ToString(), end);
				}
			}

			if(!bareBones) {
				match = SnippetRegex.Match(sb.ToString());
				while(match.Success) {
					if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
						string balanced = null;
						try {
							// If the snippet is malformed this can explode
							balanced = ExpandToBalanceBrackets(sb, match.Index, match.Value);
						}
						catch { }
						
						if(balanced == null) {
							// Replace brackets with escaped values so that the snippets regex does not trigger anymore
							sb.Replace("{", "&#123;", match.Index, match.Length);
							sb.Replace("}", "&#125;", match.Index, match.Length);
							break; // Give up
						}
						else {
							sb.Remove(match.Index, balanced.Length);
						}

						if(balanced.IndexOf("}") == balanced.Length - 1) {
							// Single-level snippet
							string[] temp = null;
							sb.Insert(match.Index,
								Format(FormatSnippet(balanced, tocString), forIndexing, context, current, out temp, bareBones).Trim('\n'));
							if(temp != null) tempLinkedPages.AddRange(temp);
						}
						else {
							// Nested snippet

							int lastOpen = 0;
							int firstClosedAfterLastOpen = 0;

							do {
								lastOpen = balanced.LastIndexOf("{");
								firstClosedAfterLastOpen = balanced.IndexOf("}", lastOpen + 1);

								if(lastOpen < 0 || firstClosedAfterLastOpen <= lastOpen) break; // Give up
								
								string internalSnippet = balanced.Substring(lastOpen, firstClosedAfterLastOpen - lastOpen + 1);
								balanced = balanced.Remove(lastOpen, firstClosedAfterLastOpen - lastOpen + 1);

								// This check allows to ignore special tags (especially Phase3)
								if(!internalSnippet.ToLowerInvariant().StartsWith("{s:")) {
									internalSnippet = internalSnippet.Replace("{", "$$$$$$$$OPEN$$$$$$$$").Replace("}", "$$$$$$$$CLOSE$$$$$$$$");
									balanced = balanced.Insert(lastOpen, internalSnippet);
									continue;
								}

								string formattedInternalSnippet = FormatSnippet(internalSnippet, tocString);
								string[] temp;
								formattedInternalSnippet = Format(formattedInternalSnippet, forIndexing, context, current, out temp, bareBones).Trim('\n');
								if(temp != null) tempLinkedPages.AddRange(temp);

								balanced = balanced.Insert(lastOpen, formattedInternalSnippet);
							} while(lastOpen != -1);

							sb.Insert(match.Index, balanced.Replace("$$$$$$$$OPEN$$$$$$$$", "{").Replace("$$$$$$$$CLOSE$$$$$$$$", "}"));
						}
					}
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = SnippetRegex.Match(sb.ToString(), end);
				}
			}

			match = TableRegex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, BuildTable(match.Value));
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = TableRegex.Match(sb.ToString(), end);
			}

			// Strip out all comments
			if(!bareBones) {
				match = CommentRegex.Match(sb.ToString());
				while(match.Success) {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, " "); // This prevents the creation of additional blank lines
					match = CommentRegex.Match(sb.ToString(), match.Index + 1);
				}
			}

			// Remove <nowiki> tags
			if(!bareBones) {
				sb.Replace("<nowiki>", "");
				sb.Replace("</nowiki>", "");
			}

			ProcessLineBreaks(sb, bareBones);

			if(addedNewLineAtEnd) {
				if(sb.ToString().EndsWith("<br />")) sb.Remove(sb.Length - 6, 6);
			}

			// Append Attachments
			if(!bareBones && attachments.Count > 0) {
				sb.Append(@"<div id=""AttachmentsDiv"">");
				for(int i = 0; i < attachments.Count; i++) {
					sb.Append(@"<a href=""");
					sb.Append(UpReplacement);
					sb.Append(Tools.UrlEncode(attachments[i]));
					sb.Append(@""" class=""attachment"">");
					sb.Append(attachments[i]);
					sb.Append("</a>");
					if(i != attachments.Count - 1) sb.Append(" - ");
				}
				sb.Append("</div>");
			}

			linkedPages = tempLinkedPages.ToArray();

			return sb.ToString();
		}

		/// <summary>
		/// Encodes a filename used in combination with {UP} tags.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="startIndex">The index where to start working.</param>
		private static void EncodeFilename(StringBuilder buffer, int startIndex) {
			// 1. Find end of the filename (first pipe or closed square bracket)
			// 2. Decode the string, so that it does not break if it was already encoded
			// 3. Encode the string

			string allData = buffer.ToString();

			int endIndex = allData.IndexOfAny(new[] { '|', ']' }, startIndex);
			if(endIndex > startIndex) {
				int len = endIndex - startIndex;
				// {, : and } are used in snippets which are useful in links
				string input = Tools.UrlDecode(allData.Substring(startIndex, len));
				string value = Tools.UrlEncode(input).Replace("%7b", "{").Replace("%7B", "{").Replace("%7d", "}").Replace("%7D", "}").Replace("%3a", ":").Replace("%3A", ":");
				buffer.Remove(startIndex, len);
				buffer.Insert(startIndex, value);
			}
		}

		/// <summary>
		/// Builds the anchor markup for a header.
		/// </summary>
		/// <param name="buffer">The string builder.</param>
		/// <param name="id">The anchor ID.</param>
		private static void BuildHeaderAnchor(StringBuilder buffer, string id) {
			buffer.Append(@"<a class=""headeranchor"" id=""");
			buffer.Append(id);
			buffer.Append(@""" href=""#");
			buffer.Append(id);
			buffer.Append(@""" title=""");
			buffer.Append(SectionLinkTextPlaceHolder);
			if(Settings.EnableSectionAnchors) buffer.Append(@""">&#0182;</a>");
			else buffer.Append(@""" style=""visibility: hidden;"">&nbsp;</a>");
		}

		/// <summary>
		/// Builds the recent changes list.
		/// </summary>
		/// <param name="allNamespaces">A value indicating whether to build a list for all namespace or for just the current one.</param>
		/// <param name="currentNamespace">The current namespace.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPage">The current page, or <c>null</c>.</param>
		/// <returns>The recent changes list HTML markup.</returns>
		private static string BuildRecentChanges(NamespaceInfo currentNamespace, bool allNamespaces, FormattingContext context, PageInfo currentPage) {
			List<RecentChange> allChanges = new List<RecentChange>(RecentChanges.GetAllChanges());

			if(allChanges.Count == 0) return "";

			// Sort by descending date/time
			allChanges.Reverse();

			Func<NamespaceInfo, string> getName = (ns) => {
				if(ns == null) return null;
				else return ns.Name;
			};

			string currentNamespaceName = getName(currentNamespace);

			// Filter by namespace
			if(!allNamespaces) {
				allChanges.RemoveAll((c) => {
					NamespaceInfo ns = Pages.FindNamespace(NameTools.GetNamespace(c.Page));
					return getName(ns) != currentNamespaceName;
				});
			}

			return BuildRecentChangesTable(allChanges, context, currentPage);
		}

		/// <summary>
		/// Builds a table containing recent changes.
		/// </summary>
		/// <param name="allChanges">The changes.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPage">The current page, or <c>null</c>.</param>
		/// <returns>The table HTML.</returns>
		public static string BuildRecentChangesTable(IList<RecentChange> allChanges, FormattingContext context, PageInfo currentPage) {
			int maxChanges = Math.Min(Settings.MaxRecentChangesToDisplay, allChanges.Count);

			StringBuilder sb = new StringBuilder(500);
			sb.Append("<table cellpadding=\"0\" cellspacing=\"0\" class=\"generictable recentchanges\"><tbody>");

			for(int i = 0; i < maxChanges; i++) {
				sb.AppendFormat("<tr class=\"{0}\">", i % 2 == 0 ? "tablerow" : "tablerowalternate");
				sb.Append("<td>");
				sb.Append(Preferences.AlignWithTimezone(allChanges[i].DateTime).ToString(Settings.DateTimeFormat));
				sb.Append("</td><td>");
				sb.Append(PrintRecentChange(allChanges[i], context, currentPage));
				sb.Append("</td>");
				sb.Append("</tr>");
			}

			sb.Append("</tbody></table>");

			return sb.ToString();
		}

		/// <summary>
		/// Prints a recent change.
		/// </summary>
		/// <param name="change">The change.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPage">The current page, or <c>null</c>.</param>
		/// <returns>The proper text to display.</returns>
		private static string PrintRecentChange(RecentChange change, FormattingContext context, PageInfo currentPage) {
			switch(change.Change) {
				case Change.PageUpdated:
					return Exchanger.ResourceExchanger.GetResource("UserUpdatedPage").Replace("##USER##", Users.UserLink(change.User)).Replace("##PAGE##", PrintPageLink(change, context, currentPage));
				case Change.PageRenamed:
					return Exchanger.ResourceExchanger.GetResource("UserRenamedPage").Replace("##USER##", Users.UserLink(change.User)).Replace("##PAGE##", PrintPageLink(change, context, currentPage));
				case Change.PageRolledBack:
					return Exchanger.ResourceExchanger.GetResource("UserRolledBackPage").Replace("##USER##", Users.UserLink(change.User)).Replace("##PAGE##", PrintPageLink(change, context, currentPage));
				case Change.PageDeleted:
					return Exchanger.ResourceExchanger.GetResource("UserDeletedPage").Replace("##USER##", Users.UserLink(change.User)).Replace("##PAGE##", PrintPageLink(change, context, currentPage));
				case Change.MessagePosted:
					return Exchanger.ResourceExchanger.GetResource("UserPostedMessage").Replace("##USER##", Users.UserLink(change.User)).Replace("##PAGE##", PrintMessageLink(change, context, currentPage));
				case Change.MessageEdited:
					return Exchanger.ResourceExchanger.GetResource("UserEditedMessage").Replace("##USER##", Users.UserLink(change.User)).Replace("##PAGE##", PrintMessageLink(change, context, currentPage));
				case Change.MessageDeleted:
					return Exchanger.ResourceExchanger.GetResource("UserDeletedMessage").Replace("##USER##", Users.UserLink(change.User)).Replace("##PAGE##", PrintMessageLink(change, context, currentPage));
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Builds a link to a page, properly handling inexistent pages.
		/// </summary>
		/// <param name="change">The change.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPage">The current page, or <c>null</c>.</param>
		/// <returns>The link HTML markup.</returns>
		private static string PrintPageLink(RecentChange change, FormattingContext context, PageInfo currentPage) {
			PageInfo page = Pages.FindPage(change.Page);
			if(page != null) {
				return string.Format(@"<a href=""{0}{1}"" class=""pagelink"">{2}</a>",
					change.Page, Settings.PageExtension, FormattingPipeline.PrepareTitle(change.Title, false, context, currentPage));
			}
			else {
				return FormattingPipeline.PrepareTitle(change.Title, false, context, currentPage) + " (" + change.Page + ")";
			}
		}

		/// <summary>
		/// Builds a link to a page discussion.
		/// </summary>
		/// <param name="change">The change.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPage">The current page, or <c>null</c>.</param>
		/// <returns>The link HTML markup.</returns>
		private static string PrintMessageLink(RecentChange change, FormattingContext context, PageInfo currentPage) {
			PageInfo page = Pages.FindPage(change.Page);
			if(page != null) {
				return string.Format(@"<a href=""{0}{1}?Discuss=1#{2}"" class=""pagelink"">{3}</a>",
					change.Page, Settings.PageExtension, Tools.GetMessageIdForAnchor(change.DateTime),
					FormattingPipeline.PrepareTitle(change.Title, false, context, currentPage) + " (" +
					FormattingPipeline.PrepareTitle(change.MessageSubject, false, context, currentPage) + ")");
			}
			else {
				return FormattingPipeline.PrepareTitle(change.Title, false, context, currentPage) + " (" + change.Page + ")";
			}
		}

		/// <summary>
		/// Builds the orhpaned pages list (valid only in non-indexing mode).
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="current">The current page, if any.</param>
		/// <returns>The list.</returns>
		private static string BuildOrphanedPagesList(NamespaceInfo nspace, FormattingContext context, PageInfo current) {
			PageInfo[] orhpans = Pages.GetOrphanedPages(nspace);

			if(orhpans.Length == 0) return "";

			StringBuilder sb = new StringBuilder(500);
			sb.Append("<ul>");

			foreach(PageInfo page in orhpans) {
				PageContent content = Content.GetPageContent(page, false);
				sb.Append("<li>");
				sb.AppendFormat(@"<a href=""{0}{1}"" title=""{2}"" class=""pagelink"">{2}</a>", Tools.UrlEncode(page.FullName), Settings.PageExtension,
					FormattingPipeline.PrepareTitle(content.Title, false, context, current));
				sb.Append("</li>");
			}

			sb.Append("</ul>");

			return sb.ToString();
		}

		/// <summary>
		/// Builds the wanted pages list.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The list.</returns>
		private static string BuildWantedPagesList(NamespaceInfo nspace) {
			Dictionary<string, List<string>> wanted = Pages.GetWantedPages(nspace != null ? nspace.Name : null);

			if(wanted.Count == 0) return "";

			StringBuilder sb = new StringBuilder(500);
			sb.Append("<ul>");

			foreach(string page in wanted.Keys) {
				sb.Append("<li>");
				sb.AppendFormat(@"<a href=""{0}{1}"" title=""{0}"" class=""unknownlink"">{2}</a>", page, Settings.PageExtension, NameTools.GetLocalName(page));
				sb.Append("</li>");
			}

			sb.Append("</ul>");

			return sb.ToString();
		}

		/// <summary>
		/// Builds the incoming links list for a page (valid only in Phase3).
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="current">The current page, if any.</param>
		/// <returns>The list.</returns>
		private static string BuildIncomingLinksList(PageInfo page, FormattingContext context, PageInfo current) {
			if(page == null) return "";

			string[] links = Pages.GetPageIncomingLinks(page);
			if(links.Length == 0) return "";

			StringBuilder sb = new StringBuilder(500);
			sb.AppendFormat("<ul>");

			foreach(string link in links) {
				PageInfo linkedPage = Pages.FindPage(link);
				if(linkedPage != null) {
					PageContent content = Content.GetPageContent(linkedPage, false);

					sb.Append("<li>");
					sb.AppendFormat(@"<a href=""{0}{1}"" title=""{2}"" class=""pagelink"">{2}</a>", Tools.UrlEncode(link), Settings.PageExtension,
						FormattingPipeline.PrepareTitle(content.Title, false, context, current));
					sb.Append("</li>");
				}
			}

			sb.AppendFormat("</ul>");

			return sb.ToString();
		}

		/// <summary>
		/// Builds the outgoing links list for a page (valid only in Phase3).
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="current">The current page, if any.</param>
		/// <returns>The list.</returns>
		private static string BuildOutgoingLinksList(PageInfo page, FormattingContext context, PageInfo current) {
			if(page == null) return "";

			string[] links = Pages.GetPageOutgoingLinks(page);
			if(links.Length == 0) return "";

			StringBuilder sb = new StringBuilder(500);
			sb.Append("<ul>");

			foreach(string link in links) {
				PageInfo linkedPage = Pages.FindPage(link);
				if(linkedPage != null) {
					PageContent content = Content.GetPageContent(linkedPage, false);

					sb.Append("<li>");
					sb.AppendFormat(@"<a href=""{0}{1}"" title=""{2}"" class=""pagelink"">{2}</a>", Tools.UrlEncode(link), Settings.PageExtension,
						FormattingPipeline.PrepareTitle(content.Title, false, context, current));
					sb.Append("</li>");
				}
			}

			sb.Append("</ul>");

			return sb.ToString();
		}

		/// <summary>
		/// Builds the link to a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The link.</returns>
		private static string BuildNamespaceLink(string nspace) {
			return "<a href=\"" + (string.IsNullOrEmpty(nspace) ? "" : Tools.UrlEncode(nspace) + ".") +
				"Default.aspx\" class=\"pagelink\" title=\"" + (string.IsNullOrEmpty(nspace) ? "" : Tools.UrlEncode(nspace)) + "\">" +
				(string.IsNullOrEmpty(nspace) ? "&lt;root&gt;" : nspace) + "</a>";
		}

		/// <summary>
		/// Builds the namespace list.
		/// </summary>
		/// <returns>The namespace list.</returns>
		private static string BuildNamespaceList() {
			StringBuilder sb = new StringBuilder(100);

			sb.Append("<ul>");
			sb.Append("<li>");
			sb.Append(BuildNamespaceLink(null));
			sb.Append("</li>");
			foreach(NamespaceInfo ns in Pages.GetNamespaces()) {
				sb.Append("<li>");
				sb.Append(BuildNamespaceLink(ns.Name));
				sb.Append("</li>");
			}

			sb.Append("</ul>");

			return sb.ToString();
		}

		/// <summary>
		/// Processes line breaks.
		/// </summary>
		/// <param name="sb">The <see cref="T:StringBuilder" /> containing the text to process.</param>
		/// <param name="bareBones">A value indicating whether the formatting is being done in bare-bones mode.</param>
		private static void ProcessLineBreaks(StringBuilder sb, bool bareBones) {
			if(AreSingleLineBreaksToBeProcessed()) {
				// Replace new-lines only when not enclosed in <nobr> tags
				Match match = NoSingleBr.Match(sb.ToString());
				while(match.Success) {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, match.Value.Replace("\n", SingleBrPlaceHolder));
					//sb.Insert(match.Index, match.Value.Replace("\n", "<br />"));

					match = NoSingleBr.Match(sb.ToString(), match.Index + 1);
				}

				sb.Replace("\n", "<br />");

				sb.Replace(SingleBrPlaceHolder, "\n");
				//sb.Replace(SingleBrPlaceHolder, "<br />");
			}
			else {
				// Replace new-lines only when not enclosed in <nobr> tags

				Match match = NoSingleBr.Match(sb.ToString());
				while(match.Success) {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, match.Value.Replace("\n", SingleBrPlaceHolder));
					//sb.Insert(match.Index, match.Value.Replace("\n", "<br />"));
					match = NoSingleBr.Match(sb.ToString(), match.Index + 1);
				}

				sb.Replace("\n\n", "<br /><br />");

				sb.Replace(SingleBrPlaceHolder, "\n");//Replace <br /><br /> with <br />

			}

			sb.Replace("<br>", "<br />");

			// BR Hacks
			sb.Replace("</ul><br /><br />", "</ul><br />");
			sb.Replace("</ol><br /><br />", "</ol><br />");
			sb.Replace("</table><br /><br />", "</table><br />");
			sb.Replace("</pre><br /><br />", "</pre><br />");
			if(AreSingleLineBreaksToBeProcessed()) {
				sb.Replace("</h1><br />", "</h1>");
				sb.Replace("</h2><br />", "</h2>");
				sb.Replace("</h3><br />", "</h3>");
				sb.Replace("</h4><br />", "</h4>");
				sb.Replace("</h5><br />", "</h5>");
				sb.Replace("</h6><br />", "</h6>");
				sb.Replace("</div><br />", "</div>");
			}
			else {
				sb.Replace("</div><br /><br />", "</div><br />");
			}

			sb.Replace("<nobr>", "");
			sb.Replace("</nobr>", "");
		}

		/// <summary>
		/// Gets a value indicating whether or not to process single line breaks.
		/// </summary>
		/// <returns><c>true</c> if SLB are to be processed, <c>false</c> otherwise.</returns>
		private static bool AreSingleLineBreaksToBeProcessed() {
			return Settings.ProcessSingleLineBreaks;
		}

		/// <summary>
		/// Replaces the {TOC} markup in a string.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="cachedToc">The TOC replacement.</param>
		/// <returns>The final string.</returns>
		private static string ReplaceToc(string input, string cachedToc) {
			// HACK: this method is used to trick the formatter so that it works when a {TOC} tag is placed in a trigger
			// Basically, the Snippet content is formatted in the same context as the main content, but without
			// The headers list available, thus the need for this special treatment

			Match match = TocRegex.Match(input);
			while(match.Success) {
				input = input.Remove(match.Index, match.Length);
				input = input.Insert(match.Index, cachedToc);
				match = TocRegex.Match(input, match.Index + 1);
			}

			return input;
		}

		/// <summary>
		/// Expands a regex selection to match the number of open curly brackets.
		/// </summary>
		/// <param name="sb">The buffer.</param>
		/// <param name="index">The match start index.</param>
		/// <param name="value">The match value.</param>
		/// <returns>The balanced string, or <c>null</c> if the brackets could not be balanced.</returns>
		private static string ExpandToBalanceBrackets(StringBuilder sb, int index, string value) {
			int tempIndex = -1;

			int openCount = 0;
			do {
				tempIndex = value.IndexOf("{", tempIndex + 1);
				if(tempIndex >= 0) openCount++;
			} while(tempIndex != -1 && tempIndex < value.Length - 1);

			int closeCount = 0;
			tempIndex = -1;
			do {
				tempIndex = value.IndexOf("}", tempIndex + 1);
				if(tempIndex >= 0) closeCount++;
			} while(tempIndex != -1 && tempIndex < value.Length - 1);

			// Already balanced
			if(openCount == closeCount) return value;

			tempIndex = index + value.Length - 1;

			string bigString = sb.ToString();

			do {
				int dummy = bigString.IndexOf("{", tempIndex + 1);
				if(dummy != -1) openCount++;
				tempIndex = bigString.IndexOf("}", tempIndex + 1);
				if(tempIndex != -1) closeCount++;

				tempIndex = Math.Max(dummy, tempIndex);

				if(closeCount == openCount) {
					// Balanced
					return bigString.Substring(index, tempIndex - index + 1);
				}
			} while(tempIndex != -1 && tempIndex < bigString.Length - 1);

			return null;
		}

		/// <summary>
		/// Formats a snippet.
		/// </summary>
		/// <param name="capturedMarkup">The captured markup.</param>
		/// <param name="cachedToc">The TOC content (trick to allow {TOC} to be inserted in snippets).</param>
		/// <returns>The formatted result.</returns>
		private static string FormatSnippet(string capturedMarkup, string cachedToc) {
			// If the markup does not contain equal signs, process it using the classic method, assuming there are only positional parameters
			//if(capturedMarkup.IndexOf("=") == -1) {
			if(!ClassicSnippetVerifier.IsMatch(capturedMarkup)) {
				string tempRes = FormatClassicSnippet(capturedMarkup);
				return ReplaceToc(tempRes, cachedToc);
			}
			// If the markup contains "=" but not new lines, simulate the required structure as shown below
			if(capturedMarkup.IndexOf("\n") == -1) {
				capturedMarkup = capturedMarkup.Replace("|", "\n|");
			}

			// The format is:
			// {s:Name | param = value			-- OR
			// | param = value					-- OR
			// | param = value
			//
			// which continues on next line, preceded by a blank
			// }

			// End bracket can be on a line on its own or on the last content line

			// 0. Find snippet object
			string snippetName = capturedMarkup.Substring(3, capturedMarkup.IndexOf("\n") - 3).Trim();
			Snippet snippet = Snippets.Find(snippetName);
			if(snippet == null) return @"<b style=""color: #FF0000;"">FORMATTER ERROR (Snippet Not Found)</b>";

			// 1. Strip all useless data at the beginning and end of the text ({s| and }, plus all whitespaces)

			// Strip opening and closing tags
			StringBuilder sb = new StringBuilder(capturedMarkup);
			sb.Remove(0, 3);
			sb.Remove(sb.Length - 1, 1);
			
			// Strip all whitespaces at the end
			while(char.IsWhiteSpace(sb[sb.Length - 1])) sb.Remove(sb.Length - 1, 1);

			// 2. Split into lines, preserving empty lines
			string[] lines = sb.ToString().Split('\n');

			// 3. Find all lines starting with a pipe and containing an equal sign -> those are the ones that define params values
			List<int> parametersLines = new List<int>(lines.Length);
			for(int i = 0; i < lines.Length; i++) {
				if(lines[i].Trim().StartsWith("|") && lines[i].Contains("=")) {
					parametersLines.Add(i);
				}
			}

			// 4. For each parameter line, extract the parameter value, spanning through all subsequent non-parameter lines
			//    Build a name->value dictionary for parameters
			Dictionary<string, string> values = new Dictionary<string, string>(parametersLines.Count);
			for(int i = 0; i < parametersLines.Count; i++) {
				// Extract parameter name
				int equalSignIndex = lines[parametersLines[i]].IndexOf("=");
				string parameterName = lines[parametersLines[i]].Substring(0, equalSignIndex);
				parameterName = parameterName.Trim(' ', '|').ToLowerInvariant();

				StringBuilder currentValue = new StringBuilder(100);
				currentValue.Append(lines[parametersLines[i]].Substring(equalSignIndex + 1).TrimStart(' '));

				// Span all subsequent lines
				if(i < parametersLines.Count - 1) {
					for(int span = parametersLines[i] + 1; span < parametersLines[i + 1]; span++) {
						currentValue.Append("\n");
						currentValue.Append(lines[span]);
					}
				}
				else if(parametersLines[i] < lines.Length - 1) {
					// All remaining lines belong to the last parameter
					for(int span = parametersLines[i] + 1; span < lines.Length; span++) {
						currentValue.Append("\n");
						currentValue.Append(lines[span]);
					}
				}

				if(!values.ContainsKey(parameterName)) values.Add(parameterName, currentValue.ToString());
				else values[parameterName] = currentValue.ToString();
			}

			// 5. Prepare the snippet output, replacing parameters placeholders with their values
			//    Use a lowercase version of the snippet to identify parameters locations

			string lowercaseSnippetContent = snippet.Content.ToLowerInvariant();
			StringBuilder output = new StringBuilder(snippet.Content);

			foreach(KeyValuePair<string, string> pair in values) {
				int index = lowercaseSnippetContent.IndexOf("?" + pair.Key + "?");
				while(index >= 0) {
					output.Remove(index, pair.Key.Length + 2);
					output.Insert(index, pair.Value);

					// Need to update lowercase representation because the parameters values alter the length
					lowercaseSnippetContent = output.ToString().ToLowerInvariant();
					index = lowercaseSnippetContent.IndexOf("?" + pair.Key + "?");
				}
			}

			// Remove all remaining parameters and return
			string tempResult = Snippets.ParametersRegex.Replace(output.ToString(), "");
			return ReplaceToc(tempResult, cachedToc);
		}

		/// <summary>
		/// Format classic number-parameterized snippets.
		/// </summary>
		/// <param name="capturedMarkup">The captured markup to process.</param>
		/// <returns>The formatted result.</returns>
		private static string FormatClassicSnippet(string capturedMarkup) {
			int secondPipe = capturedMarkup.Substring(3).IndexOf("|");
			string name = "";
			if(secondPipe == -1) name = capturedMarkup.Substring(3, capturedMarkup.Length - 4); // No parameters
			else name = capturedMarkup.Substring(3, secondPipe);
			Snippet snippet = Snippets.Find(name);
			if(snippet != null) {
				string[] parameters = CustomSplit(capturedMarkup.Substring(3 + secondPipe + 1, capturedMarkup.Length - secondPipe - 5));
				string fs = PrepareSnippet(parameters, snippet.Content);
				return fs.Trim('\n');
			}
			else {
				return @"<b style=""color: #FF0000;"">FORMATTER ERROR (Snippet Not Found)</b>";
			}
		}

		/// <summary>
		/// Splits a string at pipe characters, taking into account square brackets for links and images.
		/// </summary>
		/// <param name="data">The input data.</param>
		/// <returns>The resuling splitted strings.</returns>
		private static string[] CustomSplit(string data) {
			// 1. Find all pipes that are not enclosed in square brackets
			List<int> indices = new List<int>(10);
			int index = 0;
			int openBrackets = 0; // Account for links with two brackets, e.g. [[link]]
			while(index < data.Length) {
				if(data[index] == '|') {
					if(openBrackets == 0) indices.Add(index);
				}
				else if(data[index] == '[') openBrackets++;
				else if(data[index] == ']') openBrackets--;
				if(openBrackets < 0) openBrackets = 0;
				index++;
			}

			// 2. Split string at reported indices
			indices.Insert(0, -1);
			indices.Add(data.Length);

			List<string> result = new List<string>(indices.Count);
			for(int i = 0; i < indices.Count - 1; i++) {
				result.Add(data.Substring(indices[i] + 1, indices[i + 1] - indices[i] - 1));
			}

			return result.ToArray();
		}

		/// <summary>
		/// Prepares the content of a snippet, properly managing parameters.
		/// </summary>
		/// <param name="parameters">The snippet parameters.</param>
		/// <param name="snippet">The snippet original text.</param>
		/// <returns>The prepared snippet text.</returns>
		private static string PrepareSnippet(string[] parameters, string snippet) {
			StringBuilder sb = new StringBuilder(snippet);

			for(int i = 0; i < parameters.Length; i++) {
				sb.Replace(string.Format("?{0}?", i + 1), parameters[i]);
			}

			// Remove all remaining parameters that have no value
			return Snippets.ParametersRegex.Replace(sb.ToString(), "");
		}

		/// <summary>
		/// Escapes all the characters used by the WikiMarkup.
		/// </summary>
		/// <param name="content">The Content.</param>
		/// <returns>The escaped Content.</returns>
		private static string EscapeWikiMarkup(string content) {
			StringBuilder sb = new StringBuilder(content);
			sb.Replace("&", "&amp;"); // Before all other escapes!
			sb.Replace("#", "&#35;");
			sb.Replace("*", "&#42;");
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			sb.Replace("[", "&#91;");
			sb.Replace("]", "&#93;");
			sb.Replace("{", "&#123;");
			sb.Replace("}", "&#125;");
			sb.Replace("'''", "&#39;&#39;&#39;");
			sb.Replace("''", "&#39;&#39;");
			sb.Replace("=====", "&#61;&#61;&#61;&#61;&#61;");
			sb.Replace("====", "&#61;&#61;&#61;&#61;");
			sb.Replace("===", "&#61;&#61;&#61;");
			sb.Replace("==", "&#61;&#61;");
			sb.Replace("§§", "&#167;&#167;");
			sb.Replace("__", "&#95;&#95;");
			sb.Replace("--", "&#45;&#45;");
			sb.Replace("@@", "&#64;&#64;");
			sb.Replace(":", "&#58;");
			return sb.ToString();
		}

		/// <summary>
		/// Removes all the characters used by the WikiMarkup.
		/// </summary>
		/// <param name="content">The Content.</param>
		/// <returns>The stripped Content.</returns>
		private static string StripWikiMarkup(string content) {
			if(string.IsNullOrEmpty(content)) return "";

			StringBuilder sb = new StringBuilder(content);
			sb.Replace("*", "");
			sb.Replace("<", "");
			sb.Replace(">", "");
			sb.Replace("[", "");
			sb.Replace("]", "");
			sb.Replace("{", "");
			sb.Replace("}", "");
			sb.Replace("'''", "");
			sb.Replace("''", "");
			sb.Replace("=====", "");
			sb.Replace("====", "");
			sb.Replace("===", "");
			sb.Replace("==", "");
			sb.Replace("§§", "");
			sb.Replace("__", "");
			sb.Replace("--", "");
			sb.Replace("@@", "");
			return sb.ToString();
		}

		/// <summary>
		/// Removes all HTML markup from a string.
		/// </summary>
		/// <param name="content">The string.</param>
		/// <returns>The result.</returns>
		public static string StripHtml(string content) {
			if(string.IsNullOrEmpty(content)) return "";

			StringBuilder sb = new StringBuilder(Regex.Replace(content, "<[^>]*>", " "));
			sb.Replace("&nbsp;", "");
			sb.Replace("  ", " ");
			return sb.ToString();
		}

		/// <summary>
		/// Builds a Link.
		/// </summary>
		/// <param name="targetUrl">The (raw) HREF.</param>
		/// <param name="title">The name/title.</param>
		/// <param name="isImage">True if the link contains an Image as "visible content".</param>
		/// <param name="imageTitle">The title of the image.</param>
		/// <param name="forIndexing">A value indicating whether the formatting is being done for content indexing.</param>
		/// <param name="bareBones">A value indicating whether the formatting is being done in bare-bones mode.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPage">The current page, or <c>null</c>.</param>
		/// <param name="linkedPages">The linked pages list (both existent and inexistent).</param>
		/// <returns>The formatted Link.</returns>
		private static string BuildLink(string targetUrl, string title, bool isImage, string imageTitle,
			bool forIndexing, bool bareBones, FormattingContext context, PageInfo currentPage, List<string> linkedPages) {

			if(targetUrl == null) targetUrl = "";
			if(title == null) title = "";
			if(imageTitle == null) imageTitle = "";

			bool blank = false;
			if(targetUrl.StartsWith("^")) {
				blank = true;
				targetUrl = targetUrl.Substring(1);
			}
			targetUrl = EscapeUrl(targetUrl);
			string nstripped = StripWikiMarkup(StripHtml(title));
			string imageTitleStripped = StripWikiMarkup(StripHtml(imageTitle));

			StringBuilder sb = new StringBuilder(150);

			if(targetUrl.ToLowerInvariant().Equals("anchor") && title.StartsWith("#")) {
				sb.Append(@"<a id=""");
				sb.Append(title.Substring(1));
				sb.Append(@"""> </a>");
			}
			else if(targetUrl.StartsWith("#")) {
				sb.Append(@"<a");
				if(!isImage) sb.Append(@" class=""internallink""");
				if(blank) sb.Append(@" target=""_blank""");
				sb.Append(@" href=""");
				//sb.Append(a);
				UrlTools.BuildUrl(sb, targetUrl);
				sb.Append(@""" title=""");
				if(!isImage && title.Length > 0) sb.Append(nstripped);
				else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(targetUrl.Substring(1));
				sb.Append(@""">");
				if(title.Length > 0) sb.Append(title);
				else sb.Append(targetUrl.Substring(1));
				sb.Append("</a>");
			}
			else if(targetUrl.StartsWith("http://") || targetUrl.StartsWith("https://") || targetUrl.StartsWith("ftp://") || targetUrl.StartsWith("file://")) {
				// The link is complete
				sb.Append(@"<a");
				if(!isImage) sb.Append(@" class=""externallink""");
				sb.Append(@" href=""");
				sb.Append(targetUrl);
				sb.Append(@""" title=""");
				if(!isImage && title.Length > 0) sb.Append(nstripped);
				else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(targetUrl);
				sb.Append(@""" target=""_blank"">");
				if(title.Length > 0) sb.Append(title);
				else sb.Append(targetUrl);
				sb.Append("</a>");
			}
			else if(targetUrl.StartsWith(@"\\") || targetUrl.StartsWith("//")) {
				// The link is a UNC path
				sb.Append(@"<a");
				if(!isImage) sb.Append(@" class=""externallink""");
				sb.Append(@" href=""file://///");
				sb.Append(targetUrl.Substring(2));
				sb.Append(@""" title=""");
				if(!isImage && title.Length > 0) sb.Append(nstripped);
				else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(targetUrl);
				sb.Append(@""" target=""_blank"">");
				if(title.Length > 0) sb.Append(title);
				else sb.Append(targetUrl);
				sb.Append("</a>");
			}
			else if(targetUrl.IndexOf("@") != -1 && targetUrl.IndexOf(".") != -1) {
				// Email
				sb.Append(@"<a");
				if(!isImage) sb.Append(@" class=""emaillink""");
				if(blank) sb.Append(@" target=""_blank""");
				if(targetUrl.StartsWith(@"mailto:")) {
					sb.Append(@" href=""");
				} else {
					sb.Append(@" href=""mailto:");
				}
				sb.Append(Tools.ObfuscateText(targetUrl.Replace("&amp;", "%26"))); // Trick to let ampersands work in email addresses
				sb.Append(@""" title=""");
				if(!isImage && title.Length > 0) sb.Append(nstripped);
				else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(Tools.ObfuscateText(targetUrl));
				sb.Append(@""">");
				if(title.Length > 0) sb.Append(title);
				else sb.Append(Tools.ObfuscateText(targetUrl));
				sb.Append("</a>");
			}
			else if(((targetUrl.IndexOf(".") != -1 && !targetUrl.ToLowerInvariant().EndsWith(".aspx")) || targetUrl.EndsWith("/")) &&
				!targetUrl.StartsWith("++") && Pages.FindPage(targetUrl) == null &&
				!targetUrl.StartsWith("c:") && !targetUrl.StartsWith("C:")) {
				// Link to an internal file or subdirectory, or link to GetFile.aspx
				sb.Append(@"<a");
				if(!isImage) sb.Append(@" class=""internallink""");
				if(blank) sb.Append(@" target=""_blank""");
				sb.Append(@" href=""");
				//sb.Append(a);
				if(targetUrl.ToLowerInvariant().StartsWith("getfile.aspx")) sb.Append(targetUrl);
				else UrlTools.BuildUrl(sb, targetUrl);
				sb.Append(@""" title=""");
				if(!isImage && title.Length > 0) sb.Append(nstripped);
				else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
				else sb.Append(targetUrl);
				sb.Append(@""">");
				if(title.Length > 0) sb.Append(title);
				else sb.Append(targetUrl);
				sb.Append("</a>");
			}
			else {
				if(targetUrl.IndexOf(".aspx") != -1) {
					// The link points to a "system" page
					sb.Append(@"<a");
					if(!isImage) sb.Append(@" class=""systemlink""");
					if(blank) sb.Append(@" target=""_blank""");
					sb.Append(@" href=""");
					//sb.Append(a);
					UrlTools.BuildUrl(sb, targetUrl);
					sb.Append(@""" title=""");
					if(!isImage && title.Length > 0) sb.Append(nstripped);
					else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
					else sb.Append(targetUrl);
					sb.Append(@""">");
					if(title.Length > 0) sb.Append(title);
					else sb.Append(targetUrl);
					sb.Append("</a>");
				}
				else {
					if(targetUrl.StartsWith("c:") || targetUrl.StartsWith("C:")) {
						// Category link
						//sb.Append(@"<a href=""AllPages.aspx?Cat=");
						//sb.Append(Tools.UrlEncode(a.Substring(2)));
						sb.Append(@"<a href=""");
						UrlTools.BuildUrl(sb, "AllPages.aspx?Cat=", Tools.UrlEncode(targetUrl.Substring(2)));
						sb.Append(@""" class=""systemlink"" title=""");
						if(!isImage && title.Length > 0) sb.Append(nstripped);
						else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
						else sb.Append(targetUrl.Substring(2));
						sb.Append(@""">");
						if(title.Length > 0) sb.Append(title);
						else sb.Append(targetUrl.Substring(2));
						sb.Append("</a>");
					}
					else if(targetUrl.Contains(":") || targetUrl.ToLowerInvariant().Contains("%3a") || targetUrl.Contains("&") || targetUrl.Contains("%26")) {
						sb.Append(@"<b style=""color: #FF0000;"">FORMATTER ERROR ("":"" and ""&"" not supported in Page Names)</b>");
					}
					else {
						// The link points to a wiki page
						bool explicitNamespace = false;
						string tempLink = targetUrl;
						if(tempLink.StartsWith("++")) {
							tempLink = tempLink.Substring(2);
							targetUrl = targetUrl.Substring(2);
							explicitNamespace = true;
						}

						if(targetUrl.IndexOf("#") != -1) {
							tempLink = targetUrl.Substring(0, targetUrl.IndexOf("#"));
							targetUrl = Tools.UrlEncode(targetUrl.Substring(0, targetUrl.IndexOf("#"))) + Settings.PageExtension + targetUrl.Substring(targetUrl.IndexOf("#"));
						}
						else {
							targetUrl += Settings.PageExtension;
							// #468: Preserve ++ for ReverseFormatter
							targetUrl = (bareBones && explicitNamespace ? "++" : "") + Tools.UrlEncode(targetUrl);
						}

						string fullName = "";
						if(!explicitNamespace) {
							fullName = currentPage != null ?
								 NameTools.GetFullName(NameTools.GetNamespace(currentPage.FullName), NameTools.GetLocalName(tempLink)) :
								 tempLink;
						}
						else fullName = tempLink;

						// Add linked page without repetitions
						//linkedPages.Add(fullName);
						PageInfo info = Pages.FindPage(fullName);
						if(info != null) {
							if(!linkedPages.Contains(info.FullName)) linkedPages.Add(info.FullName);
						}
						else {
							string lowercaseFullName = fullName.ToLowerInvariant();
							if(linkedPages.Find(p => { return p.ToLowerInvariant() == lowercaseFullName; }) == null) linkedPages.Add(fullName);
						}

						if(info == null) {
							sb.Append(@"<a");
							if(!isImage) sb.Append(@" class=""unknownlink""");
							if(blank) sb.Append(@" target=""_blank""");
							sb.Append(@" href=""");
							//sb.Append(a);
							UrlTools.BuildUrl(sb, explicitNamespace ? "++" : "", targetUrl);
							sb.Append(@""" title=""");
							/*if(!isImage && title.Length > 0) sb.Append(nstripped);
							else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
							else sb.Append(tempLink);*/
							sb.Append(fullName);
							sb.Append(@""">");
							if(title.Length > 0) sb.Append(title);
							else sb.Append(tempLink);
							sb.Append("</a>");
						}
						else {
							sb.Append(@"<a");
							if(!isImage) sb.Append(@" class=""pagelink""");
							if(blank) sb.Append(@" target=""_blank""");
							sb.Append(@" href=""");
							//sb.Append(a);
							UrlTools.BuildUrl(sb, explicitNamespace ? "++" : "", targetUrl);
							sb.Append(@""" title=""");
							/*if(!isImage && title.Length > 0) sb.Append(nstripped);
							else if(isImage && imageTitle.Length > 0) sb.Append(imageTitleStripped);
							else sb.Append(FormattingPipeline.PrepareTitle(Content.GetPageContent(info, false).Title, context, currentPage));*/

							if(forIndexing) {
								// When saving a page, the SQL Server provider calls IHost.PrepareContentForIndexing
								// If the content contains a reference to the page being saved, the formatter will call GetPageContent on SQL Server,
								// resulting in a transaction deadlock (the save transaction waits for the read transaction, while the latter
								// waits the the locks on the PageContent table being released)
								// See also Content.GetPageContent

								if(currentPage != null && currentPage.FullName == info.FullName) {
									// Do not format title
									sb.Append(info.FullName);
								}
								else {
									// Try to format title
									PageContent retrievedContent = Content.GetPageContent(info, false);
									if(retrievedContent != null && !retrievedContent.IsEmpty()) {
										sb.Append(FormattingPipeline.PrepareTitle(retrievedContent.Title, forIndexing, context, currentPage));
									}
									else sb.Append(info.FullName);
								}
							}
							else sb.Append(FormattingPipeline.PrepareTitle(Content.GetPageContent(info, false).Title, forIndexing, context, currentPage));

							sb.Append(@""">");
							if(title.Length > 0) sb.Append(title);
							else sb.Append(tempLink);
							sb.Append("</a>");
						}
					}
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Detects all the Headers in a block of text (H1, H2, H3, H4).
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The List of Header objects, in the same order as they are in the text.</returns>
		public static List<HPosition> DetectHeaders(string text) {
			Match match;
			string h;
			int end = 0;
			List<int> noWikiBegin = new List<int>(), noWikiEnd = new List<int>();
			List<HPosition> hPos = new List<HPosition>();
			StringBuilder sb = new StringBuilder(text);

			ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

			int count = 0;

			match = H4Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					h = match.Value.Substring(5, match.Value.Length - 10 - (match.Value.EndsWith("\n") ? 1 : 0));
					hPos.Add(new HPosition(match.Index, h, 4, count));
					end = match.Index + match.Length;
					count++;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H4Regex.Match(sb.ToString(), end);
			}

			match = H3Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					h = match.Value.Substring(4, match.Value.Length - 8 - (match.Value.EndsWith("\n") ? 1 : 0));
					bool found = false;
					for(int i = 0; i < hPos.Count; i++) {
						if(match.Index == hPos[i].Index) {
							found = true;
							break;
						}
					}
					if(!found) {
						hPos.Add(new HPosition(match.Index, h, 3, count));
						count++;
					}
					end = match.Index + match.Length;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H3Regex.Match(sb.ToString(), end);
			}

			match = H2Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					h = match.Value.Substring(3, match.Value.Length - 6 - (match.Value.EndsWith("\n") ? 1 : 0));
					bool found = false;
					for(int i = 0; i < hPos.Count; i++) {
						if(match.Index == hPos[i].Index) {
							found = true;
							break;
						}
					}
					if(!found) {
						hPos.Add(new HPosition(match.Index, h, 2, count));
						count++;
					}
					end = match.Index + match.Length;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H2Regex.Match(sb.ToString(), end);
			}

			match = H1Regex.Match(sb.ToString());
			while(match.Success) {
				if(!IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					h = match.Value.Substring(2, match.Value.Length - 4 - (match.Value.EndsWith("\n") ? 1 : 0));
					bool found = false;
					for(int i = 0; i < hPos.Count; i++) {
						// A special treatment is needed in this case
						// because =====xxx===== matches also 2 H1 headers (=='='==)
						if(match.Index >= hPos[i].Index && match.Index <= hPos[i].Index + hPos[i].Text.Length + 5) {
							found = true;
							break;
						}
					}
					if(!found) {
						hPos.Add(new HPosition(match.Index, h, 1, count));
						count++;
					}
					end = match.Index + match.Length;
				}
				ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				match = H1Regex.Match(sb.ToString(), end);
			}

			return hPos;
		}

		/// <summary>
		/// Builds the "Edit" links for page sections.
		/// </summary>
		/// <param name="id">The section ID.</param>
		/// <param name="page">The page name.</param>
		/// <returns>The link.</returns>
		private static string BuildEditSectionLink(int id, string page) {
			if(!Settings.EnableSectionEditing) return "";

			StringBuilder sb = new StringBuilder(100);
			sb.Append(@"<a href=""");
			UrlTools.BuildUrl(sb, "Edit.aspx?Page=", Tools.UrlEncode(page), "&amp;Section=", id.ToString());
			sb.Append(@""" class=""editsectionlink"">");
			sb.Append(EditSectionPlaceHolder);
			sb.Append("</a>");
			return sb.ToString();
		}

		/// <summary>
		/// Generates list HTML markup.
		/// </summary>
		/// <param name="lines">The lines in the list WikiMarkup.</param>
		/// <param name="line">The current line.</param>
		/// <param name="level">The current level.</param>
		/// <param name="currLine">The current line.</param>
		/// <returns>The correct HTML markup.</returns>
		private static string GenerateList(string[] lines, int line, int level, ref int currLine) {
			StringBuilder sb = new StringBuilder(200);
			if(lines[currLine][level] == '*') sb.Append("<ul>");
			else if(lines[currLine][level] == '#') sb.Append("<ol>");
			while(currLine <= lines.Length - 1 && CountBullets(lines[currLine]) >= level + 1) {
				if(CountBullets(lines[currLine]) == level + 1) {
					sb.Append("<li>");
					sb.Append(lines[currLine].Substring(CountBullets(lines[currLine])).Trim());
					sb.Append("</li>");
					currLine++;
				}
				else {
					sb.Remove(sb.Length - 5, 5);
					sb.Append(GenerateList(lines, currLine, level + 1, ref currLine));
					sb.Append("</li>");
				}
			}
			if(lines[line][level] == '*') sb.Append("</ul>");
			else if(lines[line][level] == '#') sb.Append("</ol>");
			return sb.ToString();
		}

		/// <summary>
		/// Counts the bullets in a list line.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns>The number of bullets.</returns>
		private static int CountBullets(string line) {
			int res = 0, count = 0;
			while(line[count] == '*' || line[count] == '#') {
				res++;
				count++;
			}
			return res;
		}

		/// <summary>
		/// Extracts the bullets from a list line.
		/// </summary>
		/// <param name="value">The line.</param>
		/// <returns>The bullets.</returns>
		private static string ExtractBullets(string value) {
			string res = "";
			for(int i = 0; i < value.Length; i++) {
				if(value[i] == '*' || value[i] == '#') res += value[i];
				else break;
			}
			return res;
		}

		/// <summary>
		/// Builds the TOC of a document.
		/// </summary>
		/// <param name="hPos">The positions of headers.</param>
		/// <returns>The TOC HTML markup.</returns>
		private static string BuildToc(List<HPosition> hPos) {
			StringBuilder sb = new StringBuilder();

			hPos.Sort(new HPositionComparer());

			// Table only used to workaround IE idiosyncrasies - use TocCointainer for styling
			sb.Append(@"<table id=""TocContainerTable""><tr><td>");
			sb.Append(@"<div id=""TocContainer"">");
			sb.Append(@"<p class=""small"">");
			sb.Append(TocTitlePlaceHolder);
			sb.Append("</p>");

			sb.Append(@"<div id=""Toc"">");
			sb.Append("<p><br />");
			for(int i = 0; i < hPos.Count; i++) {
				switch(hPos[i].Level) {
					case 1:
						break;
					case 2:
						sb.Append("&nbsp;&nbsp;&nbsp;");
						break;
					case 3:
						sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
						break;
					case 4:
						sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
						break;
				}
				if(hPos[i].Level == 1) sb.Append("<b>");
				if(hPos[i].Level == 4) sb.Append("<small>");
				sb.Append(@"<a href=""#");
				sb.Append(BuildHAnchor(hPos[i].Text, hPos[i].ID.ToString()));
				sb.Append(@""">");
				sb.Append(StripWikiMarkup(StripHtml(hPos[i].Text)));
				sb.Append("</a>");
				if(hPos[i].Level == 4) sb.Append("</small>");
				if(hPos[i].Level == 1) sb.Append("</b>");
				sb.Append("<br />");
			}
			sb.Append("</p>");
			sb.Append("</div>");

			sb.Append("</div>");
			sb.Append("</td></tr></table>");

			return sb.ToString();
		}

		/// <summary>
		/// Builds a valid anchor name from a string.
		/// </summary>
		/// <param name="h">The string, usually a header (Hx).</param>
		/// <returns>The anchor ID.</returns>
		public static string BuildHAnchor(string h) {
			// Remove any extra spaces around the heading title:
			// '=== Title ===' results in '<a id="Title">' instead of '<a id="_Title_">'
			if(h != null) h = h.Trim();

			StringBuilder sb = new StringBuilder(StripWikiMarkup(StripHtml(h)));
			sb.Replace(" ", "_");
			sb.Replace(".", "");
			sb.Replace(",", "");
			sb.Replace(";", "");
			sb.Replace("\"", "");
			sb.Replace("/", "");
			sb.Replace("\\", "");
			sb.Replace("'", "");
			sb.Replace("(", "");
			sb.Replace(")", "");
			sb.Replace("[", "");
			sb.Replace("]", "");
			sb.Replace("{", "");
			sb.Replace("}", "");
			sb.Replace("<", "");
			sb.Replace(">", "");
			sb.Replace("#", "");
			sb.Replace("\n", "");
			sb.Replace("?", "");
			sb.Replace("&", "");
			sb.Replace("0", "A");
			sb.Replace("1", "B");
			sb.Replace("2", "C");
			sb.Replace("3", "D");
			sb.Replace("4", "E");
			sb.Replace("5", "F");
			sb.Replace("6", "G");
			sb.Replace("7", "H");
			sb.Replace("8", "I");
			sb.Replace("9", "J");
			return sb.ToString();
		}

		/// <summary>
		/// Builds a valid and unique anchor name from a string.
		/// </summary>
		/// <param name="h">The string, usually a header (Hx).</param>
		/// <param name="uid">The unique ID.</param>
		/// <returns>The anchor ID.</returns>
		public static string BuildHAnchor(string h, string uid) {
			return BuildHAnchor(h) + "_" + uid;
		}

		/// <summary>
		/// Escapes ampersands in a URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The escaped URL.</returns>
		private static string EscapeUrl(string url) {
			return url.Replace("&", "&amp;");
		}

		/// <summary>
		/// Builds a HTML table from WikiMarkup.
		/// </summary>
		/// <param name="table">The WikiMarkup.</param>
		/// <returns>The HTML.</returns>
		private static string BuildTable(string table) {
			// Proceed line-by-line, ignoring the first and last one
			string[] lines = table.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if(lines.Length < 3) {
				return "<b>FORMATTER ERROR (Malformed Table)</b>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<table");
			if(lines[0].Length > 2) {
				sb.Append(" ");
				sb.Append(lines[0].Substring(3));
			}
			sb.Append(">");
			int count = 1;
			if(lines[1].Length >= 3 && lines[1].Trim().StartsWith("|+")) {
				// Table caption
				sb.Append("<caption>");
				sb.Append(lines[1].Substring(3));
				sb.Append("</caption>");
				count++;
			}

			if(!lines[count].StartsWith("|-")) sb.Append("<tr>");

			bool thAdded = false;

			string item;
			for(int i = count; i < lines.Length - 1; i++) {
				if(lines[i].Trim().StartsWith("|-")) {
					// New line
					if(i != count) sb.Append("</tr>");

					sb.Append("<tr");
					if(lines[i].Length > 2) {
						string style = lines[i].Substring(3);
						if(style.Length > 0) {
							sb.Append(" ");
							sb.Append(style);
						}
					}
					sb.Append(">");
				}
				else if(lines[i].Trim().StartsWith("|")) {
					// Cell
					if(lines[i].Length < 3) continue;
					item = lines[i].Substring(2);
					if(item.IndexOf(" || ") != -1) {
						sb.Append("<td>");
						sb.Append(item.Replace(" || ", "</td><td>"));
						sb.Append("</td>");
					}
					else if(item.IndexOf(" | ") != -1) {
						sb.Append("<td ");
						sb.Append(item.Substring(0, item.IndexOf(" | ")));
						sb.Append(">");
						sb.Append(item.Substring(item.IndexOf(" | ") + 3));
						sb.Append("</td>");
					}
					else {
						sb.Append("<td>");
						sb.Append(item);
						sb.Append("</td>");
					}
				}
				else if(lines[i].Trim().StartsWith("!")) {
					// Header
					if(lines[i].Length < 3) continue;

					// only if ! is found in the first row of the table, it is an header
					if(lines[i + 1] == "|-") thAdded = true;

					item = lines[i].Substring(2);
					if(item.IndexOf(" !! ") != -1) {
						sb.Append("<th>");
						sb.Append(item.Replace(" !! ", "</th><th>"));
						sb.Append("</th>");
					}
					else if(item.IndexOf(" ! ") != -1) {
						sb.Append("<th ");
						sb.Append(item.Substring(0, item.IndexOf(" ! ")));
						sb.Append(">");
						sb.Append(item.Substring(item.IndexOf(" ! ") + 3));
						sb.Append("</th>");
					}
					else {
						sb.Append("<th>");
						sb.Append(item);
						sb.Append("</th>");
					}
				}
			}
			if(sb.ToString().EndsWith("<tr>")) {
				sb.Remove(sb.Length - 4 - 1, 4);
				sb.Append("</table>");
			}
			else {
				sb.Append("</tr></table>");
			}
			sb.Replace("<tr></tr>", "");

			// Add <thead>, <tbody> tags, if table contains header
			if(thAdded) {
				int thIndex = sb.ToString().IndexOf("<th");
				//if(thIndex >= 4) sb.Insert(thIndex - 4, "<thead>");
				sb.Insert(thIndex - 4, "<thead>");
				
				// search for the last </th> tag in the first row of the table
				int thCloseIndex = -1;
				int thCloseIndex_temp = -1;
				do {
					thCloseIndex = thCloseIndex_temp;
					thCloseIndex_temp = sb.ToString().IndexOf("</th>", thCloseIndex + 1);
				}
				while (thCloseIndex_temp != -1/* && thCloseIndex_temp < sb.ToString().IndexOf("</tr>") #443, but disables row-header support */);
				
				sb.Insert(thCloseIndex + 10, "</thead><tbody>");
				sb.Insert(sb.Length - 8, "</tbody>");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds an indented text block.
		/// </summary>
		/// <param name="indent">The input text.</param>
		/// <returns>The result.</returns>
		private static string BuildIndent(string indent) {
			int colons = 0;
			indent = indent.Trim();
			while(colons < indent.Length && indent[colons] == ':') colons++;
			indent = indent.Substring(colons).Trim();
			return @"<div class=""indent"" style=""margin: 0px; padding: 0px; padding-left: " + ((int)(colons * 15)).ToString() + @"px"">" + indent + "</div>";
		}

		/// <summary>
		/// Builds the tag cloud.
		/// </summary>
		/// <param name="currentNamespace">The current namespace (<c>null</c> for the root).</param>
		/// <returns>The tag cloud.</returns>
		private static string BuildCloud(NamespaceInfo currentNamespace) {
			StringBuilder sb = new StringBuilder();
			// Total categorized Pages (uncategorized Pages don't count)
			int tot = Pages.GetPages(currentNamespace).Count - Pages.GetUncategorizedPages(currentNamespace).Length;
			List<CategoryInfo> categories = Pages.GetCategories(currentNamespace);
			for(int i = 0; i < categories.Count; i++) {
				if(categories[i].Pages.Length > 0) {
					//sb.Append(@"<a href=""AllPages.aspx?Cat=");
					//sb.Append(Tools.UrlEncode(categories[i].FullName));
					sb.Append(@"<a href=""");
					UrlTools.BuildUrl(sb, "AllPages.aspx?Cat=", Tools.UrlEncode(categories[i].FullName));
					sb.Append(@""" class=""CloudLink"" style=""font-size: ");
					sb.Append(ComputeSize((float)categories[i].Pages.Length / (float)tot * 100F).ToString());
					sb.Append(@"px;"">");
					sb.Append(NameTools.GetLocalName(categories[i].FullName));
					sb.Append("</a>");
				}
				if(i != categories.Count - 1) sb.Append(" ");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Computes the sixe of a category label in the category cloud.
		/// </summary>
		/// <param name="percentage">The occurrence percentage of the category.</param>
		/// <returns>The computes size.</returns>
		private static int ComputeSize(float percentage) {
			// Interpolates min and max size on a line, so that if:
			// - percentage = 0   -> size = minSize
			// - percentage = 100 -> size = maxSize
			// - intermediate values are calculated
			float minSize = 8, maxSize = 26;
			//return (int)((maxSize - minSize) / 100F * (float)percentage + minSize); // Linear interpolation
			return (int)(maxSize - (maxSize - minSize) * Math.Exp(-percentage / 25)); // Exponential interpolation
		}

		/// <summary>
		/// Computes the positions of all NOWIKI tags.
		/// </summary>
		/// <param name="text">The input text.</param>
		/// <param name="noWikiBegin">The output list of begin indexes of NOWIKI tags.</param>
		/// <param name="noWikiEnd">The output list of end indexes of NOWIKI tags.</param>
		private static void ComputeNoWiki(string text, ref List<int> noWikiBegin, ref List<int> noWikiEnd) {
			Match match;
			noWikiBegin.Clear();
			noWikiEnd.Clear();

			match = NoWikiRegex.Match(text);
			while(match.Success) {
				noWikiBegin.Add(match.Index);
				noWikiEnd.Add(match.Index + match.Length);
				match = NoWikiRegex.Match(text, match.Index + match.Length);
			}
		}

		/// <summary>
		/// Determines whether a character is enclosed in a NOWIKI tag.
		/// </summary>
		/// <param name="index">The index of the character.</param>
		/// <param name="noWikiBegin">The list of begin indexes of NOWIKI tags.</param>
		/// <param name="noWikiEnd">The list of end indexes of NOWIKI tags.</param>
		/// <param name="end">The end index of the NOWIKI tag that encloses the specified character, or zero.</param>
		/// <returns><c>true</c> if the specified character is enclosed in a NOWIKI tag, <c>false</c> otherwise.</returns>
		private static bool IsNoWikied(int index, List<int> noWikiBegin, List<int> noWikiEnd, out int end) {
			for(int i = 0; i < noWikiBegin.Count; i++) {
				if(index > noWikiBegin[i] && index < noWikiEnd[i]) {
					end = noWikiEnd[i];
					return true;
				}
			}
			end = 0;
			return false;
		}

		/// <summary>
		/// Performs the internal Phase 3 of the Formatting pipeline.
		/// </summary>
		/// <param name="raw">The raw data.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="current">The current PageInfo, if any.</param>
		/// <returns>The formatted content.</returns>
		public static string FormatPhase3(string raw, FormattingContext context, PageInfo current) {
			StringBuilder sb = new StringBuilder(raw.Length);
			StringBuilder dummy;
			sb.Append(raw);

			Match match;

			// Format other Phase3 special tags
			match = Phase3SpecialTagRegex.Match(sb.ToString());
			while(match.Success) {
				sb.Remove(match.Index, match.Length);
				switch(match.Value.Substring(1, match.Value.Length - 2).ToUpperInvariant()) {
					case "NAMESPACE":
						string ns = Tools.DetectCurrentNamespace();
						if(string.IsNullOrEmpty(ns)) ns = "&lt;root&gt;";
						sb.Insert(match.Index, ns);
						break;
					case "NAMESPACEDROPDOWN":
						sb.Insert(match.Index, BuildCurrentNamespaceDropDown());
						break;
					case "INCOMING":
						sb.Insert(match.Index, BuildIncomingLinksList(current, context, current));
						break;
					case "OUTGOING":
						sb.Insert(match.Index, BuildOutgoingLinksList(current, context, current));
						break;
					case "USERNAME":
						if(SessionFacade.LoginKey != null) sb.Insert(match.Index, GetProfileLink(SessionFacade.CurrentUsername));
						else sb.Insert(match.Index, GetLanguageLink(Exchanger.ResourceExchanger.GetResource("Guest")));
						break;
					case "PAGENAME":
						if(current != null) sb.Insert(match.Index, current.FullName);
						break;
					case "LOGINLOGOUT":
						if(SessionFacade.LoginKey != null) sb.Insert(match.Index, GetLogoutLink());
						else sb.Insert(match.Index, GetLoginLink());
						break;
				}
				match = Phase3SpecialTagRegex.Match(sb.ToString());
			}

			sb.Replace(SectionLinkTextPlaceHolder, Exchanger.ResourceExchanger.GetResource("LinkToThisSection"));

			if(current != null) {
				match = RecentChangesRegex.Match(sb.ToString());
				while(match.Success) {
					sb.Remove(match.Index, match.Length);
					string trimmedTag = match.Value.Trim('{', '}');
					// If current page is null, assume root namespace
					NamespaceInfo currentNamespace = currentNamespace = Pages.FindNamespace(NameTools.GetNamespace(current.FullName));
					sb.Insert(match.Index, BuildRecentChanges(currentNamespace, trimmedTag.EndsWith("(*)"), context, current));
					match = RecentChangesRegex.Match(sb.ToString());
				}
			}

			match = null;

			dummy = new StringBuilder("<b>");
			dummy.Append(Exchanger.ResourceExchanger.GetResource("TableOfContents"));
			dummy.Append(@"</b><span id=""ExpandTocSpan""> [<a href=""#"" onclick=""javascript:if(document.getElementById('Toc').style['display']=='none') document.getElementById('Toc').style['display']=''; else document.getElementById('Toc').style['display']='none'; return false;"">");
			dummy.Append(Exchanger.ResourceExchanger.GetResource("HideShow"));
			dummy.Append("</a>]</span>");
			sb.Replace(TocTitlePlaceHolder, dummy.ToString());

			// Display edit links only when formatting page content (and not transcluded page content)
			if(current != null && context == FormattingContext.PageContent) {
				bool canEdit = false;
				bool canEditWithApproval = false;

				Pages.CanEditPage(current, SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(),
					out canEdit, out canEditWithApproval);

				if(canEdit || canEditWithApproval) {
					sb.Replace(EditSectionPlaceHolder, Exchanger.ResourceExchanger.GetResource("Edit"));
				}
			}
			
			// Remove all placeholders left in the page and their wrapping link
			try {
				int editSectionPhIdx = 0;
				do {
					string tempString = sb.ToString();
					editSectionPhIdx = tempString.IndexOf(EditSectionPlaceHolder);
					if(editSectionPhIdx >= 0) {
						// Find first '<' before index, and first '>' after index
						int openingIndex = editSectionPhIdx;
						while(openingIndex > 0 && tempString[openingIndex] != '<') {
							openingIndex--;
						}
						int closingIndex = tempString.IndexOf('>', editSectionPhIdx);

						sb.Remove(openingIndex, closingIndex - openingIndex + 1);
					}
				} while(editSectionPhIdx >= 0);
			}
			catch {
				// Just in case
				sb.Replace(EditSectionPlaceHolder, "");
			}

			match = SignRegex.Match(sb.ToString());
			while(match.Success) {
				sb.Remove(match.Index, match.Length);
				try {
					// Avoid that malformed tags cause a crash
					string txt = match.Value.Substring(3, match.Length - 6);
					int idx = txt.LastIndexOf(",");
					string[] fields = new string[] { txt.Substring(0, idx), txt.Substring(idx + 1) };
					dummy = new StringBuilder();
					dummy.Append(@"<span class=""signature"">");
					dummy.Append(Users.UserLink(fields[0]));
					dummy.Append(", ");
					dummy.Append(Preferences.AlignWithTimezone(DateTime.Parse(fields[1])).ToString(Settings.DateTimeFormat));
					dummy.Append("</span>");
					sb.Insert(match.Index, dummy.ToString());
				}
				catch { }
				match = SignRegex.Match(sb.ToString());
			}

			// Transclusion
			match = TransclusionRegex.Match(sb.ToString());
			while(match.Success) {
				sb.Remove(match.Index, match.Length);
				string pageName = match.Value.Substring(3, match.Value.Length - 4);
				if(pageName.StartsWith("++")) pageName = pageName.Substring(2);
				else {
					// Add current namespace, if not present
					string tsNamespace = NameTools.GetNamespace(pageName);
					string currentNamespace = current != null ? NameTools.GetNamespace(current.FullName) : null;
					if(string.IsNullOrEmpty(tsNamespace) && !string.IsNullOrEmpty(currentNamespace)) {
						pageName = NameTools.GetFullName(currentNamespace, pageName);
					}
				}
				PageInfo transcludedPage = Pages.FindPage(pageName);

				// Avoid circular transclusion
				bool transclusionAllowed =
					transcludedPage != null &&
						(current != null &&
						 transcludedPage.FullName != current.FullName ||
						 context != FormattingContext.PageContent && context != FormattingContext.TranscludedPageContent);

				if(transclusionAllowed) {
					string currentUsername = SessionFacade.GetCurrentUsername();
					string[] currentGroups = SessionFacade.GetCurrentGroupNames();

					bool canView = AuthChecker.CheckActionForPage(transcludedPage, Actions.ForPages.ReadPage, currentUsername, currentGroups);
					if(canView) {
						dummy = new StringBuilder();
						dummy.Append(@"<div class=""transcludedpage"">");
						dummy.Append(FormattingPipeline.FormatWithPhase3(
							FormattingPipeline.FormatWithPhase1And2(Content.GetPageContent(transcludedPage, true).Content,
							false, FormattingContext.TranscludedPageContent, transcludedPage),
							FormattingContext.TranscludedPageContent, transcludedPage));
						dummy.Append("</div>");
						sb.Insert(match.Index, dummy.ToString());
					}
					else {
						string formatterErrorString = @"<b style=""color: #FF0000;"">PERMISSION ERROR (You are not allowed to see transcluded page)</b>";
						sb.Insert(match.Index, formatterErrorString);
					}
				}
				else {
					string formatterErrorString = @"<b style=""color: #FF0000;"">FORMATTER ERROR (Transcluded inexistent page or this same page)</b>";
					sb.Insert(match.Index, formatterErrorString);
				}

				match = TransclusionRegex.Match(sb.ToString());
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds the current namespace drop-down list.
		/// </summary>
		/// <returns>The drop-down list HTML markup.</returns>
		private static string BuildCurrentNamespaceDropDown() {
			string ns = Tools.DetectCurrentNamespace();
			if(ns == null) ns = "";

			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			List<NamespaceInfo> allNamespaces = Pages.GetNamespaces();
			List<string> allowedNamespaces = new List<string>(allNamespaces.Count);

			if(AuthChecker.CheckActionForNamespace(null, Actions.ForNamespaces.ReadPages, currentUser, currentGroups)) {
				allowedNamespaces.Add("");
			}
			foreach(NamespaceInfo nspace in allNamespaces) {
				if(AuthChecker.CheckActionForNamespace(nspace, Actions.ForNamespaces.ReadPages, currentUser, currentGroups)) {
					allowedNamespaces.Add(nspace.Name);
				}
			}

			StringBuilder sb = new StringBuilder(500);
			sb.Append("<select class=\"namespacedropdown\" onchange=\"javascript:var sel = this.value; document.location = (sel != '' ? (sel + '.') : '') + 'Default.aspx';\">");

			foreach(string nspace in allowedNamespaces) {
				sb.AppendFormat("<option{0} value=\"{1}\">{2}</option>", nspace == ns ? " selected=\"selected\"" : "",
					nspace, nspace == "" ? "&lt;root&gt;" : nspace);
			}

			sb.Append("</select>");

			return sb.ToString();
		}

		/// <summary>
		/// Gets the link to the profile page.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The link.</returns>
		private static string GetProfileLink(string username) {
			UserInfo user = Users.FindUser(username);

			StringBuilder sb = new StringBuilder(200);
			sb.Append("<a href=\"");
			sb.Append(UrlTools.BuildUrl("Profile.aspx"));
			sb.Append("\" class=\"systemlink\" title=\"");
			sb.Append(Exchanger.ResourceExchanger.GetResource("GoToYourProfile"));
			sb.Append("\">");
			sb.Append(user != null ? Users.GetDisplayName(user) : username);
			sb.Append("</a>");
			return sb.ToString();
		}

		/// <summary>
		/// Gets the link to the language page.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The link.</returns>
		private static string GetLanguageLink(string username) {
			StringBuilder sb = new StringBuilder(200);
			sb.Append("<a href=\"");
			sb.Append(UrlTools.BuildUrl("Language.aspx"));
			sb.Append("\" class=\"systemlink\" title=\"");
			sb.Append(Exchanger.ResourceExchanger.GetResource("SelectYourLanguage"));
			sb.Append("\">");
			sb.Append(username);
			sb.Append("</a>");
			return sb.ToString();
		}

		/// <summary>
		/// Gets the login link.
		/// </summary>
		/// <returns>The login link.</returns>
		private static string GetLoginLink() {
			string login = Exchanger.ResourceExchanger.GetResource("Login");
			StringBuilder sb = new StringBuilder(200);
			sb.Append("<a href=\"");
			sb.Append(UrlTools.BuildUrl("Login.aspx?Redirect=", Tools.UrlEncode(Tools.GetCurrentUrlFixed())));
			sb.Append("\" class=\"systemlink\" title=\"");
			sb.Append(login);
			sb.Append("\">");
			sb.Append(login);
			sb.Append("</a>");
			return sb.ToString();
		}

		/// <summary>
		/// Gets the logout link.
		/// </summary>
		/// <returns>The logout link.</returns>
		private static string GetLogoutLink() {
			string login = Exchanger.ResourceExchanger.GetResource("Logout");
			StringBuilder sb = new StringBuilder(200);
			sb.Append("<a href=\"");
			sb.Append(UrlTools.BuildUrl("Login.aspx?ForceLogout=1&amp;Redirect=", Tools.UrlEncode(Tools.GetCurrentUrlFixed())));
			sb.Append("\" class=\"systemlink\" title=\"");
			sb.Append(login);
			sb.Append("\">");
			sb.Append(login);
			sb.Append("</a>");
			return sb.ToString();
		}

	}

	/// <summary>
	/// Represents a Header.
	/// </summary>
	public class HPosition {

		private int index;
		private string text;
		private int level;
		private int id;

		/// <summary>
		/// Initializes a new instance of the <b>HPosition</b> class.
		/// </summary>
		/// <param name="index">The Index.</param>
		/// <param name="text">The Text.</param>
		/// <param name="level">The Header level.</param>
		/// <param name="id">The Unique ID of the Header (0-based counter).</param>
		public HPosition(int index, string text, int level, int id) {
			this.index = index;
			this.text = text;
			this.level = level;
			this.id = id;
		}

		/// <summary>
		/// Gets or sets the Index.
		/// </summary>
		public int Index {
			get { return index; }
			set { index = value; }
		}

		/// <summary>
		/// Gets or sets the Text.
		/// </summary>
		public string Text {
			get { return text; }
			set { text = value; }
		}

		/// <summary>
		/// Gets or sets the Level.
		/// </summary>
		public int Level {
			get { return level; }
			set { level = value; }
		}

		/// <summary>
		/// Gets or sets the ID (0-based counter).
		/// </summary>
		public int ID {
			get { return id; }
			set { id = value; }
		}

	}

	/// <summary>
	/// Compares HPosition objects.
	/// </summary>
	public class HPositionComparer : IComparer<HPosition> {

		/// <summary>
		/// Performs the comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The comparison result.</returns>
		public int Compare(HPosition x, HPosition y) {
			return x.Index.CompareTo(y.Index);
		}

	}

}
