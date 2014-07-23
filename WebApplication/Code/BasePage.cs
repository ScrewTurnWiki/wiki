
using System;
using System.Configuration;
using System.Globalization;
using System.IO.Compression;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public class BasePage : Page {

		public BasePage() {
		}

		protected override void OnInit(EventArgs e) {
			base.OnInit(e);

			// Mitigate Cross-Site Request Forgery (CSRF/XSRF) attacks
			ViewStateUserKey = Session.SessionID;
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);

			// Bypass compression if the current request was made by Anthem.NET
			if(HttpContext.Current.Request["Anthem_CallBack"] != null) return;

			// Request might not be initialized -> use HttpContext
			string ua = HttpContext.Current.Request.UserAgent != null ? HttpContext.Current.Request.UserAgent.ToLowerInvariant() : "";
			if(Settings.EnableHttpCompression && !ua.Contains("konqueror") && !ua.Contains("safari")) {
				if(Request.Headers["Accept-encoding"] != null && Request.Headers["Accept-encoding"].Contains("gzip")) {
					Response.Filter = new GZipStream(Response.Filter, CompressionMode.Compress, true);
					Response.AppendHeader("Content-encoding", "gzip");
					Response.AppendHeader("Vary", "Content-encoding");
					//Response.Write("HTTP Compression Enabled (GZip)");
				}
				else if(Request.Headers["Accept-encoding"] != null && Request.Headers["Accept-encoding"].Contains("deflate")) {
					Response.Filter = new DeflateStream(Response.Filter, CompressionMode.Compress, true);
					Response.AppendHeader("Content-encoding", "deflate");
					Response.AppendHeader("Vary", "Content-encoding");
					//Response.Write("HTTP Compression Enabled (Deflate)");
				}
			}
		}

		protected override void InitializeCulture() {
			// First, look for hard-stored user preferences
			// If they are not available, look at the cookie

			string culture = Preferences.LoadLanguageFromUserData();
			if(culture == null) culture = Preferences.LoadLanguageFromCookie();

			if(culture != null) {
				Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
			}
			else {
				try {
					if(Settings.DefaultLanguage.Equals("-")) {
						Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
						Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
					}
					else {
						Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.DefaultLanguage);
						Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.DefaultLanguage);
					}
				}
				catch {
					Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
					Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
				}
			}
			//Response.Write("Culture: " + Thread.CurrentThread.CurrentCulture.Name + "<br />");
			//Response.Write("UICulture: " + Thread.CurrentThread.CurrentUICulture.Name + "</br >");
		}

		/// <summary>
		/// Detects the correct <see cref="T:PageInfo" /> object associated to the current page using the <b>Page</b> and <b>NS</b> parameters in the query string.
		/// </summary>
		/// <param name="loadDefault"><c>true</c> to load the default page of the specified namespace when <b>Page</b> is not specified, <c>false</c> otherwise.</param>
		/// <returns>If <b>Page</b> is specified and exists, the correct <see cref="T:PageInfo" />, otherwise <c>null</c> if <b>loadDefault</b> is <c>false</c>,
		/// or the <see cref="T:PageInfo" /> object representing the default page of the specified namespace if <b>loadDefault</b> is <c>true</c>.</returns>
		protected PageInfo DetectPageInfo(bool loadDefault) {
			return Tools.DetectCurrentPageInfo(loadDefault);
		}

		/// <summary>
		/// Detects the full name of the current page using the <b>Page</b> and <b>NS</b> parameters in the query string.
		/// </summary>
		/// <returns>The full name of the page, regardless of the existence of the page.</returns>
		protected string DetectFullName() {
			return Tools.DetectCurrentFullName();
		}

		/// <summary>
		/// Detects the correct <see cref="T:NamespaceInfo" /> object associated to the current namespace using the <b>NS</b> parameter in the query string.
		/// </summary>
		/// <returns>The correct <see cref="T:NamespaceInfo" /> object, or <c>null</c>.</returns>
		protected NamespaceInfo DetectNamespaceInfo() {
			return Tools.DetectCurrentNamespaceInfo();
		}

		/// <summary>
		/// Detects the name of the current namespace using the <b>NS</b> parameter in the query string.
		/// </summary>
		/// <returns>The name of the namespace, or an empty string.</returns>
		protected string DetectNamespace() {
			return Tools.DetectCurrentNamespace();
		}

	}

}
