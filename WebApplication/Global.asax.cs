
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Text;

namespace ScrewTurn.Wiki {

	public class Global : System.Web.HttpApplication {

		protected void Application_Start(object sender, EventArgs e) {
			// Nothing to do (see Application_BeginRequest).
		}

		protected void Session_Start(object sender, EventArgs e) {
			// Increment # of online users and setup a new breadcrumbs manager
			// TODO: avoid to increment # of online users when session is not InProc
			ScrewTurn.Wiki.Cache.OnlineUsers++;
		}

		protected void Application_BeginRequest(object sender, EventArgs e) {
			if(Application["StartupOK"] == null) {
				Application.Lock();
				if(Application["StartupOK"] == null) {
					// Setup Resource Exchanger
					ScrewTurn.Wiki.Exchanger.ResourceExchanger = new ScrewTurn.Wiki.ResourceExchanger();
					ScrewTurn.Wiki.StartupTools.Startup();

					// All is OK, proceed with normal startup operations
					Application["StartupOK"] = "OK";
				}
				Application.UnLock();
			}

			ScrewTurn.Wiki.UrlTools.RouteCurrentRequest();
		}

		protected void Application_AcquireRequestState(object sender, EventArgs e) {
			if(HttpContext.Current.Session != null) {
				// This should be performed on EndRequest, but Session is not available there
				SessionCache.ClearData(HttpContext.Current.Session.SessionID);

				// Try to automatically login the user through the cookie
				ScrewTurn.Wiki.LoginTools.TryAutoLogin();
			}
		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e) {
			// Nothing to do
		}

		/// <summary>
		/// Logs an error.
		/// </summary>
		/// <param name="ex">The error.</param>
		private void LogError(Exception ex) {
			//if(ex.InnerException != null) ex = ex.InnerException;
			try {
				ScrewTurn.Wiki.Log.LogEntry(Tools.GetCurrentUrlFixed() + "\n" +
					ex.Source + " thrown " + ex.GetType().FullName + "\n" + ex.Message + "\n" + ex.StackTrace,
					ScrewTurn.Wiki.PluginFramework.EntryType.Error, ScrewTurn.Wiki.Log.SystemUsername);
			}
			catch { }
		}

		protected void Application_Error(object sender, EventArgs e) {
			// Retrieve last error and log it, redirecting to Error.aspx (avoiding infinite loops)

			Exception ex = Server.GetLastError();

			HttpException httpEx = ex as HttpException;
			if(httpEx != null) {
				// Try to redirect an inexistent .aspx page to a probably existing .ashx page
				if(httpEx.GetHttpCode() == 404) {
					string page = System.IO.Path.GetFileNameWithoutExtension(Request.PhysicalPath);
					ScrewTurn.Wiki.UrlTools.Redirect(page + ScrewTurn.Wiki.Settings.PageExtension);
					return;
				}
			}

			LogError(ex);
			string url = "";
			try {
				url = Tools.GetCurrentUrlFixed();
			}
			catch { }
			EmailTools.NotifyError(ex, url);
			Session["LastError"] = Server.GetLastError();
			if(!Request.PhysicalPath.ToLowerInvariant().Contains("error.aspx")) ScrewTurn.Wiki.UrlTools.Redirect("Error.aspx");
		}

		protected void Session_End(object sender, EventArgs e) {
			// Decrement # of online users (only works when session is InProc)
			ScrewTurn.Wiki.Cache.OnlineUsers--;
		}

		protected void Application_End(object sender, EventArgs e) {
			// Try to cleanly shutdown the application and providers
			ScrewTurn.Wiki.StartupTools.Shutdown();
		}

	}

}
