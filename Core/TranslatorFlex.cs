
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ScrewTurn.Wiki.ImportWiki {

	/// <summary>
	/// Implements a translator tool for importing FlexWiki data.
	/// </summary>
	public class TranslatorFlex : ScrewTurn.Wiki.ImportWiki.ITranslator {

		private Regex noWiki = new Regex(@"\<nowiki\>(.|\s)+?\<\/nowiki\>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private Regex noFlex = new Regex(@"\<noflex\>(.|\s)+?\<\/noflex\>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// Executes the translation.
		/// </summary>
		/// <param name="input">The input content.</param>
		/// <returns>The WikiMarkup.</returns>
		public string Translate(string input) {
			StringBuilder sb = new StringBuilder();
			sb.Append(input);
			List<int> noWikiBegin = new List<int>(), noWikiEnd = new List<int>(), noFlexBegin = new List<int>(), noFlexEnd = new List<int>();

			Match match;
			Regex doubleDoubleBrackets = new Regex(@"\""{2}.+?\""{2}");
			Regex italic = new Regex(@"_.+?_");
			Regex bold = new Regex(@"\*.+?\*");
			Regex underline = new Regex(@"\+.+?\+");
			Regex strikethrough = new Regex(@"\-.*?\-");
			Regex head = new Regex(@"^!{1,6}.+", RegexOptions.Multiline);
			Regex wikiTalk = new Regex(@"@@.+?@@");
			Regex code = new Regex(@"@.+?@");
			Regex pascalCase = new Regex(@"(\""[^""]+?\""\:)?([A-Z][a-z]+){2,}(\.([A-Z][a-z]+){2,})?");
			Regex camelCase = new Regex(@"(\""[^""]+?\""\:)?(([A-Z][a-z]+){2,}\.)?\[.+?\]");
			Regex exlink = new Regex(@"(\""[^""]+?\""\:)?(?<Protocol>\w+):\/\/(?<Domain>[\w.]+\/?)\S*");
			Regex email = new Regex(@"(\""[^""]+?\""\:)?mailto\:.+");
			Regex lists = new Regex(@"^(\t|\s{8})+(\*|(1\.))", RegexOptions.Multiline);
			Regex table = new Regex(@"^\|{2}", RegexOptions.Multiline);
			Regex newlinespace = new Regex(@"^\ .+", RegexOptions.Multiline);

			sb.Replace("\r", "");

			ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);

			match = doubleDoubleBrackets.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = doubleDoubleBrackets.Match(sb.ToString(), end);
				else {
					string s = "<nowiki>" + match.Value.Substring(2, match.Length - 4) + "</nowiki>";
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = doubleDoubleBrackets.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			match = exlink.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = exlink.Match(sb.ToString(), end);
				else {
					string s;
					string[] split = match.Value.Split(new char[] { '"' });
					if(split.Length == 1) {
						if(match.Value.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase) | match.Value.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase) | match.Value.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)) {
							string imgName = match.Value.Substring(match.Value.LastIndexOf('/') + 1, match.Length - match.Value.LastIndexOf('/') - 1);
							s = "<noflex>[image|" + imgName + "|{UP}" + imgName + "]</noflex>";
						}
						else s = "[" + match.Value + "]";
					}
					else {
						if(split[1].EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase) | split[1].EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase) | split[1].EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)) {
							string imgName = split[1].Substring(split[1].LastIndexOf('/') + 1, split[1].Length - split[1].LastIndexOf('/') - 1);
							s = "<noflex>[image|" + imgName + "|{UP}" + imgName + "|" + split[2].Substring(1, split[2].Length - 1) + "]</noflex>";
						}
						else s = "<noflex>[" + split[2].Substring(1, split[2].Length - 1) + "|" + split[1] + "]</noflex>";
					}
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoFlex(sb.ToString(), ref noFlexBegin, ref noFlexEnd);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = exlink.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			match = email.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = email.Match(sb.ToString(), end);
				else {
					string mailLink = "";
					if(match.Value.StartsWith("mailto:")) mailLink = "[" + match.Value.Replace("mailto:", "") + "]";
					else mailLink = "<noflex>[" + match.Value.Split(new char[] { ':' }, 2)[1].Replace("mailto:", "") + "|" + match.Value.Split(new char[] { ':' }, 2)[0].Substring(1, match.Value.Split(new char[] { ':' }, 2)[0].Length - 2) + "]</noflex>";
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, mailLink);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = email.Match(sb.ToString(), match.Index + mailLink.Length);
				}
			}

			match = bold.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = bold.Match(sb.ToString(), end);
				else {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, "'''" + match.Value.Substring(1, match.Length - 2) + @"'''");
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = bold.Match(sb.ToString());
				}
			}

			match = underline.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = underline.Match(sb.ToString(), end);
				else {
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, "__" + match.Value.Substring(1, match.Length - 2) + @"__");
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = underline.Match(sb.ToString());
				}
			}

			match = strikethrough.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = strikethrough.Match(sb.ToString(), end);
				else {
					string s = "";
					if(match.Value == "---") s = "---";
					else if(match.Value == "--") s = "-";
					else s = "--" + match.Value.Substring(1, match.Length - 2) + @"--";
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = strikethrough.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			match = head.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = head.Match(sb.ToString(), end);
				else {
					//Count the number of ! in order to put a corresponding
					//number of =
					int count = 1;
					for(int i = 0; i < match.Length; i++) {
						if(match.Value[i] == '!')
							count++;
					}
					if(match.Value.EndsWith("!")) count--;
					string s = "";
					for(int i = 0; i < count; i++)
						s += "=";
					s += match.Value.Substring(count - 1, match.Length - (count - 1) - 1);
					for(int i = 0; i < count; i++)
						s += "=";
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = head.Match(sb.ToString());
				}
			}

			match = wikiTalk.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noFlexBegin, noFlexEnd, out end))
					match = wikiTalk.Match(sb.ToString(), end);
				else {
					string s = @"<span style=""background-color:red; color:white""><nowiki>" + match.Value + @"</nowiki></span>";
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = wikiTalk.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			match = code.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = code.Match(sb.ToString(), end);
				else {
					string s = "{{<nowiki>" + match.Value.Substring(1, match.Length - 2) + @"</nowiki>}}";
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = code.Match(sb.ToString());
				}
			}

			match = camelCase.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = camelCase.Match(sb.ToString(), end);
				else {
					string s;
					string[] split = match.Value.Split(new char[] { ':' }, 2);
					if(split.Length == 1) {
						string[] split1 = match.Value.Split(new char[] { '.' }, 2);
						if(split1.Length == 1) s = match.Value;
						else s = split1[1];
					}
					else {
						string[] split1 = split[1].Split(new char[] { '.' }, 2);
						if(split1.Length == 1) s = "[" + split[1].Substring(1, split[1].Length - 2) + "|" + split[0].Substring(1, split[0].Length - 2) + "]";
						else s = "[" + split1[1].Substring(0, split1[1].Length - 1) + "|" + split[0].Substring(1, split[0].Length - 2) + "]";
					}
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = camelCase.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			match = pascalCase.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = pascalCase.Match(sb.ToString(), end);
				else {
					string s;
					string[] split = match.Value.Split(new char[] { ':' }, 2);
					if(split.Length == 1) {
						string[] split1 = match.Value.Split(new char[] { '.' }, 2);
						if(split1.Length == 1) s = "[" + match.Value + "]";
						else s = "[" + split1[1] + "]";
					}
					else {
						string[] split1 = split[1].Split(new char[] { '.' });
						if(split1.Length == 1) s = "[" + split[1] + "|" + split[0].Substring(1, split[0].Length - 2) + "]";
						else s = "[" + split1[1] + "|" + split[0].Substring(1, split[0].Length - 2) + "]";
					}
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = pascalCase.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			match = lists.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = lists.Match(sb.ToString(), end);
				else {
					string s = "";
					char kindOfList;
					int i = 0;
					char[] ca = match.Value.ToCharArray();
					while(ca[i] == ' ')
						i++;
					if(i > 0) {
						kindOfList = ca[i];
						i = i / 8;
					}
					else {
						while(ca[i] == '\t')
							i++;
						kindOfList = ca[i];
					}
					for(int k = 1; k <= i; k++) {
						if(kindOfList == '*') s += "*";
						if(kindOfList == '1') s += "#";
					}
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = lists.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			bool tableBegin = true;
			match = table.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = table.Match(sb.ToString(), end);
				else {
					string s = "";
					int indexEndOfLine = sb.ToString().IndexOf("\r\n", match.Index);
					if(indexEndOfLine < 0) indexEndOfLine = sb.Length;
					string[] split = sb.ToString().Substring(match.Index + 2, indexEndOfLine - match.Index - 4).Split(new string[] { "||" }, StringSplitOptions.None);
					if(tableBegin) s = "{|\r\n| " + split[0];
					else s = "|-\r\n| " + split[0];
					for(int i = 1; i < split.Length; i++)
						s += " || " + split[i];
					if(indexEndOfLine != sb.Length) {
						if(sb.ToString().Substring(indexEndOfLine + 2, 2) == "||") tableBegin = false;
						else {
							tableBegin = true;
							s += "\r\n|}";
						}
					}
					else {
						tableBegin = true;
						s += "\r\n|}";
					}
					sb.Remove(match.Index, indexEndOfLine - match.Index);
					sb.Insert(match.Index, s);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
					match = table.Match(sb.ToString(), match.Index + s.Length);
				}
			}

			match = italic.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = italic.Match(sb.ToString(), end);
				else {
					if(IsNoFlexed(match.Index, noFlexBegin, noFlexEnd, out end))
						match = italic.Match(sb.ToString(), end);
					else {
						sb.Remove(match.Index, match.Length);
						sb.Insert(match.Index, "''" + match.Value.Substring(1, match.Length - 2) + @"''");
						ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
						match = italic.Match(sb.ToString());
					}
				}
			}

			bool first = true;
			match = newlinespace.Match(sb.ToString());
			while(match.Success) {
				int end;
				if(IsNoWikied(match.Index, noWikiBegin, noWikiEnd, out end))
					match = newlinespace.Match(sb.ToString(), end);
				else {
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
					sb.Remove(match.Index, match.Length);
					sb.Insert(match.Index, s);
					match = newlinespace.Match(sb.ToString(), match.Index + s.Length);
					ComputeNoWiki(sb.ToString(), ref noWikiBegin, ref noWikiEnd);
				}

			}

			return sb.ToString().Replace("<noflex>", "").Replace("</noflex>", "");
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
					return true;
				}
			}
			end = 0;
			return false;
		}

		private void ComputeNoFlex(string text, ref List<int> noFlexBegin, ref List<int> noFlexEnd) {
			Match match;
			noFlexBegin.Clear();
			noFlexEnd.Clear();

			match = noFlex.Match(text);
			while(match.Success) {
				noFlexBegin.Add(match.Index);
				noFlexEnd.Add(match.Index + match.Length - 1);
				match = noFlex.Match(text, match.Index + match.Length);
			}
		}

		private bool IsNoFlexed(int index, List<int> noFlexBegin, List<int> noFlexEnd, out int end) {
			for(int i = 0; i < noFlexBegin.Count; i++) {
				if(index >= noFlexBegin[i] && index <= noFlexEnd[i]) {
					end = noFlexEnd[i];
					return true;
				}
			}
			end = 0;
			return false;
		}
	}

}
