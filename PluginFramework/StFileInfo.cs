
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains information about a file.
	/// </summary>
	/// <remarks>This class is only used for provider-host communication.</remarks>
	public class StFileInfo : FileDetails {

		private string fullName;
		private IFilesStorageProviderV30 provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StFileInfo" /> class.
		/// </summary>
		/// <param name="size">The size of the file in bytes.</param>
		/// <param name="lastModified">The last modification date/time.</param>
		/// <param name="downloadCount">The download count.</param>
		/// <param name="fullName">The full name of the file, for example <b>/dir/sub/file.txt</b> or <b>/file.txt</b>.</param>
		/// <param name="provider">The provider that handles the file.</param>
		public StFileInfo(long size, DateTime lastModified, int downloadCount, string fullName, IFilesStorageProviderV30 provider)
			: base(size, lastModified, downloadCount) {

			this.fullName = fullName;
			this.provider = provider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StFileInfo" /> class.
		/// </summary>
		/// <param name="details">The file details.</param>
		/// <param name="fullName">The full name.</param>
		/// <param name="provider">The provider.</param>
		public StFileInfo(FileDetails details, string fullName, IFilesStorageProviderV30 provider)
			: this(details.Size, details.LastModified, details.RetrievalCount, fullName, provider) {
		}

		/// <summary>
		/// Gets the full name of the file, for example <b>/dir/sub/file.txt</b> or <b>/file.txt</b>.
		/// </summary>
		public string FullName {
			get { return fullName; }
		}

		/// <summary>
		/// Gets the provider that handles the file.
		/// </summary>
		public IFilesStorageProviderV30 Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets the directory path of the file.
		/// </summary>
		/// <returns>The directory path.</returns>
		public string GetDirectory() {
			return StDirectoryInfo.GetDirectory(fullName);
		}

	}

}
