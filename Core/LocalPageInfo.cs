
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Represents a Local Page.
	/// </summary>
	public class LocalPageInfo : PageInfo {

		private string file;

		/// <summary>
		/// Initializes a new instance of the <b>PageInfo</b> class.
		/// </summary>
		/// <param name="fullName">The Full Name of the Page.</param>
		/// <param name="provider">The Pages Storage Provider that manages this Page.</param>
		/// <param name="creationDateTime">The creation Date/Time.</param>
		/// <param name="file">The relative path of the file used for data storage.</param>
		public LocalPageInfo(string fullName, IPagesStorageProviderV30 provider, DateTime creationDateTime, string file)
			: base(fullName, provider, creationDateTime) {

			this.file = file;
		}

		/// <summary>
		/// Gets or sets the relative path of the File used for data storage.
		/// </summary>
		public string File {
			get { return file; }
			set { file = value; }
		}

	}

}
