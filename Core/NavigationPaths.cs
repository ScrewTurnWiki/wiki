
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages navigation paths.
	/// </summary>
	public static class NavigationPaths {

		/// <summary>
		/// Gets the list of the Navigation Paths.
		/// </summary>
		/// <returns>The navigation paths, sorted by name.</returns>
		public static List<NavigationPath> GetAllNavigationPaths() {
			List<NavigationPath> allPaths = new List<NavigationPath>(30);

			// Retrieve paths from every Pages provider
			foreach(IPagesStorageProviderV30 provider in Collectors.PagesProviderCollector.AllProviders) {
				allPaths.AddRange(provider.GetNavigationPaths(null));
				foreach(NamespaceInfo nspace in provider.GetNamespaces()) {
					allPaths.AddRange(provider.GetNavigationPaths(nspace));
				}
			}

			allPaths.Sort(new NavigationPathComparer());

			return allPaths;
		}

		/// <summary>
		/// Gets the list of the Navigation Paths in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace.</param>
		/// <returns>The navigation paths, sorted by name.</returns>
		public static List<NavigationPath> GetNavigationPaths(NamespaceInfo nspace) {
			List<NavigationPath> allPaths = new List<NavigationPath>(30);

			// Retrieve paths from every Pages provider
			foreach(IPagesStorageProviderV30 provider in Collectors.PagesProviderCollector.AllProviders) {
				allPaths.AddRange(provider.GetNavigationPaths(nspace));
			}

			allPaths.Sort(new NavigationPathComparer());

			return allPaths;
		}

		/// <summary>
		/// Finds a Navigation Path's Name.
		/// </summary>
		/// <param name="name">The Name.</param>
		/// <returns>True if the Navigation Path exists.</returns>
		public static bool Exists(string name) {
			return Find(name) != null;
		}

		/// <summary>
		/// Finds and returns a Path.
		/// </summary>
		/// <param name="fullName">The full name.</param>
		/// <returns>The correct <see cref="T:NavigationPath" /> object or <c>null</c> if no path is found.</returns>
		public static NavigationPath Find(string fullName) {
			List<NavigationPath> allPaths = GetAllNavigationPaths();
			int idx = allPaths.BinarySearch(new NavigationPath(fullName, null), new NavigationPathComparer());
			if(idx >= 0) return allPaths[idx];
			else return null;
		}

		/// <summary>
		/// Adds a new Navigation Path.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name.</param>
		/// <param name="pages">The Pages.</param>
		/// <param name="provider">The Provider to use for the new Navigation Path, or <c>null</c> for the default provider.</param>
		/// <returns>True if the Path is added successfully.</returns>
		public static bool AddNavigationPath(NamespaceInfo nspace, string name, List<PageInfo> pages, IPagesStorageProviderV30 provider) {
			string namespaceName = nspace != null ? nspace.Name : null;
			string fullName = NameTools.GetFullName(namespaceName, name);

			if(Exists(fullName)) return false;

			if(provider == null) provider = Collectors.PagesProviderCollector.GetProvider(Settings.DefaultPagesProvider);

			NavigationPath newPath = provider.AddNavigationPath(namespaceName, name, pages.ToArray());
			if(newPath != null) Log.LogEntry("Navigation Path " + fullName + " added", EntryType.General, Log.SystemUsername);
			else Log.LogEntry("Creation failed for Navigation Path " + fullName, EntryType.Error, Log.SystemUsername);
			return newPath != null;
		}

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="fullName">The full name of the path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		public static bool RemoveNavigationPath(string fullName) {
			NavigationPath path = Find(fullName);
			if(path == null) return false;

			bool done = path.Provider.RemoveNavigationPath(path);
			if(done) Log.LogEntry("Navigation Path " + fullName + " removed", EntryType.General, Log.SystemUsername);
			else Log.LogEntry("Deletion failed for Navigation Path " + fullName, EntryType.Error, Log.SystemUsername);
			return done;
		}

		/// <summary>
		/// Modifies a Navigation Path.
		/// </summary>
		/// <param name="fullName">The full name of the path to modify.</param>
		/// <param name="pages">The list of Pages.</param>
		/// <returns><c>true</c> if the path is modified, <c>false</c> otherwise.</returns>
		public static bool ModifyNavigationPath(string fullName, List<PageInfo> pages) {
			NavigationPath path = Find(fullName);
			if(path == null) return false;

			NavigationPath newPath = path.Provider.ModifyNavigationPath(path, pages.ToArray());
			if(newPath != null) Log.LogEntry("Navigation Path " + fullName + " modified", EntryType.General, Log.SystemUsername);
			else Log.LogEntry("Modification failed for Navigation Path " + fullName, EntryType.Error, Log.SystemUsername);
			return newPath != null;
		}

		/// <summary>
		/// Finds all the Navigation Paths that include a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The list of Navigation Paths.</returns>
		public static string[] PathsPerPage(PageInfo page) {
			NamespaceInfo pageNamespace = Pages.FindNamespace(NameTools.GetNamespace(page.FullName));

			List<string> result = new List<string>(10);
			List<NavigationPath> allPaths = GetNavigationPaths(pageNamespace);

			for(int i = 0; i < allPaths.Count; i++) {
				List<string> pages = new List<string>(allPaths[i].Pages);
				if(pages.Contains(page.FullName)) {
					result.Add(allPaths[i].FullName);
				}
			}
			return result.ToArray();
		}

	}

}
