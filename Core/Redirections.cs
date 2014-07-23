
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages information about Page Redirections.
	/// </summary>
	public static class Redirections {

		/// <summary>
		/// Adds a new Redirection.
		/// </summary>
		/// <param name="source">The source Page.</param>
		/// <param name="destination">The destination Page.</param>
		/// <returns>True if the Redirection is added, false otherwise.</returns>
		/// <remarks>The method prevents circular and multi-level redirection.</remarks>
		public static void AddRedirection(PageInfo source, PageInfo destination) {
			if(source == null) throw new ArgumentNullException("source");
			if(destination == null) throw new ArgumentNullException("destination");

			Cache.Provider.AddRedirection(source.FullName, destination.FullName);
		}

		/// <summary>
		/// Gets the destination Page.
		/// </summary>
		/// <param name="page">The source Page.</param>
		/// <returns>The destination Page, or null.</returns>
		public static PageInfo GetDestination(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			string destination = Cache.Provider.GetRedirectionDestination(page.FullName);
			if(string.IsNullOrEmpty(destination)) return null;
			else return Pages.FindPage(destination);
		}

		/// <summary>
		/// Removes any occurrence of a Page from the redirection table, both on sources and destinations.
		/// </summary>
		/// <param name="page">The Page to wipe-out.</param>
		/// <remarks>This method is useful when removing a Page.</remarks>
		public static void WipePageOut(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			Cache.Provider.RemovePageFromRedirections(page.FullName);
		}

		/// <summary>
		/// Clears the Redirection table.
		/// </summary>
		public static void Clear() {
			Cache.Provider.ClearRedirections();
		}

	}

}
