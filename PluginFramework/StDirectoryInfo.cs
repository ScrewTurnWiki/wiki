
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains information about a directory.
	/// </summary>
	/// <remarks>This class is only used for provider-host communication.</remarks>
	public class StDirectoryInfo {

		private string fullPath;
		private IFilesStorageProviderV30 provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StDirectoryInfo" /> class.
		/// </summary>
		/// <param name="fullPath">The full path of the directory, for example <b>/dir/sub/</b> or <b>/</b>.</param>
		/// <param name="provider">The provider that handles the directory.</param>
		public StDirectoryInfo(string fullPath, IFilesStorageProviderV30 provider) {
			this.fullPath = fullPath;
			this.provider = provider;
		}

		/// <summary>
		/// Gets the full path of the directory, for example <b>/dir/sub/</b> or <b>/</b>.
		/// </summary>
		public string FullPath {
			get { return fullPath; }
		}

		/// <summary>
		/// Gets the provider that handles the directory.
		/// </summary>
		public IFilesStorageProviderV30 Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets the directory of a file.
		/// </summary>
		/// <param name="filePath">The full file path, such as '/file.txt' or '/directory/sub/file.txt'.</param>
		/// <returns>The directory, such as '/' or '/directory/sub/'.</returns>
		public static string GetDirectory(string filePath) {
			if(!filePath.StartsWith("/")) filePath = "/" + filePath;

			int lastIndex = filePath.LastIndexOf("/");
			if(lastIndex == 0) return "/";
			else return filePath.Substring(0, lastIndex + 1);
		}

	}

}
