
using System;

namespace ScrewTurn.Wiki.PluginFramework
{

	/// <summary>
	/// Represents a namespace.
	/// </summary>
	public class NamespaceInfo : IComparable<NamespaceInfo>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NamespaceInfo" /> class.
		/// </summary>
		/// <param name="name">The namespace name.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="defaultPage">The default page, or <c>null</c>.</param>
		public NamespaceInfo( string name, IPagesStorageProviderV30 provider, PageInfo defaultPage )
		{
			Name = name;
			Provider = provider;
			DefaultPage = defaultPage;
		}

		/// <summary>
		/// Gets or sets the name of the namespace.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the provider.
		/// </summary>
		public IPagesStorageProviderV30 Provider { get; set; }

		/// <summary>
		/// Gets or sets the default page, or <c>null</c>.
		/// </summary>
		public PageInfo DefaultPage { get; set; }

		/// <summary>
		/// Compares the current <see cref="NamespaceInfo" /> with another <see cref="NamespaceInfo" />.
		/// </summary>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public int CompareTo( NamespaceInfo other )
		{
			//This object, by definition, cannot be null, so the 0 and -1 cases of the generic NamespaceComparer do not apply here.
			return other == null ? 1 : StringComparer.OrdinalIgnoreCase.Compare( Name, other.Name );
		}

		/// <summary>
		/// Gets a string representation of the current object.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString( )
		{
			return Name;
		}

	}
}
