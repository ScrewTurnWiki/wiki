
using System;
using System.Configuration;
using System.Web;
using System.Collections.Generic;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

    /// <summary>
    /// Contains the Contents.
    /// </summary>
    public static class Content {

		/// <summary>
		/// Gets a pseudo cache item value.
		/// </summary>
		/// <param name="name">The name of the item to retrieve the value of.</param>
		/// <returns>The value of the item, or <c>null</c>.</returns>
		public static string GetPseudoCacheValue(string name) {
			return Cache.GetPseudoCacheValue(name);
		}

		/// <summary>
		/// Sets a pseudo cache item value, only if the content cache is enabled.
		/// </summary>
		/// <param name="name">The name of the item to store the value of.</param>
		/// <param name="value">The value of the item.</param>
		public static void SetPseudoCacheValue(string name, string value) {
			if(!Settings.DisableCache) {
				Cache.SetPseudoCacheValue(name, value);
			}
		}

		/// <summary>
		/// Clears the pseudo cache.
		/// </summary>
		public static void ClearPseudoCache() {
			Cache.ClearPseudoCache();
			Redirections.Clear();
		}

		/// <summary>
		/// Reads the Content of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page.</param>
		/// <param name="cached">Specifies whether the page has to be cached or not.</param>
		/// <returns>The Page Content.</returns>
		public static PageContent GetPageContent(PageInfo pageInfo, bool cached) {
			PageContent result = Cache.GetPageContent(pageInfo);
			if(result == null) {
				result = pageInfo.Provider.GetContent(pageInfo);
				if(result!= null && !result.IsEmpty()) {
					if(cached && !pageInfo.NonCached && !Settings.DisableCache) {
						Cache.SetPageContent(pageInfo, result);
					}
				}
			}

			// result should NEVER be null
			if(result == null) {
				Log.LogEntry("PageContent could not be retrieved for page " + pageInfo.FullName + " - returning empty", EntryType.Error, Log.SystemUsername);
				result = PageContent.GetEmpty(pageInfo);
			}

			return result;
		}

		/// <summary>
		/// Gets the formatted Page Content, properly handling content caching and the Formatting Pipeline.
		/// </summary>
		/// <param name="page">The Page to get the formatted Content of.</param>
		/// <param name="cached">Specifies whether the formatted content has to be cached or not.</param>
		/// <returns>The formatted content.</returns>
		public static string GetFormattedPageContent(PageInfo page, bool cached) {
			string content = Cache.GetFormattedPageContent(page);
			if(content == null) {
				PageContent pg = GetPageContent(page, cached);
				string[] linkedPages;
				content = FormattingPipeline.FormatWithPhase1And2(pg.Content, false, FormattingContext.PageContent, page, out linkedPages);
				pg.LinkedPages = linkedPages;
				if(!pg.IsEmpty() && cached && !page.NonCached && !Settings.DisableCache) {
					Cache.SetFormattedPageContent(page, content);
				}
			}
			return FormattingPipeline.FormatWithPhase3(content, FormattingContext.PageContent, page);
		}

		/// <summary>
		/// Invalidates the cached Content of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page to invalidate the cached content of.</param>
        public static void InvalidatePage(PageInfo pageInfo) {
			Cache.RemovePage(pageInfo);
			Redirections.WipePageOut(pageInfo);
        }

		/// <summary>
		/// Invalidates all the cache Contents.
		/// </summary>
		public static void InvalidateAllPages() {
			Cache.ClearPageCache();
			Redirections.Clear();
		}

    }

}
