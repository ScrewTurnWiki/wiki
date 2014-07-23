
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.IO;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages files, directories and attachments.
	/// </summary>
	public static class FilesAndAttachments {

		#region Files

		/// <summary>
		/// Finds the provider that has a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <returns>The provider that has the file, or <c>null</c> if the file could not be found.</returns>
		public static IFilesStorageProviderV30 FindFileProvider(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(string.IsNullOrEmpty(fullName)) throw new ArgumentException("Full Name cannot be empty", "fullName");

			fullName = NormalizeFullName(fullName);

			foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
				FileDetails details = provider.GetFileDetails(fullName);
				if(details != null) return provider;
			}

			return null;
		}

		/// <summary>
		/// Gets the details of a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <returns>The details of the file, or <c>null</c> if no file is found.</returns>
		public static FileDetails GetFileDetails(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(string.IsNullOrEmpty(fullName)) throw new ArgumentException("Full Name cannot be empty", "fullName");

			fullName = NormalizeFullName(fullName);

			foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
				FileDetails details = provider.GetFileDetails(fullName);
				if(details != null) return details;
			}

			return null;
		}

		/// <summary>
		/// Retrieves a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <param name="output">The output stream.</param>
		/// <param name="countHit">A value indicating whether or not to count this retrieval in the statistics.</param>
		/// <returns><c>true</c> if the file is retrieved, <c>false</c> otherwise.</returns>
		public static bool RetrieveFile(string fullName, Stream output, bool countHit) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(output == null) throw new ArgumentNullException("destinationStream");
			if(!output.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			fullName = NormalizeFullName(fullName);

			IFilesStorageProviderV30 provider = FindFileProvider(fullName);

			if(provider == null) return false;
			else return provider.RetrieveFile(fullName, output, countHit);
		}

		#endregion

		#region Directories

		/// <summary>
		/// Finds the provider that has a directory.
		/// </summary>
		/// <param name="fullPath">The full path of the directory.</param>
		/// <returns>The provider that has the directory, or <c>null</c> if no directory is found.</returns>
		public static IFilesStorageProviderV30 FindDirectoryProvider(string fullPath) {
			if(fullPath == null) throw new ArgumentNullException("fullPath");
			if(fullPath.Length == 0) throw new ArgumentException("Full Path cannot be empty");

			fullPath = NormalizeFullPath(fullPath);

			// In order to verify that the full path exists, it is necessary to navigate 
			// from the root down to the specified directory level
			// Example: /my/very/nested/directory/structure/
			// 1. Check that / contains /my/
			// 2. Check that /my/ contains /my/very/
			// 3. ...

			// allLevels contains this:
			// /my/very/nested/directory/structure/
			// /my/very/nested/directory/
			// /my/very/nested/
			// /my/very/
			// /my/
			// /

			string oneLevelUp = fullPath;

			List<string> allLevels = new List<string>(10);
			allLevels.Add(fullPath.ToLowerInvariant());
			while(oneLevelUp != "/") {
				oneLevelUp = UpOneLevel(oneLevelUp);
				allLevels.Add(oneLevelUp.ToLowerInvariant());
			}

			foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
				bool allLevelsFound = true;

				for(int i = allLevels.Count - 1; i >= 1; i--) {
					string[] dirs = provider.ListDirectories(allLevels[i]);

					string nextLevel =
						(from d in dirs
						 where d.ToLowerInvariant() == allLevels[i - 1]
						 select d).FirstOrDefault();

					if(string.IsNullOrEmpty(nextLevel)) {
						allLevelsFound = false;
						break;
					}
				}

				if(allLevelsFound) return provider;
			}

			return null;
		}

		/// <summary>
		/// Lists the directories in a directory.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <returns>The directories.</returns>
		/// <remarks>If the specified directory is the root, then the list is performed on all providers.</remarks>
		public static string[] ListDirectories(string fullPath) {
			fullPath = NormalizeFullPath(fullPath);

			if(fullPath == "/") {
				List<string> directories = new List<string>(50);

				foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
					directories.AddRange(provider.ListDirectories(fullPath));
				}

				directories.Sort();

				return directories.ToArray();
			}
			else {
				IFilesStorageProviderV30 provider = FindDirectoryProvider(fullPath);
				return provider.ListDirectories(fullPath);
			}
		}

		/// <summary>
		/// Lists the files in a directory.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <returns>The files.</returns>
		/// <remarks>If the specified directory is the root, then the list is performed on all providers.</remarks>
		public static string[] ListFiles(string fullPath) {
			fullPath = NormalizeFullPath(fullPath);

			if(fullPath == "/") {
				List<string> files = new List<string>(50);
				
				foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
					files.AddRange(provider.ListFiles(fullPath));
				}

				files.Sort();

				return files.ToArray();
			}
			else {
				IFilesStorageProviderV30 provider = FindDirectoryProvider(fullPath);
				return provider.ListFiles(fullPath);
			}
		}

		#endregion

		#region Page Attachments

		/// <summary>
		/// Finds the provider that has a page attachment.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>The provider that has the attachment, or <c>null</c> if the attachment could not be found.</returns>
		public static IFilesStorageProviderV30 FindPageAttachmentProvider(PageInfo page, string attachmentName) {
			if(page == null) throw new ArgumentNullException("page");
			if(attachmentName == null) throw new ArgumentNullException("attachmentName");
			if(attachmentName.Length == 0) throw new ArgumentException("Attachment Name cannot be empty", "attachmentName");

			foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
				FileDetails details = provider.GetPageAttachmentDetails(page, attachmentName);
				if(details != null) return provider;
			}

			return null;
		}

		/// <summary>
		/// Gets the details of a page attachment.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>The details of the attachment, or <c>null</c> if the attachment could not be found.</returns>
		public static FileDetails GetPageAttachmentDetails(PageInfo page, string attachmentName) {
			if(page == null) throw new ArgumentNullException("page");
			if(attachmentName == null) throw new ArgumentNullException("attachmentName");
			if(attachmentName.Length == 0) throw new ArgumentException("Attachment Name cannot be empty", "attachmentName");

			foreach(IFilesStorageProviderV30 provider in Collectors.FilesProviderCollector.AllProviders) {
				FileDetails details = provider.GetPageAttachmentDetails(page, attachmentName);
				if(details != null) return details;
			}

			return null;
		}

		/// <summary>
		/// Retrieves a Page Attachment.
		/// </summary>
		/// <param name="page">The Page Info that owns the Attachment.</param>
		/// <param name="attachmentName">The name of the Attachment, for example "myfile.jpg".</param>
		/// <param name="output">The output stream.</param>
		/// <param name="countHit">A value indicating whether or not to count this retrieval in the statistics.</param>
		/// <returns><c>true</c> if the Attachment is retrieved, <c>false</c> otherwise.</returns>
		public static bool RetrievePageAttachment(PageInfo page, string attachmentName, Stream output, bool countHit) {
			if(page == null) throw new ArgumentNullException("pageInfo");
			if(attachmentName == null) throw new ArgumentNullException("name");
			if(attachmentName.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(output == null) throw new ArgumentNullException("destinationStream");
			if(!output.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			IFilesStorageProviderV30 provider = FindPageAttachmentProvider(page, attachmentName);

			if(provider == null) return false;
			else return provider.RetrievePageAttachment(page, attachmentName, output, countHit);
		}

		#endregion

		/// <summary>
		/// Normalizes a full name.
		/// </summary>
		/// <param name="fullName">The full name.</param>
		/// <returns>The normalized full name.</returns>
		private static string NormalizeFullName(string fullName) {
			if(!fullName.StartsWith("/")) fullName = "/" + fullName;
			return fullName;
		}

		/// <summary>
		/// Normalizes a full path.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <returns>The normalized full path.</returns>
		private static string NormalizeFullPath(string fullPath) {
			if(fullPath == null) return "/";
			if(!fullPath.StartsWith("/")) fullPath = "/" + fullPath;
			if(!fullPath.EndsWith("/")) fullPath += "/";
			return fullPath;
		}

		/// <summary>
		/// Goes up one level in a directory path.
		/// </summary>
		/// <param name="fullPath">The full path, normalized, different from "/".</param>
		/// <returns>The directory.</returns>
		private static string UpOneLevel(string fullPath) {
			if(fullPath == "/") throw new ArgumentException("Cannot navigate up from the root");

			string temp = fullPath.Trim('/');
			int lastIndex = temp.LastIndexOf("/");

			return "/" + temp.Substring(0, lastIndex + 1);
		}

	}

}
