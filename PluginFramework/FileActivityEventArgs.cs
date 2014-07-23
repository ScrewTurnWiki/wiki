
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {
	
	/// <summary>
	/// Contains arguments for the File Activity event.
	/// </summary>
	public class FileActivityEventArgs : EventArgs {

		private StFileInfo file;
		private string oldFileName;
		private StDirectoryInfo directory;
		private string oldDirectoryName;
		private PageInfo page;
		private FileActivity activity;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FileActivityEventArgs" /> class.
		/// </summary>
		/// <param name="file">The file that changed, if any (full path).</param>
		/// <param name="oldFileName">The old name of the file, if any (full path).</param>
		/// <param name="directory">The directory that changed, if any (full path).</param>
		/// <param name="oldDirectoryName">The old name of the directory, if any (full path).</param>
		/// <param name="page">The page owning the attachment, if any.</param>
		/// <param name="activity">The activity.</param>
		public FileActivityEventArgs(StFileInfo file, string oldFileName,
			StDirectoryInfo directory, string oldDirectoryName,
			PageInfo page, FileActivity activity) {

			this.file = file;
			this.oldFileName = oldFileName;
			this.directory = directory;
			this.oldDirectoryName = oldDirectoryName;
			this.page = page;
			this.activity = activity;
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public IFilesStorageProviderV30 Provider {
			get {
				if(file != null) return file.Provider;
				else if(directory != null) return directory.Provider;
				else return null;
			}
		}

		/// <summary>
		/// Gets the file that changed, if any.
		/// </summary>
		public StFileInfo File {
			get { return file; }
		}

		/// <summary>
		/// Gets the old name of the file, if any.
		/// </summary>
		public string OldFileName {
			get { return oldFileName; }
		}

		/// <summary>
		/// Gets the directory that changed, if any.
		/// </summary>
		public StDirectoryInfo Directory {
			get { return directory; }
		}

		/// <summary>
		/// Gets the old name of the directory, if any.
		/// </summary>
		public string OldDirectoryName {
			get { return oldDirectoryName; }
		}

		/// <summary>
		/// Gets the page owning the attachment, if any.
		/// </summary>
		public PageInfo Page {
			get { return page; }
		}

		/// <summary>
		/// Gets the activity.
		/// </summary>
		public FileActivity Activity {
			get { return activity; }
		}

	}

	/// <summary>
	/// Lists legal file activities.
	/// </summary>
	public enum FileActivity {
		/// <summary>
		/// A file has been uploaded.
		/// </summary>
		FileUploaded,
		/// <summary>
		/// A file has been renamed.
		/// </summary>
		FileRenamed,
		/// <summary>
		/// A file has been deleted.
		/// </summary>
		FileDeleted,
		/// <summary>
		/// A directory has been created.
		/// </summary>
		DirectoryCreated,
		/// <summary>
		/// A directory has been renamed.
		/// </summary>
		DirectoryRenamed,
		/// <summary>
		/// A directory (and all its contents) has been deleted.
		/// </summary>
		DirectoryDeleted,
		/// <summary>
		/// An attachment has been uploaded.
		/// </summary>
		AttachmentUploaded,
		/// <summary>
		/// An attachment has been renamed.
		/// </summary>
		AttachmentRenamed,
		/// <summary>
		/// An attachment has been deleted.
		/// </summary>
		AttachmentDeleted
	}

}
