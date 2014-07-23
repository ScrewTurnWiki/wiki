
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Represents a Navigation Path.
	/// </summary>
	public class NavigationPath {

		/// <summary>
		/// The namespace of the Navigation Path (<c>null</c> for the root).
		/// </summary>
		protected string nspace;
		/// <summary>
		/// The Name of the Navigation Path.
		/// </summary>
		protected string name;
		/// <summary>
		/// The names of the Pages in the Navigation Path.
		/// </summary>
		protected string[] pages;
		/// <summary>
		/// The Provider that handles the Navigation Path.
		/// </summary>
		protected IPagesStorageProviderV30 provider;

		/// <summary>
		/// Initializes a new instance of the <b>NavigationPath</b> class.
		/// </summary>
		/// <param name="fullName">The Full Name of the Navigation Path.</param>
		/// <param name="provider">The Provider</param>
		public NavigationPath(string fullName, IPagesStorageProviderV30 provider) {
			NameTools.ExpandFullName(fullName, out nspace, out name);
			this.provider = provider;
			pages = new string[0];
		}

		/// <summary>
		/// Gets or sets the full name of the Navigation Path, such as 'Namespace.Path' or 'Path'.
		/// </summary>
		public string FullName {
			get { return NameTools.GetFullName(nspace, name); }
			set { NameTools.ExpandFullName(value, out nspace, out name); }
		}

		/// <summary>
		/// Gets or sets the Pages of the Path.
		/// </summary>
		public string[] Pages {
			get { return pages; }
			set { pages = value; }
		}

		/// <summary>
		/// Gets the Provider.
		/// </summary>
		public IPagesStorageProviderV30 Provider {
			get { return provider; }
		}

	}

	/// <summary>
	/// Compares two <see cref="T:NavigationPaths" /> objects, using FullName as the comparison parameter.
	/// </summary>
	public class NavigationPathComparer : IComparer<NavigationPath> {

		/// <summary>
		/// Compares two Navigation Paths's FullName.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The result of the comparison (1, 0 or -1).</returns>
		public int Compare(NavigationPath x, NavigationPath y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.FullName, y.FullName);
		}

	}

}
