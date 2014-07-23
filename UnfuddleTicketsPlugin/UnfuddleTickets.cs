
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Xml;
using System.Xml.Xsl;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.PluginPack {

	/// <summary>
	/// Implements a formatter that display tickets from Unfuddle.
	/// </summary>
	public class UnfuddleTickets : IFormatterProviderV30 {

		private const string ConfigHelpHtmlValue = "Config consists of three lines:<br/><i>&lt;Url&gt;</i> - The base url to the Unfuddle API (i.e. http://account_name.unfuddle.com/api/v1/projects/project_ID)<br/><i>&lt;Username&gt;</i> - The username to the unfuddle account to use for authentication<br/><i>&lt;Password&gt;</i> - The password to the unfuddle account to use for authentication<br/>";
		private const string LoadErrorMessage = "Unable to load ticket report at this time.";
		private static readonly Regex UnfuddleRegex = new Regex(@"{unfuddle}", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly ComponentInformation Info = new ComponentInformation("Unfuddle Tickets Plugin", "Threeplicate Srl", "3.0.4.575", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/PluginPack/UnfuddleTickets2.txt");

		private string _config;
		private IHostV30 _host;
		private string _baseUrl;
		private string _username;
		private string _password;

		private XslCompiledTransform _xslTransform = null;

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <remarks>If the configuration string is not valid, the methoud should throw a <see cref="InvalidConfigurationException"/>.</remarks>
		public void Init(IHostV30 host, string config) {
			_host = host;
			_config = config ?? string.Empty;
			var configEntries = _config.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

			if(configEntries.Length != 3) throw new InvalidConfigurationException("Configuration is missing required parameters");

			_baseUrl = configEntries[0];

			if(_baseUrl.EndsWith("/"))
				_baseUrl = _baseUrl.Substring(0, _baseUrl.Length - 1);

			_username = configEntries[1];
			_password = configEntries[2];

			var settings = new XsltSettings {
				EnableScript = true,
				EnableDocumentFunction = true
			};
			_xslTransform = new XslCompiledTransform(true);
			using(var reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.PluginPack.Resources.UnfuddleTickets.xsl"))) {
				_xslTransform.Load(reader, settings, new XmlUrlResolver());
			}
		}

		/// <summary>
		/// Performs a Formatting phase.
		/// </summary>
		/// <param name="raw">The raw content to Format.</param>
		/// <param name="context">The Context information.</param>
		/// <param name="phase">The Phase.</param>
		/// <returns>The Formatted content.</returns>
		public string Format(string raw, ContextInformation context, FormattingPhase phase) {
			var buffer = new StringBuilder(raw);

			var block = FindAndRemoveFirstOccurrence(buffer);

			if(block.Key != -1) {
				string unfuddleTickets = null;
				if(HttpContext.Current != null) {
					unfuddleTickets = HttpContext.Current.Cache["UnfuddleTicketsStore"] as string;
				}

				if(string.IsNullOrEmpty(unfuddleTickets)) {
					unfuddleTickets = LoadUnfuddleTicketsFromWeb();
				}

				if(string.IsNullOrEmpty(unfuddleTickets)) {
					unfuddleTickets = LoadErrorMessage;
				}

				do {
					buffer.Insert(block.Key, unfuddleTickets);
					block = FindAndRemoveFirstOccurrence(buffer);
				} while(block.Key != -1);
			}

			return buffer.ToString();
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
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
			// Nothing to do
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return ConfigHelpHtmlValue; }
		}

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
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return Info; }
		}

		/// <summary>
		/// Finds and removes the first occurrence of the custom tag.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns>The index->content data.</returns>
		private static KeyValuePair<int, string> FindAndRemoveFirstOccurrence(StringBuilder buffer) {
			Match match = UnfuddleRegex.Match(buffer.ToString());

			if(match.Success) {
				buffer.Remove(match.Index, match.Length);

				return new KeyValuePair<int, string>(match.Index, match.Value);
			}

			return new KeyValuePair<int, string>(-1, null);
		}

		/// <summary>
		/// Builds an xml document from API calls to Unfuddle.com then runs them through an Xslt to format them.
		/// </summary>
		/// <returns>An html string that contains the tables to display the ticket information, or null</returns>
		private string LoadUnfuddleTicketsFromWeb() {
			var xml = BuildXmlFromApiCalls();
			if(xml == null) return null;

			string results;
			using(var sw = new StringWriter()) {
				using(var xnr = new XmlNodeReader(xml)) {
					_xslTransform.Transform(xnr, null, sw);
				}
				results = sw.ToString();
			}

			HttpContext.Current.Cache.Add("UnfuddleTicketsStore", results, null, DateTime.Now.AddMinutes(10),
				Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
			return results;
		}

		/// <summary>
		/// Builds 3 Xml Documents, the first two are lookups for Milestone, and People information, the second is the
		/// ticket information.
		/// </summary>
		/// <returns></returns>
		private XmlDocument BuildXmlFromApiCalls() {
			var milestones = GetXml("/milestones", _username, _password);
			if(milestones == null) {
				LogWarning("Exception occurred while pulling unfuddled ticket information from the API.");
				return null;
			}

			var people = GetXml("/people", _username, _password);
			if(people == null) {
				LogWarning("Exception occurred while pulling unfuddled ticket information from the API.");
				return null;
			}

			var tickets = GetXml("/ticket_reports/dynamic?sort_by=priority&sort_direction=DESC&conditions_string=status-neq-closed&group_by=priority&fields_string=number,priority,summary,milestone,status,version,description&formatted=true", _username, _password);
			if(tickets == null) {
				LogWarning("Exception occurred while pulling unfuddled ticket information from the API.");
				return null;
			}

			var results = new XmlDocument();
			results.AppendChild(results.CreateXmlDeclaration("1.0", "UTF-8", string.Empty));
			var element = results.CreateElement("root");
			results.AppendChild(element);
			element.AppendChild(results.ImportNode(milestones.ChildNodes[1], true));
			element.AppendChild(results.ImportNode(people.ChildNodes[1], true));
			element.AppendChild(results.ImportNode(tickets.ChildNodes[1], true));

			return results;
		}

		/// <summary>
		/// Produces an API call, then returns the results as an Xml Document
		/// </summary>
		/// <param name="Url">The Url to the specific API call</param>
		/// <param name="Username">An unfuddle account username</param>
		/// <param name="Password">The password to above unfuddle account</param>
		/// <returns></returns>
		private XmlDocument GetXml(string Url, string Username, string Password) {
			try {
				var results = new XmlDocument();
				Url = string.Format("{0}{1}", _baseUrl, Url);
				var request = WebRequest.Create(Url);
				request.Credentials = new NetworkCredential(Username, Password);
				var response = request.GetResponse();
				using(var reader = new StreamReader(response.GetResponseStream())) {
					var xmlString = reader.ReadToEnd();
					try {
						results.LoadXml(xmlString);
					}
					catch {
						LogWarning("Received Unexpected Response from Unfuddle Server.");
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
		/// Logs a warning.
		/// </summary>
		/// <param name="message">The message.</param>
		private void LogWarning(string message) {
			_host.LogEntry(message, LogEntryType.Warning, null, this);
		}

	}

}
