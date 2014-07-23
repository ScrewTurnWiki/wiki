
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a base class for local file-based data providers.
	/// </summary>
	public abstract class ProviderBase {

		/// <summary>
		/// Gets the data directory.
		/// </summary>
		/// <param name="host">The host object.</param>
		/// <returns>The data directory.</returns>
		protected string GetDataDirectory(IHostV30 host) {
			return host.GetSettingValue(SettingName.PublicDirectory);
		}

	}

}
