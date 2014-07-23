
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains information about a Provider.
	/// </summary>
	public class ComponentInformation {

		/// <summary>
		/// The Name of the Component.
		/// </summary>
		protected string name;
		/// <summary>
		/// The Author of the Component.
		/// </summary>
		protected string author;
		/// <summary>
		/// The component version.
		/// </summary>
		protected string version;
		/// <summary>
		/// The info URL of the Component/Author.
		/// </summary>
		protected string url;
		/// <summary>
		/// The component update URL which should point to a text file containing one or two rows (separated by \r\n or \n):
		/// 1. A list of increasing versions separated by pipes, such as "1.0.0|1.0.1|1.0.2" (without quotes)
		/// 2. (optional) The absolute HTTP URL of the latest DLL, for example "http://www.server.com/update/MyAssembly.dll" (without quotes)
		/// The second row should only be present if the provider can be updated automatically without any type of user
		/// intervention, i.e. by simply replacing the DLL and restarting the wiki. If the DLL contains multiple providers,
		/// they are all updated (obviously). The new DLL must have the same name of the being-replaced DLL (in other words,
		/// a provider must reside in the same DLL forever in order to be updated automatically).
		/// </summary>
		protected string updateUrl;

		/// <summary>
		/// Initializes a new instance of the <b>ComponentInformation</b> class.
		/// </summary>
		/// <param name="name">The Name of the Component.</param>
		/// <param name="author">The Author of the Component.</param>
		/// <param name="version">The component version.</param>
		/// <param name="url">The info URL of the Component/Author.</param>
		/// <param name="updateUrl">The update URL of the component, or <c>null</c>.</param>
		public ComponentInformation(string name, string author, string version, string url, string updateUrl) {
			this.name = name;
			this.author = author;
			this.version = version;
			this.url = url;
			this.updateUrl = updateUrl;
		}

		/// <summary>
		/// Gets the Name of the Component.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the Author of the Component.
		/// </summary>
		public string Author {
			get { return author; }
		}

		/// <summary>
		/// Gets the component version.
		/// </summary>
		public string Version {
			get { return version; }
		}

		/// <summary>
		/// Gets the info URL of the Component/Author.
		/// </summary>
		public string Url {
			get { return url; }
		}

		/// <summary>
		/// Gets the update URL of the component.
		/// </summary>
		public string UpdateUrl {
			get { return updateUrl; }
		}

	}

}
