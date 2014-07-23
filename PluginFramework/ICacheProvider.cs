
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Provides an interface for implementing a Cache Provider for ScrewTurn Wiki.
	/// </summary>
	/// <remarks>The Cache should preferably reside in RAM for performance purposes.</remarks>
	public interface ICacheProviderV30 : IProviderV30 {

		/// <summary>
		/// Gets or sets the number of users online.
		/// </summary>
		int OnlineUsers {
			get;
			set;
		}

		/// <summary>
		/// Gets the value of a Pseudo-cache item, previously stored in the cache.
		/// </summary>
		/// <param name="name">The name of the item being requested.</param>
		/// <returns>The value of the item, or <c>null</c> if the item is not found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		string GetPseudoCacheValue(string name);

		/// <summary>
		/// Sets the value of a Pseudo-cache item.
		/// </summary>
		/// <param name="name">The name of the item being stored.</param>
		/// <param name="value">The value of the item. If the value is <c>null</c>, then the item should be removed from the cache.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		void SetPseudoCacheValue(string name, string value);

		/// <summary>
		/// Gets the Content of a Page, previously stored in cache.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the Content being requested.</param>
		/// <returns>The Page Content object, or <c>null</c> if the item is not found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> is <c>null</c>.</exception>
		PageContent GetPageContent(PageInfo pageInfo);

		/// <summary>
		/// Sets the Content of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the Content being stored.</param>
		/// <param name="content">The Content of the Page.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> or <b>content</b> are <c>null</c>.</exception>
		void SetPageContent(PageInfo pageInfo, PageContent content);

		/// <summary>
		/// Gets the partially-formatted content (text) of a Page, previously stored in the cache.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the content being requested.</param>
		/// <returns>The partially-formatted content, or <c>null</c> if the item is not found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> is <c>null</c>.</exception>
		string GetFormattedPageContent(PageInfo pageInfo);

		/// <summary>
		/// Sets the partially-preformatted content (text) of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the content being stored.</param>
		/// <param name="content">The partially-preformatted content.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> or <paramref name="content"/> are <c>null</c>.</exception>
		void SetFormattedPageContent(PageInfo pageInfo, string content);

		/// <summary>
		/// Removes a Page from the cache.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the Page that has to be removed.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="pageInfo"/> is <c>null</c>.</exception>
		void RemovePage(PageInfo pageInfo);

		/// <summary>
		/// Clears the Page Content cache.
		/// </summary>
		void ClearPageContentCache();

		/// <summary>
		/// Clears the Pseudo-Cache.
		/// </summary>
		void ClearPseudoCache();

		/// <summary>
		/// Reduces the size of the Page Content cache, removing the least-recently used items.
		/// </summary>
		/// <param name="cutSize">The number of Pages to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="cutSize"/> is less than or equal to zero.</exception>
		void CutCache(int cutSize);

		/// <summary>
		/// Gets the number of Pages whose content is currently stored in the cache.
		/// </summary>
		int PageCacheUsage {
			get;
		}

		/// <summary>
		/// Gets the numer of Pages whose formatted content is currently stored in the cache.
		/// </summary>
		int FormatterPageCacheUsage {
			get;
		}

		/// <summary>
		/// Adds or updates an editing session.
		/// </summary>
		/// <param name="page">The edited Page.</param>
		/// <param name="user">The User who is editing the Page.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> or <paramref name="user"/> are empty.</exception>
		void RenewEditingSession(string page, string user);

		/// <summary>
		/// Cancels an editing session.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="user">The User.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> or <paramref name="user"/> are empty.</exception>
		void CancelEditingSession(string page, string user);

		/// <summary>
		/// Finds whether a Page is being edited by a different user.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="currentUser">The User who is requesting the status of the Page.</param>
		/// <returns>True if the Page is being edited by another User.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="currentUser"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> or <paramref name="currentUser"/> are empty.</exception>
		bool IsPageBeingEdited(string page, string currentUser);

		/// <summary>
		/// Gets the username of the user who's editing a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The username.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> is empty.</exception>
		string WhosEditing(string page);

		/// <summary>
		/// Adds the redirection information for a page (overwrites the previous value, if any).
		/// </summary>
		/// <param name="source">The source page.</param>
		/// <param name="destination">The destination page.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="source"/> or <paramref name="destination"/> are empty.</exception>
		void AddRedirection(string source, string destination);

		/// <summary>
		/// Gets the destination of a redirection.
		/// </summary>
		/// <param name="source">The source page.</param>
		/// <returns>The destination page, if any, <c>null</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="source"/> is empty.</exception>
		string GetRedirectionDestination(string source);

		/// <summary>
		/// Removes a pge from both sources and destinations.
		/// </summary>
		/// <param name="name">The name of the page.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		void RemovePageFromRedirections(string name);

		/// <summary>
		/// Clears all the redirections information.
		/// </summary>
		void ClearRedirections();

	}

}
