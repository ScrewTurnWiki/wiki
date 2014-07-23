
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// The base interface that all the Storage Providers must implement. All the Provider Type-specific interfaces inherit from this one or from a one, either directly or from a derived interface
	/// </summary>
	/// <remarks>This interface should not be implemented directly by a class.</remarks>
	public interface IStorageProviderV30 : IProviderV30 {

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		bool ReadOnly { get; }

	}

}
