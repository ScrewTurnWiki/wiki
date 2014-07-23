
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains basic information about a Page.
	/// </summary>
	public class PageInfo {

		/// <summary>
		/// The namespace of the Page.
		/// </summary>
		protected string nspace;
		/// <summary>
		/// The Name of the Page.
		/// </summary>
		protected string name;
		/// <summary>
		/// The Provider that handles the Page.
		/// </summary>
		protected IPagesStorageProviderV30 provider;
		/// <summary>
		/// A value specifying whether the Page should NOT be cached by the engine.
		/// </summary>
		protected bool nonCached;
		/// <summary>
		/// The Page creation Date/Time.
		/// </summary>
		protected DateTime creationDateTime;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageInfo" /> class.
		/// </summary>
		/// <param name="fullName">The Full Name of the Page.</param>
		/// <param name="provider">The Pages Storage Provider that manages this Page.</param>
		/// <param name="creationDateTime">The Page creation Date/Time.</param>
		public PageInfo(string fullName, IPagesStorageProviderV30 provider, DateTime creationDateTime) {
			NameTools.ExpandFullName(fullName, out nspace, out name);
			this.provider = provider;
			this.creationDateTime = creationDateTime;
		}

		/// <summary>
		/// Gets or sets the full name of the Page, such as 'Namespace.Page' or 'Page'.
		/// </summary>
		public string FullName {
			get { return NameTools.GetFullName(nspace, name); }
			set { NameTools.ExpandFullName(value, out nspace, out name); }
		}

		/// <summary>
		/// Gets or sets the Pages Storage Provider.
		/// </summary>
		public IPagesStorageProviderV30 Provider {
			get { return provider; }
			set { provider = value; }
		}

		/// <summary>
		/// Gets or sets a value specifying whether the Page should NOT be cached by the engine.
		/// </summary>
		public bool NonCached {
			get { return nonCached; }
			set { nonCached = value; }
		}

		/// <summary>
		/// Gets or sets the creation Date/Time.
		/// </summary>
		public DateTime CreationDateTime {
			get { return creationDateTime; }
			set { creationDateTime = value; }
		}

		/// <summary>
		/// Converts the current PageInfo to a string.
		/// </summary>
		/// <returns>The string.</returns>
		public override string ToString() {
			string result = NameTools.GetFullName(nspace, name);
			result += " [" + provider.Information.Name + "]";
			return result;
		}

	}

	/// <summary>
	/// Compares two <see cref="T:PageInfo" /> objects, using the FullName as parameter.
	/// </summary>
	/// <remarks>The comparison is <b>case insensitive</b>.</remarks>
	public class PageNameComparer : IComparer<PageInfo> {

		/// <summary>
		/// Compares two <see cref="T:PageInfo" /> objects, using the FullName as parameter.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The comparison result (-1, 0 or 1).</returns>
		public int Compare(PageInfo x, PageInfo y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.FullName, y.FullName);
		}

	}

}
