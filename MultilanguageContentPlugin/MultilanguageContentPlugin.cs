
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.PluginPack {

	/// <summary>
	/// Implements a Formatter Provider that allows to write multi-language content in Wiki Pages.
	/// </summary>
	public class MultilanguageContentPlugin : IFormatterProviderV30 {

		private IHostV30 host;
		private string config;
		private ComponentInformation info = new ComponentInformation("Multilanguage Content Plugin", "Threeplicate Srl", "3.0.1.472", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/PluginPack/Multilanguage2.txt");

		private string defaultLanguage = "en-us";
		private bool displayWarning = false;

		private const string DivStyle = "padding: 2px; margin-bottom: 10px; font-size: 11px; background-color: #FFFFC4; border: solid 1px #DDDDDD;";
		private const string StandardMessage = @"<div style=""" + DivStyle + @""">The content of this Page is localized in your language. To change the language settings, please go to the <a href=""Language.aspx"">Language Selection</a> page.</div>";
		private const string NotLocalizedMessage = @"<div style=""" + DivStyle + @""">The content of this Page is <b>not</b> available in your language, and it is displayed in the default language of the Wiki. To change the language settings, please go to the <a href=""Language.aspx"">Language Selection</a> page.</div>";

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
		/// Performs a Formatting phase.
		/// </summary>
		/// <param name="raw">The raw content to Format.</param>
		/// <param name="context">The Context information.</param>
		/// <param name="phase">The Phase.</param>
		/// <returns>The Formatted content.</returns>
		public string Format(string raw, ContextInformation context, FormattingPhase phase) {
			string result = ExtractLocalizedContent(context.Language, raw); // Try to load localized content
			bool notLocalized = false;
			bool noLocalization = false;
			if(result == null) {
				result = ExtractLocalizedContent(defaultLanguage, raw); // Load content in the default language
				notLocalized = true;
				noLocalization = false;
			}
			if(result == null) {
				result = raw; // The Page is not localized, return all the content
				notLocalized = false;
				noLocalization = true;
			}
			if(displayWarning && !noLocalization && context.Page != null) {
				if(notLocalized) return NotLocalizedMessage + result;
				else return StandardMessage + result;
			}
			else return result;
		}

		private string ExtractLocalizedContent(string language, string content) {
			string head = "\\<" + language + "\\>", tail = "\\<\\/" + language + "\\>";
			Regex regex = new Regex(head + "(.+?)" + tail, RegexOptions.IgnoreCase | RegexOptions.Singleline);
			Match match = regex.Match(content);
			StringBuilder sb = new StringBuilder(1000);
			while(match.Success) {
				sb.Append(match.Groups[1].Value);
				match = regex.Match(content, match.Index + match.Length);
			}
			if(sb.Length > 0) return sb.ToString();
			else return null;
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <remarks>If the configuration string is not valid, the methoud should throw a <see cref="InvalidConfigurationException"/>.</remarks>
		public void Init(IHostV30 host, string config) {
			this.host = host;
			this.config = config != null ? config : "";
			defaultLanguage = host.GetSettingValue(SettingName.DefaultLanguage);
			displayWarning = config.ToLowerInvariant().Equals("display warning");
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() { }

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets the execution priority of the provider (0 lowest, 100 highest).
		/// </summary>
		public int ExecutionPriority {
			get { return 50; }
		}

		/// <summary>
		/// Prepares the title of an item for display (always during phase 3).
		/// </summary>
		/// <param name="title">The input title.</param>
		/// <param name="context">The context information.</param>
		/// <returns>The prepared title (no markup allowed).</returns>
		public string PrepareTitle(string title, ContextInformation context) {
			string result = ExtractLocalizedContent(context.Language, title); // Try to load localized content
			if(context.ForIndexing || result == null) {
				result = ExtractLocalizedContent(defaultLanguage, title); // Load content in the default language
			}
			if(result == null) {
				result = title; // The Page is not localized, return all the content
			}
			return result;
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return "Specify 'display warning' to notify the user of the available content languages."; }
		}

	}

}
