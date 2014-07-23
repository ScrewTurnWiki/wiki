
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.PluginPack {

	/// <summary>
	/// Implements a formatter provider that counts download of files and attachments.
	/// </summary>
	public class DownloadCounter : IFormatterProviderV30 {

		private static readonly DateTime DefaultStartDate = new DateTime(2009, 1, 1);
		private const string CountPlaceholder = "#count#";
		private const string DailyPlaceholder = "#daily#";
		private const string WeeklyPlaceholder = "#weekly#";
		private const string MonthlyPlaceholder = "#monthly#";

		private IHostV30 _host;
		private string _config;
		private bool _enableLogging = true;
		private static readonly ComponentInformation Info = new ComponentInformation("Download Counter Plugin", "Threeplicate Srl", "3.0.1.472", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/PluginPack/DownloadCounter2.txt");

		private static readonly Regex XmlRegex = new Regex(@"\<countDownloads(.+?)\>(.+?)\<\/countDownloads\>",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
			// <countDownloads pattern="..."[ startDate="yyyy/mm/dd"]>
			//   <file name="..."[ provider="..."] />
			//   <attachment name="..." page="..."[ provider="..."] />
			// </countDownloads>
			// All downloads are grouped together
			// Pattern placeholders: #COUNT#, #DAILY#, #WEEKLY#, #MONTHLY# (case insensitive)
			// Pattern example: "Downloaded #COUNT# times (#MONTHLY#/month)!"
			// StartDate omitted -> 2009/01/01
			// Provider omitted -> default
			// File/attachment/page not found -> ignored

			StringBuilder buffer = new StringBuilder(raw);

			KeyValuePair<int, string> block = FindAndRemoveFirstOccurrence(buffer);

			while(block.Key != -1) {
				string blockHash = "DownCount-" + block.Value.ToString();

				string result = null;

				if(System.Web.HttpContext.Current != null) {
					result = System.Web.HttpContext.Current.Cache[blockHash] as string;
				}

				if(result == null) {
					XmlDocument doc = new XmlDocument();
					doc.LoadXml(block.Value);

					string pattern;
					DateTime startDate;
					GetRootAttributes(doc, out pattern, out startDate);

					double downloads = CountAllDownloads(doc);

					double timeSpanInDays = (DateTime.Now - startDate).TotalDays;

					int dailyDownloads = (int)Math.Round(downloads / timeSpanInDays);
					int weeklyDownloads = (int)Math.Round(downloads / (timeSpanInDays / 7D));
					int monthlyDownloads = (int)Math.Round(downloads / (timeSpanInDays / 30D));

					result = BuildResult(pattern, (int)downloads, dailyDownloads, weeklyDownloads, monthlyDownloads);

					if(System.Web.HttpContext.Current != null) {
						System.Web.HttpContext.Current.Cache.Add(blockHash, result, null, DateTime.Now.AddMinutes(10),
							System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
					}
				}

				buffer.Insert(block.Key, result);

				block = FindAndRemoveFirstOccurrence(buffer);
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Builds the result.
		/// </summary>
		/// <param name="pattern">The result pattern.</param>
		/// <param name="downloads">The downloads.</param>
		/// <param name="daily">The daily downloads.</param>
		/// <param name="weekly">The weekly downloads.</param>
		/// <param name="monthly">The monthly downloads.</param>
		/// <returns>The result.</returns>
		private static string BuildResult(string pattern, int downloads, int daily, int weekly, int monthly) {
			StringBuilder buffer = new StringBuilder(pattern);

			ReplacePlaceholder(buffer, CountPlaceholder, downloads.ToString());
			ReplacePlaceholder(buffer, DailyPlaceholder, daily.ToString());
			ReplacePlaceholder(buffer, WeeklyPlaceholder, weekly.ToString());
			ReplacePlaceholder(buffer, MonthlyPlaceholder, monthly.ToString());

			return buffer.ToString();
		}

		/// <summary>
		/// Replaces a placeholder with its value.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="placeholder">The placeholder.</param>
		/// <param name="value">The value.</param>
		private static void ReplacePlaceholder(StringBuilder buffer, string placeholder, string value) {
			int index = -1;

			do {
				index = buffer.ToString().ToLowerInvariant().IndexOf(placeholder);
				if(index != -1) {
					buffer.Remove(index, placeholder.Length);
					buffer.Insert(index, value);
				}
			} while(index != -1);
		}

		/// <summary>
		/// Gets the root attributes.
		/// </summary>
		/// <param name="doc">The XML document.</param>
		/// <param name="pattern">The pattern.</param>
		/// <param name="startDate">The start date/time.</param>
		private static void GetRootAttributes(XmlDocument doc, out string pattern, out DateTime startDate) {
			XmlNodeList root = doc.GetElementsByTagName("countDownloads");

			pattern = TryGetAttribute(root[0], "pattern");
			string startDateTemp = TryGetAttribute(root[0], "startDate");

			if(!DateTime.TryParseExact(startDateTemp, "yyyy'/'MM'/'dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out startDate)) {
				startDate = DefaultStartDate;
			}
		}

		/// <summary>
		/// Tries to get the value of an attribute.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="attribute">The name of the attribute.</param>
		/// <returns>The value of the attribute or <c>null</c> if no value is available.</returns>
		private static string TryGetAttribute(XmlNode node, string attribute) {
			XmlAttribute attr = node.Attributes[attribute];
			if(attr != null) return attr.Value;
			else return null;
		}

		/// <summary>
		/// Counts all the downloads.
		/// </summary>
		/// <param name="doc">The XML document.</param>
		/// <returns>The download count.</returns>
		private int CountAllDownloads(XmlDocument doc) {
			XmlNodeList files = doc.GetElementsByTagName("file");
			XmlNodeList attachments = doc.GetElementsByTagName("attachment");

			int count = 0;

			foreach(XmlNode node in files) {
				string name = TryGetAttribute(node, "name");
				string provider = TryGetAttribute(node, "provider");

				count += CountDownloads(name, provider);
			}

			foreach(XmlNode node in attachments) {
				string name = TryGetAttribute(node, "name");
				string page = TryGetAttribute(node, "page");
				string provider = TryGetAttribute(node, "provider");

				count += CountDownloads(name, page, provider);
			}

			return count;
		}

		/// <summary>
		/// Counts the downloads of a file.
		/// </summary>
		/// <param name="fullFilePath">The full file path.</param>
		/// <param name="providerName">The provider or <c>null</c> or <b>string.Empty</b>.</param>
		/// <returns>The downloads.</returns>
		private int CountDownloads(string fullFilePath, string providerName) {
			if(string.IsNullOrEmpty(fullFilePath)) return 0;

			IFilesStorageProviderV30 provider = GetProvider(providerName);
			if(provider == null) return 0;

			if(!fullFilePath.StartsWith("/")) fullFilePath = "/" + fullFilePath;

			string directory = StDirectoryInfo.GetDirectory(fullFilePath);
			StFileInfo[] files = _host.ListFiles(new StDirectoryInfo(directory, provider));

			fullFilePath = fullFilePath.ToLowerInvariant();

			foreach(StFileInfo file in files) {
				if(file.FullName.ToLowerInvariant() == fullFilePath) {
					return file.RetrievalCount;
				}
			}

			LogWarning("File " + provider.GetType().FullName + fullFilePath + " not found");
			return 0;
		}

		/// <summary>
		/// Counts the downloads of a file.
		/// </summary>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <param name="pageName">The full name of the page.</param>
		/// <param name="providerName">The provider or <c>null</c> or <b>string.Empty</b>.</param>
		/// <returns>The downloads.</returns>
		private int CountDownloads(string attachmentName, string pageName, string providerName) {
			if(string.IsNullOrEmpty(attachmentName)) return 0;
			if(string.IsNullOrEmpty(pageName)) return 0;

			PageInfo page = _host.FindPage(pageName);
			if(page == null) {
				LogWarning("Page " + pageName + " not found");
				return 0;
			}

			IFilesStorageProviderV30 provider = GetProvider(providerName);
			if(provider == null) return 0;

			StFileInfo[] attachments = _host.ListPageAttachments(page);

			attachmentName = attachmentName.ToLowerInvariant();

			foreach(StFileInfo attn in attachments) {
				if(attn.FullName.ToLowerInvariant() == attachmentName) {
					return attn.RetrievalCount;
				}
			}

			LogWarning("Attachment " + provider.GetType().FullName + "(" + pageName + ") " + attachmentName + " not found");
			return 0;
		}

		/// <summary>
		/// Gets the specified provider or the default one.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns></returns>
		private IFilesStorageProviderV30 GetProvider(string provider) {
			if(string.IsNullOrEmpty(provider)) provider = _host.GetSettingValue(SettingName.DefaultFilesStorageProvider);
			provider = provider.ToLowerInvariant();

			IFilesStorageProviderV30[] all = _host.GetFilesStorageProviders(true);
			foreach(IFilesStorageProviderV30 prov in all) {
				if(prov.GetType().FullName.ToLowerInvariant() == provider) return prov;
			}

			LogWarning("Provider " + provider + " not found");
			return null;
		}

		/// <summary>
		/// Finds and removes the first occurrence of the XML markup.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns>The index-content data.</returns>
		private static KeyValuePair<int, string> FindAndRemoveFirstOccurrence(StringBuilder buffer) {
			Match match = XmlRegex.Match(buffer.ToString());

			if(match.Success) {
				buffer.Remove(match.Index, match.Length);

				return new KeyValuePair<int, string>(match.Index, match.Value);
			}

			return new KeyValuePair<int, string>(-1, null);
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
			get { return "Specify <i>nolog</i> for disabling warning log messages for non-existent files or attachments."; }
		}

	}

}
