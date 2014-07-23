using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.RatingManagerPlugin {

	/// <summary>
	/// A plugin for assigning a rating to pages.
	/// </summary>
	public class RatingManager : IFormatterProviderV30 {

		const string defaultDirectoryName = "/__RatingManagerPlugin/";
		const string cssFileName = "RatingManagerPluginCss.css";
		const string jsFileName = "RatingManagerPluginJs.js";
		const string starImageFileName = "RatingManagerPluginStarImage.gif";
		const string ratingFileName = "RatingManagerPluginRatingFile.dat";

		private IHostV30 _host;
		private bool _enableLogging = true;
        private static readonly ComponentInformation Info = new ComponentInformation("Rating Manager Plugin", "Threeplicate Srl", "3.0.3.555", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/PluginPack/RatingManager2.txt");

		private bool foundRatings = false;

		private static readonly Regex VotesRegex = new Regex(@"{rating(\|(.+?))?}",
			RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		/// <summary>
		/// Initializes a new instance of the <see cref="RatingManager"/> class.
		/// </summary>
		public RatingManager() {
			
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
		/// Performs a Formatting phase.
		/// </summary>
		/// <param name="raw">The raw content to Format.</param>
		/// <param name="context">The Context information.</param>
		/// <param name="phase">The Phase.</param>
		/// <returns>The Formatted content.</returns>
		public string Format(string raw, ContextInformation context, FormattingPhase phase) {
			// {rating}
			// _backendpage not found -> ignored

			StringBuilder buffer = new StringBuilder(raw);
			try {
				if(context.Context == FormattingContext.PageContent && context.Page != null) {
					if(context.HttpContext.Request["vote"] != null) {
						AddRating(context.Page.FullName, int.Parse(context.HttpContext.Request["vote"]));
						System.Web.HttpCookie cookie = new System.Web.HttpCookie("RatingManagerPlugin_" + context.Page.FullName, context.HttpContext.Request["vote"]);
						cookie.Expires = DateTime.Now.AddYears(10);
						context.HttpContext.Response.Cookies.Add(cookie);
						return "";
					}
				}
				if(context.Page != null) {
					ComputeRating(context, buffer, context.Page.FullName);
				}
				else {
					return raw;
				}
			}
			catch(Exception ex) {
				LogWarning(string.Format("Exception occurred: {0}", ex.StackTrace));
			}
			if(foundRatings) {
				buffer.Append(@"<script type=""text/javascript"" src=""GetFile.aspx?file=" + defaultDirectoryName + jsFileName + @"""></script>");
				buffer.Append(@"<link rel=""StyleSheet"" href=""GetFile.aspx?file=" + defaultDirectoryName + cssFileName + @""" type=""text/css"" />");
				buffer.Append(@"<script type=""text/javascript""> <!--
function GenerateStaticStars(rate, cssClass) {
var string = '';
var i = 0;
for (i=0; i<rate; i++) {
string +='<span class=""static-rating ' + cssClass + '""></span>';
}
for(i=rate; i<5; i++) {
string +='<span class=""static-rating ui-rating-empty""></span>';
}
return string;
}
//--> </script>");
				foundRatings = false;
			}
			return buffer.ToString();
		}

		/// <summary>
		/// Gets the rating of the plugin from the backendpage and display it to the user.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="buffer">The page content.</param>
		/// <param name="fullPageName">Full name of the page.</param>
		private void ComputeRating(ContextInformation context, StringBuilder buffer, string fullPageName) {
			KeyValuePair<int, Match> block = FindAndRemoveFirstOccurrence(buffer);
			int numRatings = 0;
			while(block.Key != -1) {
				foundRatings = true;
				numRatings++;

				string result = null;

				if(block.Value.Groups[2].Value != "") {
					int average = (int)Math.Round((decimal)GetCurrentAverage(block.Value.Groups[2].Value), 0, MidpointRounding.AwayFromZero);

					result += @"<span id=""staticStar" + numRatings + @""" class=""rating""></span>";

					result += @"<script type=""text/javascript""> <!--
$(document).ready(function() {
$('#staticStar" + numRatings + @"').html(GenerateStaticStars(" + average + @", 'ui-rating-full'));
});
//--> </script>";
				}
				else if(context.HttpContext.Request.Cookies.Get("RatingManagerPlugin_" + fullPageName) != null) {
					int average = (int)Math.Round((decimal)GetCurrentAverage(fullPageName), 0, MidpointRounding.AwayFromZero);

					result += @"<span id=""staticStar" + numRatings + @""" class=""rating""></span>";

					result += @"<script type=""text/javascript""> <!--
$(document).ready(function() {
$('#staticStar" + numRatings + @"').html(GenerateStaticStars(" + average + @", 'ui-rating-full'));
});
//--> </script>";
				}
				else {
					int average = (int)Math.Round((decimal)GetCurrentAverage(fullPageName), 0, MidpointRounding.AwayFromZero);

					result += @"<select name=""myRating"" class=""rating"" id=""serialStar" + numRatings + @""">  
									<option value=""1"">Alright</option>  
									<option value=""2"">Ok</option>  
									<option value=""3"">Getting Better</option>  
									<option value=""4"">Pretty Good</option>  
									<option value=""5"">Awesome</option>  
								</select>
								<span id=""staticStar" + numRatings + @""" style=""vertical-align: middle;""></span> <span id=""average" + numRatings + @""" style=""margin-left: 5px; font-weight: bold;""></span>";

					result += @"<script type=""text/javascript""> <!--
$(document).ready(function(){
var voting = true;
//Show that we can bind on the select box  
$('#serialStar" + numRatings + @"').bind(""change"", function(){
if(voting){
voting = false;
var vote = $('#serialStar" + numRatings + @"').val();
$.ajax({ url: '?vote=' + vote });
$('#serialStar" + numRatings + @"').remove();
$('.ui-rating').remove();
$('#staticStar" + numRatings + @"').html(GenerateStaticStars(vote, 'ui-rating-hover'));
$('#average" + numRatings + @"').html('Thanks!');
}
});
//Set the initial value
$('#serialStar" + numRatings + @"').rating({showCancel: false, startValue: " + average + @"});
});
//--> </script>";

				}

				result += @"";

				buffer.Insert(block.Key, result);

				block = FindAndRemoveFirstOccurrence(buffer);
			}
		}

		
		private float GetCurrentAverage(string fullPageName) {
			float average = 0;
			try {
				IFilesStorageProviderV30 filesStorageProvider = GetDefaultFilesStorageProvider();

				MemoryStream stream = new MemoryStream();
				string fileContent = "";

				if(FileExists(filesStorageProvider, defaultDirectoryName, ratingFileName)) {
					filesStorageProvider.RetrieveFile(defaultDirectoryName + ratingFileName, stream, true);
					stream.Seek(0, SeekOrigin.Begin);
					fileContent = Encoding.UTF8.GetString(stream.ToArray());
				}

				string[] plugins = fileContent.Split(new String[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

				// If the plugin is found return the posizion in the plugins array
				// otherwise return -1
				int pluginIndex = SearchPlugin(plugins, fullPageName);
				if(pluginIndex != -1) {
					string[] pluginDetails = plugins[pluginIndex].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
					average = (float)int.Parse(pluginDetails[2]) / (float)100;
				}
			}
			catch(Exception ex) {
				LogWarning(String.Format("Exception occurred {0}", ex.StackTrace));
			}
			return average;
		}


		private void AddRating(string fullPageName, int rate) {
			IFilesStorageProviderV30 filesStorageProvider = GetDefaultFilesStorageProvider();

			MemoryStream stream = new MemoryStream();

			if(FileExists(filesStorageProvider, defaultDirectoryName, ratingFileName)) {
				filesStorageProvider.RetrieveFile(defaultDirectoryName + ratingFileName, stream, true);
				stream.Seek(0, SeekOrigin.Begin);
			}
			string fileContent = Encoding.UTF8.GetString(stream.ToArray());

			string[] plugins = fileContent.Split(new String[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

			StringBuilder sb = new StringBuilder();

			// If the plugin is found return the posizion in the plugins array
			// otherwise return -1
			int pluginIndex = SearchPlugin(plugins, fullPageName);
			if(pluginIndex != -1) {
				int numRates = int.Parse(plugins[pluginIndex].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[1]);
				int average = int.Parse(plugins[pluginIndex].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[2]);
				int newAverage = ((average * numRates) + (rate * 100)) / (numRates + 1);
				numRates++;
				plugins[pluginIndex] = fullPageName + "|" + numRates + "|" + newAverage;
				foreach(string plugin in plugins) {
					sb.Append(plugin + "||");
				}
			}
			else {
				foreach(string plugin in plugins) {
					sb.Append(plugin + "||");
				}
				sb.Append(fullPageName + "|1|" + (rate * 100));
			}

			stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));

			filesStorageProvider.StoreFile(defaultDirectoryName + ratingFileName, stream, true);

			//statisticsPage.Provider.ModifyPage(statisticsPage, statisticsPageContent.Title, statisticsPageContent.User, DateTime.Now, statisticsPageContent.Comment, sb.ToString(), statisticsPageContent.Keywords, statisticsPageContent.Description, SaveMode.Normal);
		}

		/// <summary>
		/// Searches the plugin.
		/// </summary>
		/// <param name="plugins">The plugins array.</param>
		/// <param name="currentPlugin">The current plugin.</param>
		/// <returns>
		/// The position of the plugin in the <paramref name="plugins"/> array, otherwise -1
		/// </returns>
		private int SearchPlugin(string[] plugins, string currentPlugin) {
			for(int i = 0; i < plugins.Length; i++) {
				if(plugins[i].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[0] == currentPlugin)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Finds the and remove first occurrence.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns>The index->content data.</returns>
		private KeyValuePair<int, Match> FindAndRemoveFirstOccurrence(StringBuilder buffer) {
			Match match = VotesRegex.Match(buffer.ToString());

			if(match.Success) {
				buffer.Remove(match.Index, match.Length);
				return new KeyValuePair<int, Match>(match.Index, match);
			}

			return new KeyValuePair<int, Match>(-1, null);
		}

		/// <summary>
		/// Logs the warning.
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
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			_host = host;

			if(config != null) {
				string[] configEntries = config.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
				for(int i = 0; i < configEntries.Length; i++) {
					string[] configEntryDetails = configEntries[i].Split(new string[] { "=" }, 2, StringSplitOptions.None);
					switch(configEntryDetails[0].ToLowerInvariant()) {
						case "logoptions":
							if(configEntryDetails[1] == "nolog") {
								_enableLogging = false;
							}
							else {
								LogWarning(@"Unknown value in ""logOptions"" configuration string: " + configEntries[i] + "Supported values are: nolog.");
							}
							break;
						default:
							LogWarning("Unknown value in configuration string: " + configEntries[i]);
							break;
					}
				}
			}

			IFilesStorageProviderV30 filesStorageProvider = GetDefaultFilesStorageProvider();

			if(!DirectoryExists(filesStorageProvider, defaultDirectoryName)) {
				filesStorageProvider.CreateDirectory("/", defaultDirectoryName.Trim('/'));
			}
			if(!FileExists(filesStorageProvider, defaultDirectoryName, cssFileName)) {
				filesStorageProvider.StoreFile(defaultDirectoryName + cssFileName, Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.RatingManagerPlugin.Resources.jquery.rating.css"), true);
			}
			if(!FileExists(filesStorageProvider, defaultDirectoryName, jsFileName)) {
				filesStorageProvider.StoreFile(defaultDirectoryName + jsFileName, Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.RatingManagerPlugin.Resources.jquery.rating.pack.js"), true);
			}
			if(!FileExists(filesStorageProvider, defaultDirectoryName, starImageFileName)) {
				filesStorageProvider.StoreFile(defaultDirectoryName + starImageFileName, Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Plugins.RatingManagerPlugin.Resources.star.gif"), true);
			}
		}


		private IFilesStorageProviderV30 GetDefaultFilesStorageProvider() {
			string defaultFilesStorageProviderName = _host.GetSettingValue(SettingName.DefaultFilesStorageProvider);
			return _host.GetFilesStorageProviders(true).First(p => p.GetType().FullName == defaultFilesStorageProviderName);
		}

		private bool DirectoryExists(IFilesStorageProviderV30 filesStorageProvider, string directoryName) {
			string[] directoryList = filesStorageProvider.ListDirectories("/");
			foreach(string dir in directoryList) {
				if(dir == directoryName) return true;
			}
			return false;
		}

		private bool FileExists(IFilesStorageProviderV30 filesStorageProvider, string directory, string fileName) {
			string[] filesList = filesStorageProvider.ListFiles(directory);
			foreach(string file in filesList) {
				if(file == directory + fileName) return true;
			}
			return false;
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
			get { return "Specify <i>logooptions=nolog</i> for disabling warning log messages for exceptions."; }
		}
	}
}
