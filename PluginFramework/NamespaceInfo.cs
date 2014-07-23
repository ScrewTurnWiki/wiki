
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Represents a namespace.
	/// </summary>
	public class NamespaceInfo {

		/// <summary>
		/// The name of the namespace.
		/// </summary>
		protected string name;
		/// <summary>
		/// The provider of the namespace.
		/// </summary>
		protected IPagesStorageProviderV30 provider;
		/// <summary>
		/// The default page of the namespace (can be <c>null</c>).
		/// </summary>
		protected PageInfo defaultPage;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NamespaceInfo" /> class.
		/// </summary>
		/// <param name="name">The namespace name.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="defaultPage">The default page, or <c>null</c>.</param>
		public NamespaceInfo(string name, IPagesStorageProviderV30 provider, PageInfo defaultPage) {
			this.name = name;
			this.provider = provider;
			this.defaultPage = defaultPage;
		}

		/// <summary>
		/// Gets or sets the name of the namespace.
		/// </summary>
		public string Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets or sets the provider.
		/// </summary>
		public IPagesStorageProviderV30 Provider {
			get { return provider; }
			set { provider = value; }
		}

		/// <summary>
		/// Gets or sets the default page, or <c>null</c>.
		/// </summary>
		public PageInfo DefaultPage {
			get { return defaultPage; }
			set { defaultPage = value; }
		}

		/// <summary>
		/// Gets a string representation of the current object.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return name;
		}

	}

	/// <summary>
	/// Compares two <see cref="T:NamespaceInfo" /> objects, using the Name as parameter.
	/// </summary>
	public class NamespaceComparer : IComparer<NamespaceInfo> {

		/// <summary>
		/// Compares two <see cref="T:NamespaceInfo" /> objects, using the Name as parameter.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The comparison result (-1, 0 or 1).</returns>
		public int Compare(NamespaceInfo x, NamespaceInfo y) {
			if(x == null && y == null) return 0;
			else if(x == null && y != null) return -1;
			else if(x != null && y == null) return 1;
			else return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}

	}

}
