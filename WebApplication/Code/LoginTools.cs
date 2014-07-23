
using System;
using System.Collections.Generic;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	
	/// <summary>
	/// Implements login tools.
	/// </summary>
	public static class LoginTools {

		/// <summary>
		/// The login key.
		/// </summary>
		public const string LoginKey = "LoginKey";

		/// <summary>
		/// The username.
		/// </summary>
		public const string Username = "Username";

		/// <summary>
		/// A logout flag.
		/// </summary>
		public const string Logout = "Logout";

		/// <summary>
		/// Tries to automatically login the current user.
		/// </summary>
		public static void TryAutoLogin() {
			if(SessionFacade.LoginKey == null && HttpContext.Current.Request.Cookies[Settings.LoginCookieName] != null) {
				string username = HttpContext.Current.Request.Cookies[Settings.LoginCookieName].Values[Username];
				string key = HttpContext.Current.Request.Cookies[Settings.LoginCookieName].Values[LoginKey];

				// Try cookie login
				UserInfo user = Users.TryCookieLogin(username, key);
				if(user != null) {
					SetupSession(user);
					Log.LogEntry("User " + user.Username + " logged in through cookie", EntryType.General, Log.SystemUsername);
					TryRedirect(false);
				}
				else {
					// Cookie is not valid, delete it
					SetLoginCookie("", "", DateTime.Now.AddYears(-1));
					SetupSession(null);
				}
			}
			else if(SessionFacade.LoginKey == null && HttpContext.Current.Session[Logout] == null) { // Check for filtered autologin
				// If no cookie is available, try to autologin through providers
				UserInfo user = Users.TryAutoLogin(HttpContext.Current);
				if(user != null) {
					SetupSession(user);
					Log.LogEntry("User " + user.Username + " logged in via " + user.Provider.GetType().FullName + " autologin", EntryType.General, Log.SystemUsername);
					TryRedirect(false);
				}
			}
		}

		/// <summary>
		/// Sets up a user session.
		/// </summary>
		/// <param name="user">The user (<c>null</c> for anonymous).</param>
		public static void SetupSession(UserInfo user) {
			if(user != null) {
				SessionFacade.LoginKey = Users.ComputeLoginKey(user.Username, user.Email, user.DateTime);
				SessionFacade.CurrentUsername = user.Username;

				HttpContext.Current.Session[Logout] = null; // No session facade because this key is used only in this page
			}
			else {
				SessionFacade.LoginKey = null;
				SessionFacade.CurrentUsername = null;
			}
		}

		/// <summary>
		/// Tries to redirect the user to any specified URL.
		/// </summary>
		/// <param name="goHome">A value indicating whether to redirect to the home page if no explicit redirect URL is found.</param>
		public static void TryRedirect(bool goHome) {
			if(HttpContext.Current.Request["Redirect"] != null) {
				string target = HttpContext.Current.Request["Redirect"];
				if(target.StartsWith("http:") || target.StartsWith("https:")) HttpContext.Current.Response.Redirect(target);
				else UrlTools.Redirect(UrlTools.BuildUrl(target));
			}
			else if(goHome) UrlTools.Redirect(UrlTools.BuildUrl("Default.aspx"));
		}

		/// <summary>
		/// Sets the login cookie.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="loginKey">The login key.</param>
		/// <param name="expiration">The expiration date/time.</param>
		public static void SetLoginCookie(string username, string loginKey, DateTime expiration) {
			HttpCookie cookie = new HttpCookie(Settings.LoginCookieName);
			cookie.Expires = expiration;
			cookie.Path = Settings.CookiePath;
			cookie.Values.Add(LoginKey, loginKey);
			cookie.Values.Add(Username, username);
			HttpContext.Current.Response.Cookies.Add(cookie);
		}

		/// <summary>
		/// Verifies read permissions for the current user, redirecting to the appropriate page if no valid permissions are found.
		/// </summary>
		public static void VerifyReadPermissionsForCurrentNamespace() {
			string currentUsername = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();

			bool canViewNamespace = AuthChecker.CheckActionForNamespace(
				Tools.DetectCurrentNamespaceInfo(), Actions.ForNamespaces.ReadPages,
				currentUsername, currentGroups);

			if(!canViewNamespace) {
				if(SessionFacade.CurrentUsername == null) UrlTools.Redirect("Login.aspx?Redirect=" + Tools.UrlEncode(Tools.GetCurrentUrlFixed()));
				else UrlTools.Redirect("AccessDenied.aspx");
			}
		}

	}

}
