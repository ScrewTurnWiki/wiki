
using System;
using System.Web;

namespace ScrewTurn.Wiki
{
	using System.IO;

	public class Global : System.Web.HttpApplication
	{

		protected void Application_Start( object sender, EventArgs e )
		{
			// Nothing to do (see Application_BeginRequest).
		}

		protected void Session_Start( object sender, EventArgs e )
		{
			// Increment # of online users and setup a new breadcrumbs manager
			// TODO: avoid to increment # of online users when session is not InProc
			ScrewTurn.Wiki.Cache.OnlineUsers++;
		}

		protected void Application_BeginRequest( object sender, EventArgs e )
		{
			if ( Application[ "StartupOK" ] == null )
			{
				Application.Lock( );
				if ( Application[ "StartupOK" ] == null )
				{
					// Setup Resource Exchanger
					ScrewTurn.Wiki.Exchanger.ResourceExchanger = new ScrewTurn.Wiki.ResourceExchanger( );
					ScrewTurn.Wiki.StartupTools.Startup( );

					// All is OK, proceed with normal startup operations
					Application[ "StartupOK" ] = "OK";
				}
				Application.UnLock( );
			}

			RouteCurrentRequest( );
		}

		protected void Application_AcquireRequestState( object sender, EventArgs e )
		{
			if ( HttpContext.Current.Session != null )
			{
				// This should be performed on EndRequest, but Session is not available there
				SessionCache.ClearData( HttpContext.Current.Session.SessionID );

				// Try to automatically login the user through the cookie
				ScrewTurn.Wiki.LoginTools.TryAutoLogin( );
			}
		}

		protected void Application_AuthenticateRequest( object sender, EventArgs e )
		{
			// Nothing to do
		}

		/// <summary>
		/// Logs an error.
		/// </summary>
		/// <param name="ex">The error.</param>
		private void LogError( Exception ex )
		{
			//if(ex.InnerException != null) ex = ex.InnerException;
			try
			{
				ScrewTurn.Wiki.Log.LogEntry( Tools.GetCurrentUrlFixed( ) + "\n" +
					ex.Source + " thrown " + ex.GetType( ).FullName + "\n" + ex.Message + "\n" + ex.StackTrace,
					ScrewTurn.Wiki.PluginFramework.EntryType.Error, ScrewTurn.Wiki.Log.SystemUsername );
			}
			catch { }
		}

		protected void Application_Error( object sender, EventArgs e )
		{
			// Retrieve last error and log it, redirecting to Error.aspx (avoiding infinite loops)

			Exception ex = Server.GetLastError( );

			HttpException httpEx = ex as HttpException;
			if ( httpEx != null )
			{
				// Try to redirect an inexistent .aspx page to a probably existing .ashx page
				if ( httpEx.GetHttpCode( ) == 404 )
				{
					string page = System.IO.Path.GetFileNameWithoutExtension( Request.PhysicalPath );
					ScrewTurn.Wiki.UrlTools.Redirect( page + ScrewTurn.Wiki.Settings.PageExtension );
					return;
				}
			}

			LogError( ex );
			string url = "";
			try
			{
				url = Tools.GetCurrentUrlFixed( );
			}
			catch { }
			EmailTools.NotifyError( ex, url );
			Session[ "LastError" ] = Server.GetLastError( );
			if ( !Request.PhysicalPath.ToLowerInvariant( ).Contains( "error.aspx" ) ) ScrewTurn.Wiki.UrlTools.Redirect( "Error.aspx" );
		}

		protected void Session_End( object sender, EventArgs e )
		{
			// Decrement # of online users (only works when session is InProc)
			ScrewTurn.Wiki.Cache.OnlineUsers--;
		}

		protected void Application_End( object sender, EventArgs e )
		{
			// Try to cleanly shutdown the application and providers
			ScrewTurn.Wiki.StartupTools.Shutdown( );
		}

		/// <summary>
		/// Properly routes the current virtual request to a physical ASP.NET page.
		/// </summary>
		public static void RouteCurrentRequest( )
		{
			string physicalPath = null;

			try
			{
				physicalPath = HttpContext.Current.Request.PhysicalPath;
			}
			catch ( ArgumentException )
			{
				// Illegal characters in path
				HttpContext.Current.Response.Redirect( "~/PageNotFound.aspx" );
				return;
			}

			// Extract the physical page name, e.g. MainPage, Edit or Category
			string pageName = Path.GetFileNameWithoutExtension( physicalPath );
			// Exctract the extension, e.g. .ashx or .aspx
			var extension = Path.GetExtension( HttpContext.Current.Request.PhysicalPath );
			if ( extension == null )
			{
				//If extension null, nothing to do.
				return;
			}
			string ext = extension.ToLowerInvariant( );
			// Remove trailing dot, .ashx -> ashx
			if ( ext.Length > 0 )
				ext = ext.Substring( 1 );

			// IIS7+Integrated Pipeline handles all requests through the ASP.NET engine
			// All non-interesting files are not processed, such as GIF, CSS, etc.
			if ( ext != "ashx" && ext != "aspx" ) return;

			// Extract the current namespace, if any
			string nspace = UrlTools.GetCurrentNamespace( ) + "";
			if ( !string.IsNullOrEmpty( nspace ) )
			{
				// Verify that namespace exists
				if ( Pages.FindNamespace( nspace ) == null )
					HttpContext.Current.Response.Redirect( "~/PageNotFound.aspx?Page=" + pageName );
			}
			// Trim Namespace. from pageName
			if ( !string.IsNullOrEmpty( nspace ) )
				pageName = pageName.Substring( nspace.Length + 1 );

			string queryString = ""; // Empty or begins with ampersand, not question mark
			try
			{
				// This might throw exceptions if 3rd-party modules interfer with the request pipeline
				queryString = HttpContext.Current.Request.Url.Query.Replace( "?", "&" ); // Host not used
			}
			catch { }

			if ( ext.Equals( "ashx" ) )
			{
				// Content page requested, process it via Default.aspx
				if ( !queryString.Contains( "NS=" ) )
				{
					HttpContext.Current.RewritePath( "~/Default.aspx?Page=" + Tools.UrlEncode( pageName ) + "&NS=" + Tools.UrlEncode( nspace ) + queryString );
				}
				else
				{
					HttpContext.Current.RewritePath( "~/Default.aspx?Page=" + Tools.UrlEncode( pageName ) + queryString );
				}
			}
			else if ( ext.Equals( "aspx" ) )
			{
				// System page requested, redirect to the root of the application
				// For example: http://www.server.com/Namespace.Edit.aspx?Page=MainPage -> http://www.server.com/Edit.aspx?Page=MainPage&NS=Namespace
				if ( !string.IsNullOrEmpty( nspace ) )
				{
					if ( !queryString.Contains( "NS=" ) )
					{
						HttpContext.Current.RewritePath( "~/" + Tools.UrlEncode( pageName ) + "." + ext + "?NS=" + Tools.UrlEncode( nspace ) + queryString );
					}
					else
					{
						if ( queryString.Length > 1 ) queryString = "?" + queryString.Substring( 1 );
						HttpContext.Current.RewritePath( "~/" + Tools.UrlEncode( pageName ) + "." + ext + queryString );
					}
				}
			}
			// else nothing to do
		}

	}

}
