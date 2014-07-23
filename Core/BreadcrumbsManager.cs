
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Web;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages navigation Breadcrumbs.
	/// </summary>
	public class BreadcrumbsManager {

		private const int MaxPages = 10;
		private const string CookieName = "ScrewTurnWikiBreadcrumbs3";
		private const string CookieValue = "B";

		private List<PageInfo> pages;

		/// <summary>
		/// Gets the cookie.
		/// </summary>
		/// <returns>The cookie, or <c>null</c>.</returns>
		private HttpCookie GetCookie() {
			if(HttpContext.Current.Request != null) {
				HttpCookie cookie = HttpContext.Current.Request.Cookies[CookieName];
				return cookie;
			}
			else return null;
		}

		/// <summary>
		/// Initializes a new instance of the <b>BreadcrumbsManager</b> class.
		/// </summary>
		public BreadcrumbsManager() {
			pages = new List<PageInfo>(MaxPages);

			HttpCookie cookie = GetCookie();
			if(cookie != null && !string.IsNullOrEmpty(cookie.Values[CookieValue])) {
				try {
					foreach(string p in cookie.Values[CookieValue].Split('|')) {
						PageInfo page = Pages.FindPage(p);
						if(page != null) pages.Add(page);
					}
				}
				catch { }
			}
		}

		/// <summary>
		/// Updates the cookie.
		/// </summary>
		private void UpdateCookie() {
			HttpCookie cookie = GetCookie();
			if(cookie == null) {
				cookie = new HttpCookie(CookieName);
			}
			cookie.Path = Settings.CookiePath;

			StringBuilder sb = new StringBuilder(MaxPages * 20);
			for(int i = 0; i < pages.Count; i++) {
				sb.Append(pages[i].FullName);
				if(i != pages.Count - 1) sb.Append("|");
			}

			cookie.Values[CookieValue] = sb.ToString();
			if(HttpContext.Current.Response != null) {
				HttpContext.Current.Response.Cookies.Set(cookie);
			}
			if(HttpContext.Current.Request != null) {
				HttpContext.Current.Request.Cookies.Set(cookie);
			}
		}

		/// <summary>
		/// Adds a Page to the Breadcrumbs trail.
		/// </summary>
		/// <param name="page">The Page to add.</param>
		public void AddPage(PageInfo page) {
			lock(this) {
				int index = FindPage(page);
				if(index != -1) pages.RemoveAt(index);
				pages.Add(page);
				if(pages.Count > MaxPages) pages.RemoveRange(0, pages.Count - MaxPages);

				UpdateCookie();
			}
		}

		/// <summary>
		/// Finds a page by name.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The index in the collection.</returns>
		private int FindPage(PageInfo page) {
			lock(this) {
				if(pages == null || pages.Count == 0) return -1;

				PageNameComparer comp = new PageNameComparer();
				for(int i = 0; i < pages.Count; i++) {
					if(comp.Compare(pages[i], page) == 0) return i;
				}

				return -1;
			}
		}

		/// <summary>
		/// Removes a Page from the Breadcrumbs trail.
		/// </summary>
		/// <param name="page">The Page to remove.</param>
		public void RemovePage(PageInfo page) {
			lock(this) {
				int index = FindPage(page);
				if(index >= 0) pages.RemoveAt(index);

				UpdateCookie();
			}
		}

		/// <summary>
		/// Clears the Breadcrumbs trail.
		/// </summary>
		public void Clear() {
			lock(this) {
				pages.Clear();

				UpdateCookie();
			}
		}

		/// <summary>
		/// Gets all the Pages in the trail that still exist.
		/// </summary>
		public PageInfo[] AllPages {
			get {
				lock(this) {
					List<PageInfo> newPages = new List<PageInfo>(pages.Count);
					foreach(PageInfo p in pages) {
						if(Pages.FindPage(p.FullName) != null) newPages.Add(p);
					}

					return newPages.ToArray();
				}
			}
		}

	}

}
