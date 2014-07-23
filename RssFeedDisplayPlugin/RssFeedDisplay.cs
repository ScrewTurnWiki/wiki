
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ScrewTurn.Wiki.PluginFramework;
using System.Net;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.PluginPack {

	/// <summary>
	/// Implements a formatter provider that counts download of files and attachments.
	/// </summary>
	public class RssFeedDisplay : IFormatterProviderV30 {

		private IHostV30 _host;
		private string _config;
		private bool _enableLogging = true;
		private static readonly ComponentInformation Info = new ComponentInformation("RSS Feed Display Plugin", "Threeplicate Srl", "3.0.2.528", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/PluginPack/RssFeedDisplay2.txt");

		private static readonly Regex RssRegex = new Regex(@"{(RSS|Twitter):(.+?)(\|(.+?))?}",
			RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		/// <summary>
		/// Specifies whether or not to execute Phase 1.
		/// </summary>
		public bool PerformPhase1 {
			get { return false; }
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 2.
		/// </summary>
		public bool PerformPhase2 {
			get { return false; }
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 3.
		/// </summary>
		public bool PerformPhase3 {
			get { return true; }
		}

		/// <summary>
		/// Gets the execution priority of the provider (0 lowest, 100 highest).
		/// </summary>
		public int ExecutionPriority {
			get { return 50; }
		}

		/// <summary>
		/// Performs a Formatting phase.
		/// </summary>
		/// <param name="raw">The raw content to Format.</param>
		/// <param name="context">The Context information.</param>
		/// <param name="phase">The Phase.</param>
		/// <returns>The Formatted content.</returns>
		public string Format(string raw, ContextInformation context, FormattingPhase phase) {
			// {RSS:FeedAddress}
			// FeedAddress not found -> ignored

			StringBuilder buffer = new StringBuilder(raw);

			try {
				KeyValuePair<int, Match> block = FindAndRemoveFirstOccurrence(buffer);

				while(block.Key != -1) {
					string blockHash = block.Value.ToString();

					string result = null;

					if(System.Web.HttpContext.Current != null) {
						result = System.Web.HttpContext.Current.Cache[blockHash] as string;
					}

					if(result == null) {
						bool isTwitter = block.Value.Groups[1].Value.ToLowerInvariant() == "twitter";
						int entries = 1;
						bool newWindow = true;
						int words = 350;

						if(block.Value.Groups.Count > 3) {
							AnalyzeSettings(block.Value.Groups[4].Value, out entries, out newWindow, out words);
						}

						if(isTwitter) {
							result = @"<div class=""twitterfeed"">";
						}
						else {
							result = @"<div class=""rssfeed"">";
						}
						XmlDocument feedXml = GetXml(block.Value.Groups[2].Value);
						XmlNode node = feedXml.DocumentElement;
						for(int i = 0; i < entries; i++) {
							XmlNode itemTitle = node.SelectNodes("/rss/channel/item/title")[i];
							if(itemTitle != null) {
								XmlNode itemLink = node.SelectNodes("/rss/channel/item/link")[i];
								XmlNode itemContent = node.SelectNodes("/rss/channel/item/description")[i];
								string itemContentStr = StripHtml(itemContent.InnerText);
								itemContentStr = (itemContentStr.Length > words && itemContentStr.Substring(words - 3, 5) != "[...]") ? itemContentStr.Substring(0, itemContentStr.IndexOf(" ", words - 5) + 1) + " [...]" : itemContentStr;
								if(itemContentStr.Length <= 1) itemContentStr = StripHtml(itemContent.InnerText);

								if(isTwitter) {
									string tweet = itemTitle.InnerText;
									tweet = tweet.Substring(tweet.IndexOf(":") + 2);
									result += @"<div class=""tweet"">
										 <a href=""" + itemLink.InnerText + @""" title=""Go to this Tweet""";
									if(newWindow) result += @" target=""_blank""";
									result += @">" + tweet + @"</a>
										</div>";
								}
								else {
									result += @"<div class=""rssentry"">
										<span class=""rsstitle"">
										<a href=""" + itemLink.InnerText + @""" title=""" + itemTitle.InnerText + @"""";
									if(newWindow) result += @" target=""_blank""";
									result += @">" + itemTitle.InnerText + @"</a>
										</span>
										<br />
										<span class=""rsscontent"">" + itemContentStr + @"</span>
										</div>";
								}
							}
						}
						result += @"</div>";

						if(System.Web.HttpContext.Current != null) {
							System.Web.HttpContext.Current.Cache.Add(blockHash, result, null, DateTime.Now.AddMinutes(60),
								System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
						}
					}

					buffer.Insert(block.Key, result);

					block = FindAndRemoveFirstOccurrence(buffer);
				}
			}
			catch(Exception ex) {
				LogWarning(string.Format("Exception occurred: {0}", ex.Message));
			}
			return buffer.ToString();
		}

		/// <summary>
		/// Analizes the settings string.
		/// </summary>
		/// <param name="settingString">The setting string.</param>
		/// <param name="entries">The number of entries.</param>
		/// <param name="newWindow">The newWindow value.</param>
		/// <param name="words">The max number of words.</param>
		private void AnalyzeSettings(string settingString, out int entries, out bool newWindow, out int words) {
			entries = 1;
			newWindow = true;
			words = 350;

			String[] settings = settingString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach(string set in settings) {
				string key = set.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().ToLowerInvariant();
				string value = set.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[1].Trim().ToLowerInvariant();
				if(key == "entries") {
					try {
						entries = Int32.Parse(value);
					}
					catch(ArgumentNullException) {
						throw new ArgumentNullException("entries setting could not be null.");
						//LogWarning("entries setting could not be null.");
					}
					catch(FormatException) {
						throw new FormatException("entries setting is not a valid integer.");
					}
				}
				else if(key == "newwindow") {
					if(value == "true" || value == "1" || value == "yes") {
						newWindow = true;
					}
					else if(value == "false" || value == "0" || value == "no") {
						newWindow = false;
					}
					else {
						throw new FormatException("newWindow setting is not a valid value. Use: true/false or 1/0 or yes/no.");
					}
				}
				else if(key == "words") {
					try {
						words = Int32.Parse(value);
					}
					catch(ArgumentNullException) {
						throw new ArgumentNullException("words setting could not be null.");
					}
					catch(FormatException) {
						throw new FormatException("words setting is not a valid integer.");
					}
				}
			}
		}

		/// <summary>
		/// Produces an API call, then returns the results as an Xml Document
		/// </summary>
		/// <param name="Url">The Url to the specific API call</param>
		/// <returns></returns>
		private XmlDocument GetXml(string Url) {
			try {
				var results = new XmlDocument();
				Url = string.Format("{0}", Url);
				var request = WebRequest.Create(Url);
				var response = request.GetResponse();
				using(var reader = new StreamReader(response.GetResponseStream())) {
					var xmlString = reader.ReadToEnd();
					try {
						results.LoadXml(xmlString);
					}
					catch {
						LogWarning("Received Unexpected Response from server.");
					}
				}
				return results;
			}
			catch(Exception ex) {
				LogWarning(string.Format("Exception occurred: {0}", ex.Message));
				return null;
			}
		}

		/// <summary>
		/// Finds and removes the first occurrence of the custom tag.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns>The index->content data.</returns>
		private static KeyValuePair<int, Match> FindAndRemoveFirstOccurrence(StringBuilder buffer) {
			Match match = RssRegex.Match(buffer.ToString());

			if(match.Success) {
				buffer.Remove(match.Index, match.Length);

				return new KeyValuePair<int, Match>(match.Index, match);
			}

			return new KeyValuePair<int, Match>(-1, null);
		}

		/// <summary>
		/// Removes all HTML markup from a string.
		/// </summary>
		/// <param name="content">The string.</param>
		/// <returns>The result.</returns>
		private static string StripHtml(string content) {
			if(string.IsNullOrEmpty(content)) return "";

			StringBuilder sb = new StringBuilder(Regex.Replace(content, "<[^>]*>", " "));
			sb.Replace("&nbsp;", "");
			sb.Replace("  ", " ");
			return sb.ToString();
		}

		/// <summary>
		/// Prepares the title of an item for display (always during phase 3).
		/// </summary>
		/// <param name="title">The input title.</param>
		/// <param name="context">The context information.</param>
		/// <returns>The prepared title (no markup allowed).</returns>
		public string PrepareTitle(string title, ContextInformation context) {
			return title;
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <remarks>If the configuration string is not valid, the methoud should throw a <see cref="InvalidConfigurationException"/>.</remarks>
		public void Init(IHostV30 host, string config) {
			this._host = host;
			this._config = config != null ? config : "";

			if(this._config.ToLowerInvariant() == "nolog") _enableLogging = false;
		}

		/// <summary>
		/// Logs a warning.
		/// </summary>
		/// <param name="message">The message.</param>
		private void LogWarning(string message) {
			if(_enableLogging) {
				_host.LogEntry(message, LogEntryType.Warning, null, this);
			}
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
			// Nothing to do
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return Info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return "Specify <i>nolog</i> for disabling warning log messages."; }
		}

	}

}
