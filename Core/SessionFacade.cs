
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.SessionState;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Exposes in a strongly-typed fashion the Session variables.
	/// </summary>
	public static class SessionFacade {

		/// <summary>
		/// Gets the current Session object.
		/// </summary>
		private static HttpSessionState Session {
			get {
				if(HttpContext.Current == null) return null;
				else return HttpContext.Current.Session;
			}
		}

		/// <summary>
		/// Gets or sets the Login Key.
		/// </summary>
		public static string LoginKey {
			get { return Session != null ? (string)Session["LoginKey"] : null; }
			set { if(Session != null) Session["LoginKey"] = value; }
		}

		/// <summary>
		/// Gets or sets the current username, or <c>null</c>.
		/// </summary>
		public static string CurrentUsername {
			get { return Session != null ? Session["Username"] as string : null; }
			set { if(Session != null) Session["Username"] = value; }
		}

		/// <summary>
		/// Gets the current user, if any.
		/// </summary>
		/// <returns>The current user, or <c>null</c>.</returns>
		public static UserInfo GetCurrentUser() {
			if(Session != null) {
				string sessionId = Session.SessionID;

				UserInfo current = SessionCache.GetCurrentUser(sessionId);
				if(current != null) return current;
				else {
					string un = CurrentUsername;
					if(string.IsNullOrEmpty(un)) return null;
					else if(un == AnonymousUsername) return Users.GetAnonymousAccount();
					else {
						current = Users.FindUser(un);
						if(current != null) {
							SessionCache.SetCurrentUser(sessionId, current);
							return current;
						}
						else {
							// Username is invalid
							Session.Clear();
							Session.Abandon();
							return null;
						}
					}
				}
			}
			else return null;
		}

		/// <summary>
		/// The username for anonymous users.
		/// </summary>
		public const string AnonymousUsername = "|";

		/// <summary>
		/// Gets the current username for security checks purposes only.
		/// </summary>
		/// <returns>The current username, or <b>AnonymousUsername</b> if anonymous.</returns>
		public static string GetCurrentUsername() {
			string un = CurrentUsername;
			if(string.IsNullOrEmpty(un)) return AnonymousUsername;
			else return un;
		}

		/// <summary>
		/// Gets the current user groups.
		/// </summary>
		public static UserGroup[] GetCurrentGroups() {
			if(Session != null) {
				string sessionId = Session.SessionID;
				UserGroup[] groups = SessionCache.GetCurrentGroups(sessionId);

				if(groups == null || groups.Length == 0) {
					UserInfo current = GetCurrentUser();
					if(current != null) {
						// This check is necessary because after group deletion the session might contain outdated data
						List<UserGroup> temp = new List<UserGroup>(current.Groups.Length);
						for(int i = 0; i < current.Groups.Length; i++) {
							UserGroup tempGroup = Users.FindUserGroup(current.Groups[i]);
							if(tempGroup != null) temp.Add(tempGroup);
						}
						groups = temp.ToArray();
					}
					else {
						groups = new UserGroup[] { Users.FindUserGroup(Settings.AnonymousGroup) };
					}

					SessionCache.SetCurrentGroups(sessionId, groups);
				}

				return groups;
			}
			else return new UserGroup[0];
		}

		/// <summary>
		/// Gets the current group names.
		/// </summary>
		/// <returns>The group names.</returns>
		public static string[] GetCurrentGroupNames() {
			return Array.ConvertAll(GetCurrentGroups(), delegate(UserGroup g) { return g.Name; });
		}

		/// <summary>
		/// Gets the Breadcrumbs Manager.
		/// </summary>
		public static BreadcrumbsManager Breadcrumbs {
			get { return new BreadcrumbsManager(); }
		}

	}

	/// <summary>
	/// Implements a session data cache whose lifetime is only limited to one request.
	/// </summary>
	public static class SessionCache {

		private static Dictionary<string, UserInfo> currentUsers = new Dictionary<string, UserInfo>(100);
		private static Dictionary<string, UserGroup[]> currentGroups = new Dictionary<string, UserGroup[]>(100);

		/// <summary>
		/// Gets the current user, if any, of a session.
		/// </summary>
		/// <param name="sessionId">The session ID.</param>
		/// <returns>The current user, or <c>null</c>.</returns>
		public static UserInfo GetCurrentUser(string sessionId) {
			lock(currentUsers) {
				UserInfo result = null;
				currentUsers.TryGetValue(sessionId, out result);
				return result;
			}
		}

		/// <summary>
		/// Sets the current user of a session.
		/// </summary>
		/// <param name="sessionId">The session ID.</param>
		/// <param name="user">The user.</param>
		public static void SetCurrentUser(string sessionId, UserInfo user) {
			lock(currentUsers) {
				currentUsers[sessionId] = user;
			}
		}

		/// <summary>
		/// Gets the current groups, if any, of a session.
		/// </summary>
		/// <param name="sessionId">The session ID.</param>
		/// <returns>The groups, or <b>null.</b></returns>
		public static UserGroup[] GetCurrentGroups(string sessionId) {
			lock(currentGroups) {
				UserGroup[] result = null;
				currentGroups.TryGetValue(sessionId, out result);
				return result;
			}
		}

		/// <summary>
		/// Sets the current groups of a session.
		/// </summary>
		/// <param name="sessionId">The session ID.</param>
		/// <param name="groups">The groups.</param>
		public static void SetCurrentGroups(string sessionId, UserGroup[] groups) {
			lock(currentGroups) {
				currentGroups[sessionId] = groups;
			}
		}

		/// <summary>
		/// Clears all cached data of a session.
		/// </summary>
		/// <param name="sessionId">The session ID.</param>
		public static void ClearData(string sessionId) {
			lock(currentUsers) {
				currentUsers.Remove(sessionId);
			}
			lock(currentGroups) {
				currentGroups.Remove(sessionId);
			}
		}

	}

}
