
using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Globalization;
using ScrewTurn.Wiki.PluginFramework;
using System.Reflection;
using System.Net;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains useful Tools.
	/// </summary>
	public static class Tools {

		/// <summary>
		/// Gets all the included files for the HTML Head, such as CSS, JavaScript and Icon pluginAssemblies, for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The includes.</returns>
		public static string GetIncludes(string nspace) {
			string themePath = Settings.GetThemePath(nspace);

			StringBuilder result = new StringBuilder(300);

			result.Append(GetJavaScriptIncludes());

			string themeDir = Settings.ThemesDirectory + Settings.GetTheme(nspace);
			if(!Directory.Exists(themeDir)) themeDir = Settings.ThemesDirectory + "Default";

			string[] css = Directory.GetFiles(themeDir, "*.css");
			string firstChunk;
			for(int i = 0; i < css.Length; i++) {
				if(Path.GetFileName(css[i]).IndexOf("_") != -1) {
					firstChunk = Path.GetFileName(css[i]).Substring(0, Path.GetFileName(css[i]).IndexOf("_")).ToLowerInvariant();
					if(firstChunk.Equals("screen") || firstChunk.Equals("print") || firstChunk.Equals("all") ||
						firstChunk.Equals("aural") || firstChunk.Equals("braille") || firstChunk.Equals("embossed") ||
						firstChunk.Equals("handheld") || firstChunk.Equals("projection") || firstChunk.Equals("tty") || firstChunk.Equals("tv")) {
						result.Append(@"<link rel=""stylesheet"" media=""" + firstChunk + @""" href=""" + themePath + Path.GetFileName(css[i]) + @""" type=""text/css"" />" + "\n");
					}
					else {
						result.Append(@"<link rel=""stylesheet"" href=""" + themePath + Path.GetFileName(css[i]) + @""" type=""text/css"" />" + "\n");
					}
				}
				else {
					result.Append(@"<link rel=""stylesheet"" href=""" + themePath + Path.GetFileName(css[i]) + @""" type=""text/css"" />" + "\n");
				}
			}

			string customEditorCss = Path.Combine(themeDir, "Editor.css");
			if(File.Exists(customEditorCss)) result.AppendFormat(@"<link rel=""stylesheet"" href=""{0}Editor.css"" type=""text/css"" />" + "\n", themePath);
			else result.Append(@"<link rel=""stylesheet"" href=""Themes/Editor.css"" type=""text/css"" />" + "\n");

			// OpenSearch
			result.AppendFormat(@"<link rel=""search"" href=""Search.aspx?OpenSearch=1"" type=""application/opensearchdescription+xml"" title=""{1}"" />",
				Settings.MainUrl, Settings.WikiTitle + " - Search");

			string[] js = Directory.GetFiles(themeDir, "*.js");
			for(int i = 0; i < js.Length; i++) {
				result.Append(@"<script src=""" + themePath + Path.GetFileName(js[i]) + @""" type=""text/javascript""></script>" + "\n");
			}

			string[] icons = Directory.GetFiles(themeDir, "Icon.*");
			if(icons.Length > 0) {
				result.Append(@"<link rel=""shortcut icon"" href=""" + themePath + Path.GetFileName(icons[0]) + @""" type=""");
				switch(Path.GetExtension(icons[0]).ToLowerInvariant()) {
					case ".ico":
						result.Append("image/x-icon");
						break;
					case ".gif":
						result.Append("image/gif");
						break;
					case ".png":
						result.Append("image/png");
						break;
				}
				result.Append(@""" />" + "\n");
			}

			// Include HTML Head
			result.Append(Settings.Provider.GetMetaDataItem(MetaDataItem.HtmlHead, nspace));

			return result.ToString();
		}

		/// <summary>
		/// Gets all the JavaScript files to include.
		/// </summary>
		/// <returns>The JS files.</returns>
		public static string GetJavaScriptIncludes() {
			StringBuilder buffer = new StringBuilder(100);

			foreach(string js in Directory.GetFiles(Settings.JsDirectory, "*.js")) {
				buffer.Append(@"<script type=""text/javascript"" src=""" + Settings.JsDirectoryName + "/" + Path.GetFileName(js) + @"""></script>" + "\n");
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Gets the canonical URL tag for a page.
		/// </summary>
		/// <param name="requestUrl">The request URL.</param>
		/// <param name="currentPage">The current page.</param>
		/// <param name="nspace">The namespace.</param>
		/// <returns>The canonical URL, or an empty string if <paramref name="requestUrl"/> is already canonical.</returns>
		public static string GetCanonicalUrlTag(string requestUrl, PageInfo currentPage, NamespaceInfo nspace) {
			string url = "";
			if(nspace == null && currentPage.FullName == Settings.DefaultPage) url = Settings.GetMainUrl().ToString();
			else url = Settings.GetMainUrl().ToString().TrimEnd('/') + "/" + currentPage.FullName + Settings.PageExtension;

			// Case sensitive
			if(url == requestUrl) return "";
			else return "<link rel=\"canonical\" href=\"" + url + "\" />";
		}

		/// <summary>
		/// Converts a byte number into a string, formatted using KB, MB or GB.
		/// </summary>
		/// <param name="bytes">The # of bytes.</param>
		/// <returns>The formatted string.</returns>
		public static string BytesToString(long bytes) {
			if(bytes < 1024) return bytes.ToString() + " B";
			else if(bytes < 1048576) return string.Format("{0:N2} KB", (float)bytes / 1024F);
			else if(bytes < 1073741824) return string.Format("{0:N2} MB", (float)bytes / 1048576F);
			else return string.Format("{0:N2} GB", (float)bytes / 1073741824F);
		}

		/// <summary>
		/// Computes the Disk Space Usage of a directory.
		/// </summary>
		/// <param name="dir">The directory.</param>
		/// <returns>The used Disk Space, in bytes.</returns>
		public static long DiskUsage(string dir) {
			string[] files = Directory.GetFiles(dir);
			string[] directories = Directory.GetDirectories(dir);
			long result = 0;

			FileInfo file;
			for(int i = 0; i < files.Length; i++) {
				file = new FileInfo(files[i]);
				result += file.Length;
			}
			for(int i = 0; i < directories.Length; i++) {
				result += DiskUsage(directories[i]);
			}
			return result;
		}

		/// <summary>
		/// Generates the standard 5-digit Page Version string.
		/// </summary>
		/// <param name="version">The Page version.</param>
		/// <returns>The 5-digit Version string.</returns>
		public static string GetVersionString(int version) {
			string result = version.ToString();
			int len = result.Length;
			for(int i = 0; i < 5 - len; i++) {
				result = "0" + result;
			}
			return result;
		}

		/// <summary>
		/// Gets the available Themes.
		/// </summary>
		public static string[] AvailableThemes {
			get {
				string[] dirs = Directory.GetDirectories(Settings.ThemesDirectory);
				string[] res = new string[dirs.Length];
				for(int i = 0; i < dirs.Length; i++) {
					//if(dirs[i].EndsWith("\\")) dirs[i] = dirs[i].Substring(0, dirs[i].Length - 1);
					dirs[i] = dirs[i].TrimEnd(Path.DirectorySeparatorChar);
					res[i] = dirs[i].Substring(dirs[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
				}
				return res;
			}
		}

		/// <summary>
		/// Gets the available Cultures.
		/// </summary>
		public static string[] AvailableCultures {
			get {
				// It seems, at least in VS 2008, that for Precompiled Web Sites, the GlobalResources pluginAssemblies that are not the
				// default resource (Culture=neutral), get sorted into subdirectories named by the Culture Info name.  Every
				// assembly in these directories is called "ScrewTurn.Wiki.resources.dll"

				// I'm sure it's possible to just use the subdirectory names in the bin directory to get the culture info names,
				// however, I'm not sure what other things might get tossed in there by the compiler now or in the future.
				// That's why I'm specifically going for the App_GlobalResources.resources.dlls.

				// So, get all of the App_GlobalResources.resources.dll pluginAssemblies from bin and recurse subdirectories
				string[] dllFiles = Directory.GetFiles(Path.Combine(Settings.RootDirectory, "bin"), "ScrewTurn.Wiki.resources.dll", SearchOption.AllDirectories);
				// List to collect constructed culture names
				List<string> cultureNames = new List<string>();

				// Manually add en-US culture
				CultureInfo enCI = new CultureInfo("en-US");
				cultureNames.Add(enCI.Name + "|" + UppercaseInitial(enCI.NativeName) + " - " + enCI.EnglishName);

				// For every file we find
				// List format: xx-ZZ|Native name (English name)
				foreach(string s in dllFiles) {
					try {
						// Load a reflection only assembly from the filename
						Assembly asm = Assembly.ReflectionOnlyLoadFrom(s);
						// string for destructive parsing of the assembly's full name
						// Which, btw, looks something like this
						// App_GlobalResources.resources, Version=0.0.0.0, Culture=zh-cn, PublicKeyToken=null
						string fullName = asm.FullName;
						// Find the Culture= attribute
						int find = fullName.IndexOf("Culture=");
						// Remove it and everything prior
						fullName = fullName.Substring(find + 8);
						// Find the trailing comma
						find = fullName.IndexOf(',');
						// Remove it and everything after
						fullName = fullName.Substring(0, find);
						// Fullname should now be the culture info name and we can instantiate the CultureInfo class from it
						CultureInfo ci = new CultureInfo(fullName);
						// StringBuilders
						StringBuilder sb = new StringBuilder();
						sb.Append(ci.Name);
						sb.Append("|");
						sb.Append(UppercaseInitial(ci.NativeName));
						sb.Append(" - ");
						sb.Append(ci.EnglishName);
						// Add the newly constructed Culture string
						cultureNames.Add(sb.ToString());
					}
					catch(Exception ex) {
						Log.LogEntry("Error parsing culture info from " + s + Environment.NewLine + ex.Message, EntryType.Error, Log.SystemUsername);
					}
				}

				// If for whatever reason every one fails, this will return a 1 element array with the en-US info.
				cultureNames.Sort();
				return cultureNames.ToArray();
			}
		}

		private static string UppercaseInitial(string value) {
			if(value.Length > 0) {
				return value[0].ToString().ToUpper(CultureInfo.CurrentCulture) + value.Substring(1);
			}
			else return "";
		}

		/// <summary>
		/// Gets the current culture.
		/// </summary>
		public static string CurrentCulture {
			get { return CultureInfo.CurrentUICulture.Name; }
		}

		/// <summary>
		/// Get the direction of the current culture.
		/// </summary>
		/// <returns><c>true</c> if the current culture is RTL, <c>false</c> otherwise.</returns>
		public static bool IsRightToLeftCulture() {
			return new CultureInfo(CurrentCulture).TextInfo.IsRightToLeft;
		}

		/// <summary>
		/// Computes the Hash of a Username, mixing it with other data, in order to avoid illegal Account activations.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="email">The email.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <returns>The secured Hash of the Username.</returns>
		public static string ComputeSecurityHash(string username, string email, DateTime dateTime) {
			return Hash.ComputeSecurityHash(username, email, dateTime, Settings.MasterPassword);
		}

		/// <summary>
		/// Escapes bad characters in a string (pipes and \n).
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The escaped string.</returns>
		public static string EscapeString(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("\r", "");
			sb.Replace("\n", "%0A");
			sb.Replace("|", "%7C");
			return sb.ToString();
		}

		/// <summary>
		/// Unescapes bad characters in a string (pipes and \n).
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The unescaped string.</returns>
		public static string UnescapeString(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("%7C", "|");
			sb.Replace("%0A", "\n");
			return sb.ToString();
		}
		
		/// <summary>
		/// Generates a random 10-char Password.
		/// </summary>
		/// <returns>The Password.</returns>
		public static string GenerateRandomPassword() {
			Random r = new Random();
			string password = "";
			for(int i = 0; i < 10; i++) {
				if(i % 2 == 0)
					password += ((char)r.Next(65, 91)).ToString(); // Uppercase letter
				else password += ((char)r.Next(97, 123)).ToString(); // Lowercase letter
			}
			return password;
		}

		/// <summary>
		/// Gets the approximate System Uptime.
		/// </summary>
		public static TimeSpan SystemUptime {
			get {
				int t = Environment.TickCount;
				if(t < 0) t = t + int.MaxValue;
				t = t / 1000;
				return TimeSpan.FromSeconds(t);
			}
		}

		/// <summary>
		/// Converts a Time Span to string.
		/// </summary>
		/// <param name="span">The Time Span.</param>
		/// <returns>The string.</returns>
		public static string TimeSpanToString(TimeSpan span) {
			string result = span.Days.ToString() + "d ";
			result += span.Hours.ToString() + "h ";
			result += span.Minutes.ToString() + "m ";
			result += span.Seconds.ToString() + "s";
			return result;
		}

		private static string CleanupPort(string url, string host) {
			if(!url.Contains(host)) {
				int colonIndex = host.IndexOf(":");
				if(colonIndex != -1) {
					host = host.Substring(0, colonIndex);
				}
			}
			
			return host;
		}

		/// <summary>
		/// Automatically replaces the host and port in the URL with those obtained from <see cref="Settings.GetMainUrl"/>.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <returns>The URL with fixed host and port.</returns>
		public static Uri FixHost(this Uri url) {
			// Make sure the host is replaced only once
			string originalUrl = url.ToString();
			string originalHost = url.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped);
			Uri mainUrl = Settings.GetMainUrl();
			string newHost = mainUrl.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped);

			originalHost = CleanupPort(originalUrl, originalHost);
			newHost = CleanupPort(mainUrl.ToString(), newHost);
			
			int hostIndex = originalUrl.IndexOf(originalHost);
			string newUrl = originalUrl.Substring(0, hostIndex) + newHost + originalUrl.Substring(hostIndex + originalHost.Length);

			return new Uri(newUrl);
		}

		/// <summary>
		/// Gets the current request's URL, with the host already fixed.
		/// </summary>
		/// <returns>The current URL.</returns>
		public static string GetCurrentUrlFixed() {
			return HttpContext.Current.Request.Url.FixHost().ToString();
		}

		/// <summary>
		/// Executes URL-encoding, avoiding to use '+' for spaces.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The encoded string.</returns>
		public static string UrlEncode(string input) {
			if(HttpContext.Current != null && HttpContext.Current.Server != null) return HttpContext.Current.Server.UrlEncode(input).Replace("+", "%20");
			else {
				Log.LogEntry("HttpContext.Current or HttpContext.Current.Server were null (Tools.UrlEncode)", EntryType.Warning, Log.SystemUsername);
				return input;
			}
		}

		/// <summary>
		/// Executes URL-decoding, replacing spaces as processed by UrlEncode.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The decoded string.</returns>
		public static string UrlDecode(string input) {
			return HttpContext.Current.Server.UrlDecode(input.Replace("%20", " "));
		}

		/// <summary>
		/// Removes all HTML tags from a text.
		/// </summary>
		/// <param name="html">The input HTML.</param>
		/// <returns>The extracted plain text.</returns>
		public static string RemoveHtmlMarkup(string html) {
			StringBuilder sb = new StringBuilder(System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " "));
			sb.Replace("&nbsp;", " ");
			sb.Replace("  ", " ");
			return sb.ToString();
		}

		/// <summary>
		/// Extracts the directory name from a path used in the Files Storage Providers.
		/// </summary>
		/// <param name="path">The path, for example '/folder/blah/'.</param>
		/// <returns>The directory name, for example 'blah'.</returns>
		public static string ExtractDirectoryName(string path) {
			path = path.Trim('/');

			int idx = path.LastIndexOf("/");
			return idx != -1 ? path.Substring(idx + 1) : path;
		}

		/// <summary>
		/// Detects the correct <see cref="T:PageInfo" /> object associated to the current page using the <b>Page</b> and <b>NS</b> parameters in the query string.
		/// </summary>
		/// <param name="loadDefault"><c>true</c> to load the default page of the specified namespace when <b>Page</b> is not specified, <c>false</c> otherwise.</param>
		/// <returns>If <b>Page</b> is specified and exists, the correct <see cref="T:PageInfo" />, otherwise <c>null</c> if <b>loadDefault</b> is <c>false</c>,
		/// or the <see cref="T:PageInfo" /> object representing the default page of the specified namespace if <b>loadDefault</b> is <c>true</c>.</returns>
		public static PageInfo DetectCurrentPageInfo(bool loadDefault) {
			string nspace = HttpContext.Current.Request["NS"];
			NamespaceInfo nsinfo = nspace != null ? Pages.FindNamespace(nspace) : null;

			string page = HttpContext.Current.Request["Page"];
			if(string.IsNullOrEmpty(page)) {
				if(loadDefault) {
					if(nsinfo == null) page = Settings.DefaultPage;
					else page = nsinfo.DefaultPage != null ? nsinfo.DefaultPage.FullName : "";
				}
				else return null;
			}

			string fullName = null;
			if(!page.StartsWith(nspace + ".")) fullName = nspace + "." + page;
			else fullName = page;

			fullName = fullName.Trim('.');

			return Pages.FindPage(fullName);
		}

		/// <summary>
		/// Detects the full name of the current page using the <b>Page</b> and <b>NS</b> parameters in the query string.
		/// </summary>
		/// <returns>The full name of the page, regardless of the existence of the page.</returns>
		public static string DetectCurrentFullName() {
			string nspace = HttpContext.Current.Request["NS"] != null ? HttpContext.Current.Request["NS"] : "";
			string page = HttpContext.Current.Request["Page"] != null ? HttpContext.Current.Request["Page"] : "";

			string fullName = null;
			if(!page.StartsWith(nspace + ".")) fullName = nspace + "." + page;
			else fullName = page;

			return fullName.Trim('.');
		}

		/// <summary>
		/// Detects the correct <see cref="T:NamespaceInfo" /> object associated to the current namespace using the <b>NS</b> parameter in the query string.
		/// </summary>
		/// <returns>The correct <see cref="T:NamespaceInfo" /> object, or <c>null</c>.</returns>
		public static NamespaceInfo DetectCurrentNamespaceInfo() {
			string nspace = HttpContext.Current.Request["NS"];
			NamespaceInfo nsinfo = nspace != null ? Pages.FindNamespace(nspace) : null;
			return nsinfo;
		}

		/// <summary>
		/// Detects the name of the current namespace using the <b>NS</b> parameter in the query string.
		/// </summary>
		/// <returns>The name of the namespace, or an empty string.</returns>
		public static string DetectCurrentNamespace() {
			return HttpContext.Current.Request["NS"] != null ? HttpContext.Current.Request["NS"] : "";
		}

		/// <summary>
		/// Gets the message ID for HTML anchors.
		/// </summary>
		/// <param name="messageDateTime">The message date/time.</param>
		/// <returns>The ID.</returns>
		public static string GetMessageIdForAnchor(DateTime messageDateTime) {
			return "MSG_" + messageDateTime.ToString("yyyyMMddHHmmss");
		}

		/// <summary>
		/// Gets the name of a file's directory.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <returns>The name of the item.</returns>
		public static string GetDirectoryName(string filename) {
			if(filename != null) {
				int index = filename.LastIndexOf("/");
				if(index > 0) {
					string directoryName = filename.Substring(0, index + 1);
					if(!directoryName.StartsWith("/")) directoryName = "/" + directoryName;
					return directoryName;
				}
			}
			
			// Assume to navigate in the root directory
			return "/";
		}

		/// <summary>
		/// Gets the update status of a component.
		/// </summary>
		/// <param name="url">The version file URL.</param>
		/// <param name="currentVersion">The current version.</param>
		/// <param name="newVersion">The new version, if any.</param>
		/// <param name="newAssemblyUrl">The URL of the new assembly, if applicable and available.</param>
		/// <returns>The update status.</returns>
		/// <remarks>This method only works in Full Trust.</remarks>
		public static UpdateStatus GetUpdateStatus(string url, string currentVersion, out string newVersion, out string newAssemblyUrl) {
			// TODO: Verify usage of WebPermission class
			// http://msdn.microsoft.com/en-us/library/system.net.webpermission.aspx

			string urlHash = "UpdUrlCache-" + url.GetHashCode().ToString();

			try {
				string ver = null;

				if(HttpContext.Current != null) {
					ver = HttpContext.Current.Cache[urlHash] as string;
				}

				if(ver == null) {
					HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
					req.AllowAutoRedirect = true;
					HttpWebResponse res = (HttpWebResponse)req.GetResponse();

					if(res.StatusCode != HttpStatusCode.OK) {
						newVersion = null;
						newAssemblyUrl = null;
						return UpdateStatus.Error;
					}

					StreamReader sr = new StreamReader(res.GetResponseStream());
					ver = sr.ReadToEnd();
					sr.Close();

					if(HttpContext.Current != null) {
						HttpContext.Current.Cache.Add(urlHash, ver, null, DateTime.Now.AddMinutes(5),
							System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
					}
				}

				string[] lines = ver.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if(lines.Length == 0) {
					newVersion = null;
					newAssemblyUrl = null;
					return UpdateStatus.Error;
				}

				string[] versions = lines[0].Split('|');
				bool upToDate = false;
				for(int i = 0; i < versions.Length; i++) {
					ver = versions[i];
					if(versions[i].Equals(currentVersion)) {
						if(i == versions.Length - 1) upToDate = true;
						else upToDate = false;
						ver = versions[versions.Length - 1];
						break;
					}
				}

				if(upToDate) {
					newVersion = null;
					newAssemblyUrl = null;
					return UpdateStatus.UpToDate;
				}
				else {
					newVersion = ver;

					if(lines.Length == 2) newAssemblyUrl = lines[1];
					else newAssemblyUrl = null;

					return UpdateStatus.NewVersionFound;
				}
			}
			catch(Exception) {
				if(HttpContext.Current != null) {
					HttpContext.Current.Cache.Add(urlHash, "", null, DateTime.Now.AddMinutes(5),
							System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
				}

				newVersion = null;
				newAssemblyUrl = null;
				return UpdateStatus.Error;
			}
		}

		/// <summary>
		/// Computes the hash value of a string that is value across application instances and versions.
		/// </summary>
		/// <param name="value">The string to compute the hash of.</param>
		/// <returns>The hash value.</returns>
		public static uint HashDocumentNameForTemporaryIndex(string value) {
			if(value == null) throw new ArgumentNullException("value");

			// sdbm algorithm, borrowed from http://www.cse.yorku.ca/~oz/hash.html
			uint hash = 0;

			foreach(char c in value) {
				// hash(i) = hash(i - 1) * 65599 + str[i]
				hash = c + (hash << 6) + (hash << 16) - hash;
			}

			return hash;
		}

		/// <summary>
		/// Obfuscates text, replacing each character with its HTML escaped sequence, for example a becomes <c>&amp;#97;</c>.
		/// </summary>
		/// <param name="input">The input text.</param>
		/// <returns>The output obfuscated text.</returns>
		public static string ObfuscateText(string input) {
			StringBuilder buffer = new StringBuilder(input.Length * 4);

			foreach(char c in input) {
				buffer.Append("&#" + ((int)c).ToString("D2") + ";");
			}

			return buffer.ToString();
		}

	}

	/// <summary>
	/// Lists legal update statuses.
	/// </summary>
	public enum UpdateStatus {
		/// <summary>
		/// Error while retrieving version information.
		/// </summary>
		Error,
		/// <summary>
		/// The component is up-to-date.
		/// </summary>
		UpToDate,
		/// <summary>
		/// A new version was found.
		/// </summary>
		NewVersionFound
	}

}
