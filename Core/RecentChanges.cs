
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages the Wiki's Recent Changes.
	/// </summary>
	public static class RecentChanges {

		/// <summary>
		/// Gets all the changes, sorted by date/time ascending.
		/// </summary>
		public static RecentChange[] GetAllChanges() {
			RecentChange[] changes = Settings.Provider.GetRecentChanges();

			RecentChange[] myCopy = new RecentChange[changes.Length];
			Array.Copy(changes, myCopy, changes.Length);

			Array.Sort(myCopy, (x, y) => { return x.DateTime.CompareTo(y.DateTime); });

			return myCopy;
		}

		/// <summary>
		/// Adds a new change.
		/// </summary>
		/// <param name="page">The page name.</param>
		/// <param name="title">The page title.</param>
		/// <param name="messageSubject">The message subject.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="user">The user.</param>
		/// <param name="change">The change.</param>
		/// <param name="descr">The description (optional).</param>
		public static void AddChange(string page, string title, string messageSubject, DateTime dateTime, string user, Change change, string descr) {
			RecentChange[] allChanges = GetAllChanges();
			if(allChanges.Length > 0) {
				RecentChange lastChange = allChanges[allChanges.Length - 1];
				if(lastChange.Page == page && lastChange.Title == title &&
					lastChange.MessageSubject == messageSubject + "" &&
					lastChange.User == user &&
					lastChange.Change == change &&
					(dateTime - lastChange.DateTime).TotalMinutes <= 60) {

					// Skip this change
					return;
				}
			}

			Settings.Provider.AddRecentChange(page, title, messageSubject, dateTime, user, change, descr);
		}

	}

}
