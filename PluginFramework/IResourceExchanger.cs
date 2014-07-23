
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Exposes methods for exchanging Resources.
	/// </summary>
	public interface IResourceExchanger {

		/// <summary>
		/// Gets a Resource String.
		/// </summary>
		/// <param name="name">The Name of the Resource.</param>
		/// <returns>The Resource String.</returns>
		string GetResource(string name);

	}

}
