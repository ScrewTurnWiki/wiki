
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages data cache.
	/// </summary>
	public static class Cache {

		/// <summary>
		/// Gets the cache provider.
		/// </summary>
		public static ICacheProviderV30 Provider {
			get { return Collectors.CacheProviderCollector.GetProvider(Settings.DefaultCacheProvider); }
		}

		/// <summary>
		/// Gets or sets the number of users online.
		/// </summary>
		public static int OnlineUsers {
			get { return Provider.OnlineUsers; }
			set { Provider.OnlineUsers = value; }
		}

		/// <summary>
		/// Clears the pages cache.
		/// </summary>
		public static void ClearPageCache() {
			Provider.ClearPageContentCache();
		}

		/// <summary>
		/// Clears the pseudo cache.
		/// </summary>
		public static void ClearPseudoCache() {
			Provider.ClearPseudoCache();
		}

		/// <summary>
		/// Gets a cached <see cref="T:PageContent" />.
		/// </summary>
		/// <param name="page">The page to get the content of.</param>
		/// <returns>The page content, or <c>null</c>.</returns>
		public static PageContent GetPageContent(PageInfo page) {
			if(page == null) return null;
			return Provider.GetPageContent(page);
		}

		/// <summary>
		/// Gets a cached formatted page content.
		/// </summary>
		/// <param name="page">The page to get the formatted content of.</param>
		/// <returns>The formatted page content, or <c>null</c>.</returns>
		public static string GetFormattedPageContent(PageInfo page) {
			if(page == null) return null;
			return Provider.GetFormattedPageContent(page);
		}

		/// <summary>
		/// Sets the page content in cache.
		/// </summary>
		/// <param name="page">The page to set the content of.</param>
		/// <param name="content">The content.</param>
		public static void SetPageContent(PageInfo page, PageContent content) {
			Provider.SetPageContent(page, content);
			if(Provider.PageCacheUsage > Settings.CacheSize) {
				Provider.CutCache(Settings.CacheCutSize);
			}
		}

		/// <summary>
		/// Sets the formatted page content in cache.
		/// </summary>
		/// <param name="page">The page to set the content of.</param>
		/// <param name="content">The content.</param>
		public static void SetFormattedPageContent(PageInfo page, string content) {
			Provider.SetFormattedPageContent(page, content);
		}

		/// <summary>
		/// Removes a page from the cache.
		/// </summary>
		/// <param name="page">The page to remove.</param>
		public static void RemovePage(PageInfo page) {
			Provider.RemovePage(page);
		}

		/// <summary>
		/// Gets a pseudo cache item value.
		/// </summary>
		/// <param name="name">The name of the item to get the value of.</param>
		/// <returns>The value of the item, or <c>null</c>.</returns>
		public static string GetPseudoCacheValue(string name) {
			return Provider.GetPseudoCacheValue(name);
		}

		/// <summary>
		/// Sets a pseudo cache item value.
		/// </summary>
		/// <param name="name">The name of the item to set the value of.</param>
		/// <param name="value">The value of the item.</param>
		public static void SetPseudoCacheValue(string name, string value) {
			Provider.SetPseudoCacheValue(name, value);
		}

		/// <summary>
		/// Gets the number of pages currently in the page cache.
		/// </summary>
		public static int PageCacheUsage {
			get { return Provider.PageCacheUsage; }
		}

		/// <summary>
		/// Gets the number of formatted pages currently in the page cache.
		/// </summary>
		public static int FormattedPageCacheUsage {
			get { return Provider.FormatterPageCacheUsage; }
		}

	}

}
