
using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Resources;
using System.Reflection;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a Resource Exchanger.
	/// </summary>
	public class ResourceExchanger : IResourceExchanger {

		private ResourceManager manager;

		/// <summary>
		/// Initialises a new instance of the <b>ResourceExchanger</b> class.
		/// </summary>
		public ResourceExchanger() {
			manager = new ResourceManager("ScrewTurn.Wiki.Properties.Messages", typeof(Properties.Messages).Assembly);
		}

		/// <summary>
		/// Gets a Resource String.
		/// </summary>
		/// <param name="name">The Name of the Resource.</param>
		/// <returns>The Resource String.</returns>
		public string GetResource(string name) {
			return manager.GetString(name);
		}

	}

}
