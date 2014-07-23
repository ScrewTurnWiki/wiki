
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains information about a file.
	/// </summary>
	public class FileDetails {

		private long size;
		private DateTime lastModified;
		private int retrievalCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FileDetails" /> class.
		/// </summary>
		/// <param name="size">The size of the file in bytes.</param>
		/// <param name="lastModified">The modification date/time.</param>
		/// <param name="retrievalCount">The number of times the file was retrieved.</param>
		public FileDetails(long size, DateTime lastModified, int retrievalCount) {
			this.size = size;
			this.lastModified = lastModified;
			this.retrievalCount = retrievalCount;
		}
		
		/// <summary>
		/// Gets the size of the file in bytes.
		/// </summary>
		public long Size {
			get { return size; }
		}

		/// <summary>
		/// Gets the modification date/time.
		/// </summary>
		public DateTime LastModified {
			get { return lastModified; }
		}

		/// <summary>
		/// Gets the number of times the file was retrieved.
		/// </summary>
		public int RetrievalCount {
			get { return retrievalCount; }
		}

	}

}
