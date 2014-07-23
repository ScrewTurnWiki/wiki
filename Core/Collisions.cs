
using System;
using System.Collections.Generic;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages Page Editing collisions.
	/// </summary>
	public static class Collisions {

		/// <summary>
		/// The refresh interval used for renewing the sessions.
		/// </summary>
		public const int EditingSessionTimeout = 20;

		/// <summary>
		/// Adds or updates an editing session.
		/// </summary>
		/// <param name="page">The edited Page.</param>
		/// <param name="user">The User who is editing the Page.</param>
		public static void RenewEditingSession(PageInfo page, string user) {
			Cache.Provider.RenewEditingSession(page.FullName, user);
		}

		/// <summary>
		/// Cancels an editing session.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="user">The User.</param>
		public static void CancelEditingSession(PageInfo page, string user) {
			Cache.Provider.CancelEditingSession(page.FullName, user);
		}

		/// <summary>
		/// Finds whether a Page is being edited by a different user.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="currentUser">The User who is requesting the status of the Page.</param>
		/// <returns>True if the Page is being edited by another User.</returns>
		public static bool IsPageBeingEdited(PageInfo page, string currentUser) {
			return Cache.Provider.IsPageBeingEdited(page.FullName, currentUser);
		}

		/// <summary>
		/// Gets the username of the user who's editing a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The username.</returns>
		public static string WhosEditing(PageInfo page) {
			return Cache.Provider.WhosEditing(page.FullName);
		}

	}

}
