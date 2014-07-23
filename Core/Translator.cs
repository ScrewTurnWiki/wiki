
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ScrewTurn.Wiki.ImportWiki {

	/// <summary>
	/// Implements a translator tool for importing MediaWiki data.
	/// </summary>
	public class Translator : ScrewTurn.Wiki.ImportWiki.ITranslator {

		private Regex noWiki = new Regex(@"<nowiki>(.|\n|\r)*?</nowiki>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// Executes the translation.
		/// </summary>
		/// <param name="input">The input content.</param>
		/// <returns>The WikiMarkup.</returns>
		public string Translate(string input) {
			StringBuilder sb = new StringBuilder();
			sb.Append(input);

			Regex firsttitle = new Regex(@"(\={2,5}).+?\1");
			Regex doubleSquare = new Regex(@"\[{2}.+?\]{2}");
			Regex exlink = new Regex(@"(\[(\w+):\/\/([^\]]+)\])|(?<!\"")((?<Protocol>\w+):\/\/(?<Domain>[\w.]+\/?)\S*)(?!\"")");
			Regex mailto = new Regex(@"(\[mailto\:([\w\d\.\@]+)\])|(?<!\"")(mailto\:([\w\d\.\@]+))(?!\"")");
			Regex image = new Regex(@"\[Image\:.+?\]");
			Regex pre = new Regex(@"<pre>((.|\n|\r)*?</pre>)?");
			Regex newlinespace = new Regex(@"^\ .+", RegexOptions.Multiline);
			Regex transclusion = new Regex(@"(\{{2})(.|\n)+?(\}{2})");
			Regex math = new Regex(@"<math>(.|\n|\r)*?</math>");
			Regex references = new Regex(@"<ref>(.|\n|\r)*?</ref>");
			Regex references1 = new Regex(@"\<references\/\>");
			Regex redirect = new Regex(@"\#REDIRECT\ \[{2}.+\]{2}");
			Match match;

			List<int> noWikiBegin = new List<int>(), noWikiEnd = new List<int>();

			/*ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

			if (sb.ToString().Contains("__NOTOC__"))
			{
				sb.Remove(sb.ToString().IndexOf("__NOTOC__"), 9);
				if (sb.ToString().Contains("{{compactTOC}}"))
				{
					sb.Remove(sb.ToString().IndexOf("{{compactTOC}}"),14);
					sb.Insert(sb.ToString().IndexOf("{{compactTOC}}"), "\n{TOC}\n");
				}
			}
			else
			{
				match = firsttitle.Match(sb.ToString());
				if(match.Success)
					sb.Insert(match.Index, "\n{TOC}\n");
			}*/

			sb.Replace("\r", "");

			ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

			///////////////////////////////////////////////
			//////BEGIN of unsupported formatting-tag//////
			///////////////////////////////////////////////
			match = math.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Replace(match.Value, @"<span style=""background-color:red; color:white""><nowiki><esc>&lt;math&gt;" + match.Value.Substring(6, match.Length - 13) + @"&lt;/math&gt;</esc></nowiki></span> ");
					match = math.Match(sb.ToString());
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				else
					match = math.Match(sb.ToString(), end);
			}

			match = references.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Replace(match.Value, @"<span style=""background-color:red; color:white""><nowiki><esc>&lt;ref&gt;" + match.Value.Substring(5, match.Length - 11) + @"&lt;/ref&gt;</esc></nowiki></span> ");
					match = references.Match(sb.ToString());
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				else
					match = references.Match(sb.ToString(), end);
			}

			match = references1.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Replace(match.Value, @"<span style=""background-color:red; color:white""><nowiki><esc>&lt;references/&gt;</esc></nowiki></span> ");
					match = references1.Match(sb.ToString());
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				else
					match = references1.Match(sb.ToString(), end);
			}

			match = redirect.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					sb.Replace(match.Value, @"<span style=""background-color:red; color:white""><nowiki><esc>" + match.Value + @"</esc></nowiki></span> ");
					match = redirect.Match(sb.ToString());
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				else
					match = redirect.Match(sb.ToString(), end);
			}

			match = transclusion.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					string s = @"<span style=""background-color:red; color:white""><nowiki>" + match.Value + @"</nowiki></span> ";
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = transclusion.Match(sb.ToString(), match.Index + s.Length);
				}
				match = transclusion.Match(sb.ToString(), end);
			}
			//////////////////////////////////////////
			/////END of unsupported formetter-tag/////
			//////////////////////////////////////////

			match = pre.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					string s = "{{{{<nowiki>" + match.Value.Replace("<pre>", "").Replace("</pre>", "") + @"</nowiki>}}}}";
					sb.Replace(match.Value, s);
					match = pre.Match(sb.ToString(), match.Index + s.Length);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				else
					match = pre.Match(sb.ToString(), end);
			}

			match = exlink.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					if(match.Value[0] == '[') {
						string[] split = match.Value.Split(new char[] { ' ' }, 2);
						if(split.Length == 2) {
							sb.Remove(match.Index, match.Length);
							sb.Insert(match.Index, split[0] + "|" + split[1]);
							match = exlink.Match(sb.ToString(), match.Index + match.Length);
						}
						else
							match = exlink.Match(sb.ToString(), match.Index + match.Length);
					}
					else {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, "[" + match.Value + "]");
						match = exlink.Match(sb.ToString(), match.Index + match.Length + 1);
						ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					}
				}
				else
					match = exlink.Match(sb.ToString(), end);
			}

			match = mailto.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					string str = match.Value.Replace(@"mailto:", "");
					if(str[0] == '[') {
						string[] split = str.Split(new char[] { ' ' }, 2);
						if(split.Length == 2) {
							sb.Remove(match.Index, match.Length);
							sb.Insert(match.Length, split[0] + "|" + split[1]);
						}
						/*else
						{
							sb.Remove(match.Index, match.Length);
							sb.Insert(match.Index, split[0]);
						}*/
						match = mailto.Match(sb.ToString(), match.Index + str.Length);
					}
					else {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, "[" + str.Substring(0, str.Length) + "]");
						match = mailto.Match(sb.ToString(), match.Index + str.Length + 1);
						ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					}
				}
				else
					match = mailto.Match(sb.ToString(), end);
			}

			match = image.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					string str = match.Value.Remove(0, 7);
					str = str.Remove(str.Length - 1);

					string name = "";
					string location = "";
					string caption = "";
					string[] splits = str.Split(new char[] { '|' });
					name = splits[0];
					for(int i = 0; i < splits.Length; i++) {
						if(splits[i] == "right" || splits[i] == "left" || splits[i] == "center" || splits[i] == "none")
							location = splits[i];
						else if(splits[i].Contains("px")) { }
						else if(splits[i] == "thumb" || splits[i] == "thumbnail" || splits[i] == "frame") { }
						else
							caption = splits[i] + "|";
					}
					if(location == "right" || location == "left") {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, "[image" + location + "|" + caption + "{UP}" + name + "]");
					}
					else {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, "[imageauto|" + caption + "{UP}" + name + "]");
					}
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				match = image.Match(sb.ToString(), end);
			}

			match = doubleSquare.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					if(match.Value.Contains(":")) {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, match.Value.Replace(':', '_').Replace("/", "_").Replace(@"\", "_").Replace('?', '_'));
					}
					else {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, match.Value.Substring(1, match.Length - 2).Replace("/", "_").Replace(@"\", "_").Replace('?', '_'));
					}
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				match = doubleSquare.Match(sb.ToString(), end);
			}

			bool first = true;
			match = newlinespace.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end)) {
					string s = "";
					if(first)
						s += "{{{{" + match.Value.Substring(1, match.Value.Length - 1);
					else
						s += match.Value.Substring(1, match.Value.Length - 1);
					if(sb.Length > match.Index + match.Length + 1) {
						if(sb[match.Index + match.Length] == '\n' && sb[match.Index + match.Length + 1] == ' ') {
							first = false;
						}
						else {
							s += "}}}}";
							first = true;
						}
					}
					else {
						s += "}}}}";
						first = true;
					}
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					match = newlinespace.Match(sb.ToString(), match.Index + s.Length);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}
				else
					match = newlinespace.Match(sb.ToString(), end);

			}

			return sb.ToString();
		}

		private void ComputeNoWiki(string text, ref List<int> noWikiBegin, ref List<int> noWikiEnd) {
			Match match;
			noWikiBegin.Clear();
			noWikiEnd.Clear();

			match = noWiki.Match(text);
			while(match.Success) {
				noWikiBegin.Add(match.Index);
				noWikiEnd.Add(match.Index + match.Length - 1);
				match = noWiki.Match(text, match.Index + match.Length);
			}
		}

		private bool IsNoWikied(int index, List<int> noWikiBegin, List<int> noWikiEnd, out int end) {
			for(int i = 0; i < noWikiBegin.Count; i++) {
				if(index >= noWikiBegin[i] && index <= noWikiEnd[i]) {
					end = noWikiEnd[i];
					return false;
				}
			}
			end = 0;
			return true;
		}
	}

}
