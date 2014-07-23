
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Represents a Page Category. A page can be binded with one or more categories (within the same Provider); this class manages this binding.
	/// </summary>
	public class CategoryInfo {

		/// <summary>
		/// The namespace of the Category.
		/// </summary>
		protected string nspace;
		/// <summary>
		/// The Name of the Category.
		/// </summary>
		protected string name;
		/// <summary>
		/// The Provider that handles the Category.
		/// </summary>
		protected IPagesStorageProviderV30 provider;
		/// <summary>
		/// The Pages of the Category.
		/// </summary>
		protected string[] pages = new string[0];

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CategoryInfo" /> class.
		/// </summary>
		/// <param name="fullName">The Full Name of the Category.</param>
		/// <param name="provider">The Storage that manages the category.</param>
		public CategoryInfo(string fullName, IPagesStorageProviderV30 provider) {
			NameTools.ExpandFullName(fullName, out nspace, out name);
			this.provider = provider;
		}

		/// <summary>
		/// Gets or sets the full name of the Category, such as 'Namespace.Category' or 'Category'.
		/// </summary>
		public string FullName {
			get { return NameTools.GetFullName(nspace, name); }
			set { NameTools.ExpandFullName(value, out nspace, out name); }
		}

		/// <summary>
		/// Gets or sets the Provider that manages the Category.
		/// </summary>
		public IPagesStorageProviderV30 Provider {
			get { return provider; }
			set { provider = value; }
		}

		/// <summary>
		/// Gets or sets the Page array, containing their names.
		/// </summary>
		public string[] Pages {
			get { return pages; }
			set { pages = value; }
		}

		/// <summary>
		/// Gets a string representation of the current object.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return NameTools.GetFullName(nspace, name);
		}

	}

	/// <summary>
	/// Compares two <b>CategoryInfo</b> objects, using the FullName as parameter.
	/// </summary>
	/// <remarks>The comparison is <b>case insensitive</b>.</remarks>
	public class CategoryNameComparer : IComparer<CategoryInfo> {

		/// <summary>
		/// Compares two <see cref="CategoryInfo"/> objects, using the FullName as parameter.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The comparison result (-1, 0 or 1).</returns>
		public int Compare(CategoryInfo x, CategoryInfo y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.FullName, y.FullName);
		}

	}

}
