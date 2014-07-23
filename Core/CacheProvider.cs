
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a local cache provider. All instance members are thread-safe.
	/// </summary>
	public class CacheProvider : ICacheProviderV30 {

		private readonly ComponentInformation _info =
			new ComponentInformation("Local Cache Provider", "Threeplicate Srl", Settings.WikiVersion, "http://www.screwturn.eu", null);

		private IHostV30 _host;

		// Implements the pseudo cache
		private Dictionary<string, string> _pseudoCache;

		// Contains the page contents
		private Dictionary<PageInfo, PageContent> _pageContentCache;

		// Contains the partially-formatted page content
		private Dictionary<PageInfo, string> _formattedContentCache;

		// Records, for each page, how many times a page has been requested,
		// limited to page contents (not formatted content)
		private Dictionary<PageInfo, int> _pageCacheUsage;

		private int _onlineUsers = 0;

		private List<EditingSession> _sessions;

		// Key is lowercase, invariant culture
		private Dictionary<string, string> _redirections;

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_host = host;

			int s = int.Parse(host.GetSettingValue(SettingName.CacheSize));

			// Initialize pseudo cache
			_pseudoCache = new Dictionary<string, string>(10);

			// Initialize page content cache
			_pageContentCache = new Dictionary<PageInfo, PageContent>(s);
			_pageCacheUsage = new Dictionary<PageInfo, int>(s);

			// Initialize formatted page content cache
			_formattedContentCache = new Dictionary<PageInfo, string>(s);

			_sessions = new List<EditingSession>(50);

			_redirections = new Dictionary<string, string>(50);
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
			ClearPseudoCache();
			ClearPageContentCache();
		}

		/// <summary>
		/// Gets or sets the number of users online.
		/// </summary>
		public int OnlineUsers {
			get {
				lock(this) {
					return _onlineUsers;
				}
			}
			set {
				lock(this) {
					_onlineUsers = value;
				}
			}
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return _info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

		/// <summary>
		/// Gets the value of a Pseudo-cache item, previously stored in the cache.
		/// </summary>
		/// <param name="name">The name of the item being requested.</param>
		/// <returns>The value of the item, or <c>null</c> if the item is not found.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public string GetPseudoCacheValue(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			string value = null;
			lock(_pseudoCache) {
				_pseudoCache.TryGetValue(name, out value);
			}
			return value;
		}

		/// <summary>
		/// Sets the value of a Pseudo-cache item.
		/// </summary>
		/// <param name="name">The name of the item being stored.</param>
		/// <param name="value">The value of the item. If the value is <c>null</c>, then the item should be removed from the cache.</param>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public void SetPseudoCacheValue(string name, string value) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			lock(_pseudoCache) {
				if(value == null) _pseudoCache.Remove(name);
				else _pseudoCache[name] = value;
			}
		}

		/// <summary>
		/// Gets the Content of a Page, previously stored in cache.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the Content being requested.</param>
		/// <returns>The Page Content object, or <c>null</c> if the item is not found.</returns>
		/// <exception cref="ArgumentNullException">If <b>pageInfo</b> is <c>null</c>.</exception>
		public PageContent GetPageContent(PageInfo pageInfo) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");

			PageContent value = null;
			lock(_pageContentCache) {
				if(_pageContentCache.TryGetValue(pageInfo, out value)) {
					_pageCacheUsage[pageInfo]++;
				}
			}
			return value;
		}

		/// <summary>
		/// Sets the Content of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the Content being stored.</param>
		/// <param name="content">The Content of the Page.</param>
		/// <exception cref="ArgumentNullException">If <b>pageInfo</b> or <b>content</b> are <c>null</c>.</exception>
		public void SetPageContent(PageInfo pageInfo, PageContent content) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(content == null) throw new ArgumentNullException("content");

			lock(_pageContentCache) {
				_pageContentCache[pageInfo] = content;
				lock(_pageCacheUsage) {
					_pageCacheUsage[pageInfo] = 0;
				}
			}
		}

		/// <summary>
		/// Gets the partially-formatted content (text) of a Page, previously stored in the cache.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the content being requested.</param>
		/// <returns>The partially-formatted content, or <c>null</c> if the item is not found.</returns>
		/// <exception cref="ArgumentNullException">If <b>pageInfo</b> is <c>null</c>.</exception>
		public string GetFormattedPageContent(PageInfo pageInfo) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");

			string value = null;
			lock(_formattedContentCache) {
				_formattedContentCache.TryGetValue(pageInfo, out value);
			}
			return value;
		}

		/// <summary>
		/// Sets the partially-preformatted content (text) of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the content being stored.</param>
		/// <param name="content">The partially-preformatted content.</param>
		/// <exception cref="ArgumentNullException">If <b>pageInfo</b> or <b>content</b> are <c>null</c>.</exception>
		public void SetFormattedPageContent(PageInfo pageInfo, string content) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");
			if(content == null) throw new ArgumentNullException("content");

			lock(_formattedContentCache) {
				_formattedContentCache[pageInfo] = content;
			}
		}

		/// <summary>
		/// Removes a Page from the cache.
		/// </summary>
		/// <param name="pageInfo">The Page Info object related to the Page that has to be removed.</param>
		/// <exception cref="ArgumentNullException">If <b>pageInfo</b> is <c>null</c>.</exception>
		public void RemovePage(PageInfo pageInfo) {
			if(pageInfo == null) throw new ArgumentNullException("pageInfo");

			// In this order
			lock(_formattedContentCache) {
				_formattedContentCache.Remove(pageInfo);
			}
			lock(_pageContentCache) {
				_pageContentCache.Remove(pageInfo);
			}
			lock(_pageCacheUsage) {
				_pageCacheUsage.Remove(pageInfo);
			}
		}

		/// <summary>
		/// Clears the Page Content cache.
		/// </summary>
		public void ClearPageContentCache() {
			// In this order
			lock(_formattedContentCache) {
				_formattedContentCache.Clear();
			}
			lock(_pageContentCache) {
				_pageContentCache.Clear();
			}
			lock(_pageCacheUsage) {
				_pageCacheUsage.Clear();
			}
		}

		/// <summary>
		/// Clears the Pseudo-Cache.
		/// </summary>
		public void ClearPseudoCache() {
			lock(_pseudoCache) {
				_pseudoCache.Clear();
			}
		}

		/// <summary>
		/// Reduces the size of the Page Content cache, removing the least-recently used items.
		/// </summary>
		/// <param name="cutSize">The number of Pages to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <b>cutSize</b> is less than or equal to zero.</exception>
		public void CutCache(int cutSize) {
			if(cutSize <= 0) throw new ArgumentOutOfRangeException("cutSize", "Cut Size should be greater than zero");

			lock(_pageContentCache) {

				// TODO: improve performance - now the operation is O(cache_size^2)
				for(int i = 0; i < cutSize; i++) {
					PageInfo key = null;
					int min = int.MaxValue;
					
					// Find the page that has been requested the least times
					foreach(PageInfo p in _pageCacheUsage.Keys) {
						if(_pageCacheUsage[p] < min) {
							key = p;
							min = _pageCacheUsage[p];
						}
					}
					
					// This is necessary to avoid infinite loops
					if(key == null) {
						break;
					}
					// Remove the page from cache
					RemovePage(key);
				}
			}
		}

		/// <summary>
		/// Gets the number of Pages whose content is currently stored in the cache.
		/// </summary>
		public int PageCacheUsage {
			get {
				lock(_pageContentCache) {
					return _pageContentCache.Count;
				}
			}
		}

		/// <summary>
		/// Gets the numer of Pages whose formatted content is currently stored in the cache.
		/// </summary>
		public int FormatterPageCacheUsage {
			get {
				lock(_formattedContentCache) {
					return _formattedContentCache.Count;
				}
			}
		}

		/// <summary>
		/// Adds or updates an editing session.
		/// </summary>
		/// <param name="page">The edited Page.</param>
		/// <param name="user">The User who is editing the Page.</param>
		/// <exception cref="ArgumentNullException">If <b>page</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> or <b>user</b> are empty.</exception>
		public void RenewEditingSession(string page, string user) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			lock(this) {
				bool found = false;
				for(int i = 0; i < _sessions.Count; i++) {
					if(_sessions[i].Page == page && _sessions[i].User.Equals(user)) {
						_sessions[i].Renew();
						found = true;
						break;
					}
				}
				if(!found) _sessions.Add(new EditingSession(page, user));
			}
		}

		/// <summary>
		/// Cancels an editing session.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="user">The User.</param>
		/// <exception cref="ArgumentNullException">If <b>page</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> or <b>user</b> are empty.</exception>
		public void CancelEditingSession(string page, string user) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			lock(this) {
				for(int i = 0; i < _sessions.Count; i++) {
					if(_sessions[i].Page == page && _sessions[i].User.Equals(user)) {
						_sessions.Remove(_sessions[i]);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Finds whether a Page is being edited by a different user.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="currentUser">The User who is requesting the status of the Page.</param>
		/// <returns>True if the Page is being edited by another User.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> or <b>currentUser</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> or <b>currentUser</b> are empty.</exception>
		public bool IsPageBeingEdited(string page, string currentUser) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(currentUser == null) throw new ArgumentNullException("currentUser");
			if(currentUser.Length == 0) throw new ArgumentException("Current User cannot be empty", "currentUser");

			lock(this) {
				int timeout = int.Parse(_host.GetSettingValue(SettingName.EditingSessionTimeout));
				DateTime limit = DateTime.Now.AddSeconds(-(timeout + 5)); // Allow 5 seconds for network delays

				for(int i = 0; i < _sessions.Count; i++) {
					if(_sessions[i].Page == page && !_sessions[i].User.Equals(currentUser)) {
						if(_sessions[i].LastContact.CompareTo(limit) >= 0) {
							// Page is being edited
							return true;
						}
						else {
							// Lost contact
							_sessions.Remove(_sessions[i]);
							return false;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the username of the user who's editing a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The username.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>page</b> is empty.</exception>
		public string WhosEditing(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("Page cannot be empty", "page");

			lock(this) {
				foreach(EditingSession s in _sessions) {
					if(s.Page == page) return s.User;
				}
			}
			return "";
		}

		/// <summary>
		/// Adds the redirection information for a page (overwrites the previous value, if any).
		/// </summary>
		/// <param name="source">The source page.</param>
		/// <param name="destination">The destination page.</param>
		/// <exception cref="ArgumentNullException">If <b>source</b> or <b>destination</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>source</b> or <b>destination</b> are empty.</exception>
		public void AddRedirection(string source, string destination) {
			if(source == null) throw new ArgumentNullException("source");
			if(source.Length == 0) throw new ArgumentException("Source cannot be empty", "source");
			if(destination == null) throw new ArgumentNullException("destination");
			if(destination.Length == 0) throw new ArgumentException("Destination cannot be empty", "destination");

			lock(_redirections) {
				_redirections[source.ToLowerInvariant()] = destination;
			}
		}

		/// <summary>
		/// Gets the destination of a redirection.
		/// </summary>
		/// <param name="source">The source page.</param>
		/// <returns>The destination page, if any, <c>null</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>source</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>source</b> is empty.</exception>
		public string GetRedirectionDestination(string source) {
			if(source == null) throw new ArgumentNullException("source");
			if(source.Length == 0) throw new ArgumentException("Source cannot be empty", "source");

			string dest = null;

			lock(_redirections) {
				_redirections.TryGetValue(source.ToLowerInvariant(), out dest);
			}

			return dest;
		}

		/// <summary>
		/// Removes a pge from both sources and destinations.
		/// </summary>
		/// <param name="name">The name of the page.</param>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public void RemovePageFromRedirections(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			name = name.ToLowerInvariant();

			lock(_redirections) {
				_redirections.Remove(name);

				List<string> keysToRemove = new List<string>(10);

				foreach(KeyValuePair<string, string> pair in _redirections) {
					if(pair.Value.ToLowerInvariant() == name) keysToRemove.Add(pair.Key);
				}

				foreach(string key in keysToRemove) {
					_redirections.Remove(key);
				}
			}
		}

		/// <summary>
		/// Clears all the redirections information.
		/// </summary>
		public void ClearRedirections() {
			lock(_redirections) {
				_redirections.Clear();
			}
		}

	}

	/// <summary>
	/// Represents an Editing Session.
	/// </summary>
	public class EditingSession {

		private string page;
		private string user;
		private DateTime lastContact;

		/// <summary>
		/// Initializes a new instance of the <b>EditingSession</b> class.
		/// </summary>
		/// <param name="page">The edited Page.</param>
		/// <param name="user">The User who is editing the Page.</param>
		public EditingSession(string page, string user) {
			this.page = page;
			this.user = user;
			lastContact = DateTime.Now;
		}

		/// <summary>
		/// Gets the edited Page.
		/// </summary>
		public string Page {
			get { return page; }
		}

		/// <summary>
		/// Gets the User.
		/// </summary>
		public string User {
			get { return user; }
		}

		/// <summary>
		/// Sets the Last Contact to now.
		/// </summary>
		public void Renew() {
			lastContact = DateTime.Now;
		}

		/// <summary>
		/// Gets the Last Contact Date/Time.
		/// </summary>
		public DateTime LastContact {
			get { return lastContact; }
		}

	}

}
