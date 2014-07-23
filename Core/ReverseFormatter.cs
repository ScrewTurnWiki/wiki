
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.IO;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements reverse formatting methods (HTML-&gt;WikiMarkup).
	/// </summary>
	public static class ReverseFormatter {

		private static void ProcessList(XmlNodeList nodes, string marker, StringBuilder sb) {
			string ul = "*";
			string ol = "#";
			foreach(XmlNode node in nodes) {
				string text = "";
				if(node.Name == "li") {
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name == "br") {
							text += "\n";
						}
						else if(child.Name != "ol" && child.Name != "ul") {
							TextReader reader = new StringReader(child.OuterXml);
							XmlDocument n = FromHTML(reader);
							StringBuilder tempSb = new StringBuilder();
							ProcessChild(n.ChildNodes, tempSb);
							text += tempSb.ToString();
						}
					}
					XmlAttribute styleAttribute = node.Attributes["style"];
					if(styleAttribute != null) {
						if(styleAttribute.Value.Contains("bold")) {
							text = "'''" + text + "'''";
						}
						if(styleAttribute.Value.Contains("italic")) {
							text = "''" + text + "''";
						}
						if(styleAttribute.Value.Contains("underline")) {
							text = "__" + text + "__";
						}
					}
					sb.Append(marker + " " + text);
					if(!sb.ToString().EndsWith("\n")) sb.Append("\n");
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name == "ol") ProcessList(child.ChildNodes, marker + ol, sb);
						if(child.Name == "ul") ProcessList(child.ChildNodes, marker + ul, sb);
					}
				}
			}
		}

		private static string ProcessImage(XmlNode node) {
			string result = "";
			if(node.Attributes.Count != 0) {
				foreach(XmlAttribute attName in node.Attributes) {
					if(attName.Name == "src") {
						string[] path = attName.Value.Split('=');
						if(path.Length > 2) result += "{" + "UP(" + path[1].Split('&')[0].Replace("%20", " ") + ")}" + path[2].Replace("%20", " ");
						else result += "{UP}" + path[path.Length - 1].Replace("%20", " ");
					}
				}
			}
			return result;
		}

		private static string ProcessLink(string link) {
			link = link.Replace("%20", " ");
			string subLink = "";
			if(link.ToLowerInvariant().StartsWith("getfile.aspx")) {
				string[] urlParameters = link.Remove(0, 13).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

				string pageName = urlParameters.FirstOrDefault(p => p.ToLowerInvariant().StartsWith("page"));
				if(!string.IsNullOrEmpty(pageName)) pageName = Uri.UnescapeDataString(pageName.Split(new char[] { '=' })[1]);
				string fileName = urlParameters.FirstOrDefault(p => p.ToLowerInvariant().StartsWith("file"));
				fileName = Uri.UnescapeDataString(fileName.Split(new char[] { '=' })[1]);
				if(string.IsNullOrEmpty(pageName)) {
					subLink = "{UP}" + fileName;
				}
				else {
					subLink = "{UP(" + pageName + ")}" + fileName;
				}
				link = subLink;
			}
			return link;
		}

		private static void ProcessChildImage(XmlNodeList nodes, StringBuilder sb) {
			string image = "";
			string p = "";
			string url = "";
			bool hasDescription = false;
			foreach(XmlNode node in nodes) {
				if(node.Name.ToLowerInvariant() == "img") image += ProcessImage(node);
				else if(node.Name.ToLowerInvariant() == "p") {
					hasDescription = true;
					StringBuilder tempSb = new StringBuilder();
					ProcessChild(node.ChildNodes, tempSb);
					p += "|" + tempSb.ToString() + "|";
				}
				else if(node.Name.ToLowerInvariant() == "a") {
					string link = "";
					string target = "";
					if(node.Attributes.Count != 0) {
						XmlAttributeCollection attribute = node.Attributes;
						foreach(XmlAttribute attName in attribute) {
							if(attName.Value == "_blank") target += "^";
							if(attName.Name == "href") link += attName.Value;
						}
					}
					link = ProcessLink(link);
					image += ProcessImage(node.LastChild);
					url = "|" + target + link;
				}
			}
			if(!hasDescription) p = "||";
			sb.Append(p + image + url);
		}
		
		private static void ProcessTableImage(XmlNodeList nodes, StringBuilder sb) {
			foreach(XmlNode node in nodes) {
				switch(node.Name.ToLowerInvariant()) {
					case "tbody":
						ProcessTableImage(node.ChildNodes, sb);
						break;
					case "tr":
						ProcessTableImage(node.ChildNodes, sb);
						break;
					case "td":
						string image = "";
						string aref = "";
						string p = "";
						bool hasLink = false;
						if(node.FirstChild.Name.ToLowerInvariant() == "img") {
							StringBuilder tempSb = new StringBuilder();
							ProcessTableImage(node.ChildNodes, tempSb);
							image += tempSb.ToString();
						}
						if(node.FirstChild.Name.ToLowerInvariant() == "a") {
							hasLink = true;
							StringBuilder tempSb = new StringBuilder();
							ProcessTableImage(node.ChildNodes, tempSb);
							aref += tempSb.ToString();
						}
						if(node.LastChild.Name.ToLowerInvariant() == "p") p += node.LastChild.InnerText;
						if(!hasLink) sb.Append(p + image);
						else sb.Append(p + aref);
						break;
					case "img":
						sb.Append("|" + ProcessImage(node));
						break;
					case "a":
						string link = "";
						string target = "";
						string title = "";
						if(node.Attributes.Count != 0) {
							XmlAttributeCollection attribute = node.Attributes;
							foreach(XmlAttribute attName in attribute) {
								if(attName.Name != "id".ToLowerInvariant()) {
									if(attName.Value == "_blank") target += "^";
									if(attName.Name == "href") link += attName.Value;
									if(attName.Name == "title") title += attName.Value;
								}
								link = ProcessLink(link);
							}
							ProcessTableImage(node.ChildNodes, sb);
							sb.Append("|" + target + link);
						}
						break;
				}
			}
		}

		private static void ProcessTable(XmlNodeList nodes, StringBuilder sb) {
			foreach(XmlNode node in nodes) {
				switch(node.Name.ToLowerInvariant()) {
					case "thead":
						ProcessTable(node.ChildNodes, sb);
						break;
					case "th":
						sb.Append("! ");
						ProcessChild(node.ChildNodes, sb);
						sb.Append("\n");
						break;
					case "caption":
						sb.Append("|+ ");
						ProcessChild(node.ChildNodes, sb);
						sb.Append("\n");
						break;
					case "tbody":
						ProcessTable(node.ChildNodes, sb);
						break;
					case "tr":
						string style = "";
						foreach(XmlAttribute attr in node.Attributes) {
							if(attr.Name.ToLowerInvariant() == "style") style += "style=\"" + attr.Value + "\" ";
						}
						sb.Append("|- " + style + "\n");
						ProcessTable(node.ChildNodes, sb);
						break;
					case "td":
						string styleTd = "";
						if(node.Attributes.Count != 0) {
							foreach(XmlAttribute attr in node.Attributes) {
								styleTd += " " + attr.Name + "=\"" + attr.Value + "\" ";
							}
							sb.Append("| " + styleTd + " | ");
							ProcessChild(node.ChildNodes, sb);
							sb.Append("\n");
						}
						else {
							sb.Append("| ");
							ProcessChild(node.ChildNodes, sb);
							sb.Append("\n");
						}
						break;
				}
			}
		}

		private static void ProcessChild(XmlNodeList nodes, StringBuilder sb) {
			foreach(XmlNode node in nodes) {
				bool anchor = false;
				if(node.NodeType == XmlNodeType.Text) sb.Append(node.Value.TrimStart('\n'));
				else if(node.NodeType != XmlNodeType.Whitespace) {
					switch(node.Name.ToLowerInvariant()) {
						case "html":
							ProcessChild(node.ChildNodes, sb);
							break;
						case "b":
						case "strong":
							if(node.HasChildNodes) {
								sb.Append("'''");
								ProcessChild(node.ChildNodes, sb);
								sb.Append("'''");
							}
							break;
						case "strike":
						case "s":
							if(node.HasChildNodes) {
								sb.Append("--");
								ProcessChild(node.ChildNodes, sb);
								sb.Append("--");
							}
							break;
						case "em":
						case "i":
							if(node.HasChildNodes) {
								sb.Append("''");
								ProcessChild(node.ChildNodes, sb);
								sb.Append("''");
							}
							break;
						case "u":
							if(node.HasChildNodes) {
								sb.Append("__");
								ProcessChild(node.ChildNodes, sb);
								sb.Append("__");
							}
							break;
						case "h1":
							if(node.HasChildNodes) {
								if(node.FirstChild.NodeType == XmlNodeType.Whitespace) {
									sb.Append("----\n");
									ProcessChild(node.ChildNodes, sb);
								}
								else {
									if(!(sb.Length == 0 || sb.ToString().EndsWith("\n"))) sb.Append("\n");
									sb.Append("==");
									ProcessChild(node.ChildNodes, sb);
									sb.Append("==\n");
								}
							}
							else sb.Append("----\n");
							break;
						case "h2":
							if(!(sb.Length == 0 || sb.ToString().EndsWith("\n"))) sb.Append("\n");
							sb.Append("===");
							ProcessChild(node.ChildNodes, sb);
							sb.Append("===\n");
							break;
						case "h3":
							if(!(sb.Length == 0 || sb.ToString().EndsWith("\n"))) sb.Append("\n");
							sb.Append("====");
							ProcessChild(node.ChildNodes, sb);
							sb.Append("====\n");
							break;
						case "h4":
							if(!(sb.Length == 0 || sb.ToString().EndsWith("\n"))) sb.Append("\n");
							sb.Append("=====");
							ProcessChild(node.ChildNodes, sb);
							sb.Append("=====\n");
							break;
						case "pre":
							if(node.HasChildNodes) sb.Append("@@" + node.InnerText + "@@");
							break;
						case "code":
							if(node.HasChildNodes) {
								sb.Append("{{");
								ProcessChild(node.ChildNodes, sb);
								sb.Append("}}");
							}
							break;
						case "hr":
						case "hr /":
							sb.Append("\n== ==\n");
							ProcessChild(node.ChildNodes, sb);
							break;
						case "span":
							if(node.Attributes["style"] != null && node.Attributes["style"].Value.Replace(" ", "").ToLowerInvariant().Contains("font-weight:normal")) {
								ProcessChild(node.ChildNodes, sb);
							}
							else if(node.Attributes["style"] != null && node.Attributes["style"].Value.Replace(" ", "").ToLowerInvariant().Contains("white-space:pre")) {
								sb.Append(": ");
							}
							else {
								sb.Append(node.OuterXml);
							}
							break;
						case "br":
							if(node.PreviousSibling != null && node.PreviousSibling.Name == "br") {
								sb.Append("\n");
							}
							else {
								if(Settings.ProcessSingleLineBreaks) sb.Append("\n");
								else sb.Append("\n\n");
							}
							break;
						case "table":
							string tableStyle = "";

							if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("imageauto")) {
								sb.Append("[imageauto|");
								ProcessTableImage(node.ChildNodes, sb);
								sb.Append("]");
							}
							else {
								foreach(XmlAttribute attName in node.Attributes) {
									tableStyle += attName.Name + "=\"" + attName.Value + "\" ";
								}
								sb.Append("{| " + tableStyle + "\n");
								ProcessTable(node.ChildNodes, sb);
								sb.Append("|}\n");
							}
							break;
						case "ol":
							if(node.PreviousSibling != null && node.PreviousSibling.Name != "br") {
								sb.Append("\n");
							}
							if(node.ParentNode != null) {
								if(node.ParentNode.Name != "td") ProcessList(node.ChildNodes, "#", sb);
								else sb.Append(node.OuterXml);
							}
							else ProcessList(node.ChildNodes, "#", sb);
							break;
						case "ul":
							if(node.PreviousSibling != null && node.PreviousSibling.Name != "br") {
								sb.Append("\n");
							}
							if(node.ParentNode != null) {
								if(node.ParentNode.Name != "td") ProcessList(node.ChildNodes, "*", sb);
								else sb.Append(node.OuterXml);
							}
							else ProcessList(node.ChildNodes, "*", sb);
							break;
						case "sup":
							sb.Append("<sup>");
							ProcessChild(node.ChildNodes, sb);
							sb.Append("</sup>");
							break;
						case "sub":
							sb.Append("<sub>");
							ProcessChild(node.ChildNodes, sb);
							sb.Append("</sub>");
							break;
						case "p":
							if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("imagedescription")) {
								continue;
							}
							else {
								ProcessChild(node.ChildNodes, sb);
								sb.Append("\n");
								if(!Settings.ProcessSingleLineBreaks) sb.Append("\n");
							}
							break;
						case "div":
							if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("box")) {
								if(node.HasChildNodes) {
									sb.Append("(((");
									ProcessChild(node.ChildNodes, sb);
									sb.Append(")))");
								}
							}
							else if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("imageleft")) {
								sb.Append("[imageleft");
								ProcessChildImage(node.ChildNodes, sb);
								sb.Append("]");
							}
							else if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("imageright")) {
								sb.Append("[imageright");
								ProcessChildImage(node.ChildNodes, sb);
								sb.Append("]");
							}
							else if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("image")) {
								sb.Append("[image");
								ProcessChildImage(node.ChildNodes, sb);
								sb.Append("]");
							}
							else if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("indent")) {
								sb.Append(": ");
								ProcessChild(node.ChildNodes, sb);
								sb.Append("\n");
							}
							else if(node.Attributes.Count > 0) {
								sb.Append(node.OuterXml);
							}
							else {
								sb.Append("\n");
								if(node.PreviousSibling != null && node.PreviousSibling.Name != "div") {
									if(!Settings.ProcessSingleLineBreaks) sb.Append("\n");
								}
								if(node.FirstChild != null && node.FirstChild.Name == "br") {
									node.RemoveChild(node.FirstChild);
								}
								if(node.HasChildNodes) {
									ProcessChild(node.ChildNodes, sb);
									if(Settings.ProcessSingleLineBreaks) sb.Append("\n");
									else sb.Append("\n\n");
								}
							}
							break;
						case "img":
							string description = "";
							bool hasClass = false;
							bool isLink = false;
							if(node.ParentNode != null && node.ParentNode.Name.ToLowerInvariant() == "a") isLink = true;
							if(node.Attributes.Count != 0) {
								foreach(XmlAttribute attName in node.Attributes) {
									if(attName.Name == "alt") description = attName.Value;
									if(attName.Name == "class") hasClass = true;
								}
							}
							if(!hasClass && !isLink) sb.Append("[image|" + description + "|" + ProcessImage(node) + "]\n");
							else if(!hasClass && isLink) sb.Append("[image|" + description + "|" + ProcessImage(node));
							else sb.Append(description + "|" + ProcessImage(node));
							break;
						case "a":
							bool isTable = false;
							string link = "";
							string target = "";
							string title = "";
							string formattedLink = "";
							bool isSystemLink = false;
							bool childImg = false;
							bool pageLink = false;
							if(node.FirstChild != null && node.FirstChild.Name == "img") childImg = true;
							if(node.ParentNode.Name == "td") isTable = true;
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Name != "id".ToLowerInvariant()) {
										if(attName.Value.ToLowerInvariant() == "_blank") target += "^";
										if(attName.Name.ToLowerInvariant() == "href") link += attName.Value.Replace("%20", " ");
										if(attName.Name.ToLowerInvariant() == "title") title += attName.Value;
										if(attName.Name.ToLowerInvariant() == "class" && attName.Value.ToLowerInvariant() == "systemlink") isSystemLink = true;
										if(attName.Name.ToLowerInvariant() == "class" && (attName.Value.ToLowerInvariant() == "unknownlink" || attName.Value.ToLowerInvariant() == "pagelink")) pageLink = true;
									}
									else {
										anchor = true;
										sb.Append("[anchor|#" + attName.Value + "]");
										ProcessChild(node.ChildNodes, sb);
										break;
									}
								}
								if(isSystemLink) {
									string[] splittedLink = link.Split('=');
									if(splittedLink.Length == 2) formattedLink = "c:" + splittedLink[1];
									else formattedLink = link.LastIndexOf('/') > 0 ? link.Substring(link.LastIndexOf('/') + 1) : link;
								}
								else if(pageLink) {
									formattedLink = link.LastIndexOf('/') > 0 ? link.Substring(link.LastIndexOf('/') + 1) : link;
									formattedLink = formattedLink.Remove(formattedLink.IndexOf(Settings.PageExtension));
									formattedLink = Uri.UnescapeDataString(formattedLink);
								}
								else {
									formattedLink = ProcessLink(link);
								}
								if(!anchor && !isTable && !childImg) {
									if(HttpUtility.HtmlDecode(title) != HttpUtility.HtmlDecode(link)) {
										sb.Append("[" + target + formattedLink + "|");
										ProcessChild(node.ChildNodes, sb);
										sb.Append("]");
									}
									else {
										sb.Append("[" + target + formattedLink + "]");
									}
								}
								if(!anchor && !childImg && isTable) {
									sb.Append("[" + target + formattedLink + "|");
									ProcessChild(node.ChildNodes, sb);
									sb.Append("]");
								}
								if(!anchor && childImg && !isTable) {
									ProcessChild(node.ChildNodes, sb);
									sb.Append("|" + target + formattedLink + "]");
								}
							}
							break;
						default:
							sb.Append(node.OuterXml);
							break;
					}
				}
			}
		}

		private static XmlDocument FromHTML(TextReader reader) {
			// setup SgmlReader
			Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
			sgmlReader.DocType = "HTML";
			sgmlReader.WhitespaceHandling = WhitespaceHandling.None;

			sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
			sgmlReader.InputStream = reader;

			// create document
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			doc.XmlResolver = null;
			doc.Load(sgmlReader);
			return doc;
		}

		/// <summary>
		/// Reverse formats HTML content into WikiMarkup.
		/// </summary>
		/// <param name="html">The input HTML.</param>
		/// <returns>The corresponding WikiMarkup.</returns>
		public static string ReverseFormat(string html) {
			StringReader strReader = new StringReader(html);
			XmlDocument x = FromHTML((TextReader)strReader);
			if(x != null && x.HasChildNodes && x.FirstChild.HasChildNodes) {
				StringBuilder sb = new StringBuilder();
				ProcessChild(x.FirstChild.ChildNodes, sb);
				return sb.ToString();
			}
			else {
				return "";
			}
		}
	}
}