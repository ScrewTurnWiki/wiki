
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Class used for exchaning data between the <b>Core</b> library and the Wiki engine.
	/// </summary>
	public static class Exchanger {

		private static IResourceExchanger resourceExchanger;

		/// <summary>
		/// Gets or sets the singleton instance of the Resource Exchanger object.
		/// </summary>
		public static IResourceExchanger ResourceExchanger {
			get { return resourceExchanger; }
			set { resourceExchanger = value; }
		}

	}

}
