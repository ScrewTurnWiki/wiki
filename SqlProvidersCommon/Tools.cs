
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements tools.
	/// </summary>
	public static class Tools {

		/// <summary>
		/// Reads the contents of a <see cref="T:Stream" /> in a byte array, beginning at the current position through the end.
		/// </summary>
		/// <param name="stream">The <see cref="T:Stream" />.</param>
		/// <param name="buffer">The output byte array (allocated by the method).</param>
		/// <param name="maxSize">The max size to read.</param>
		/// <returns>The number of bytes read, or <b>-maxSize</b> if the max size is exceeded.</returns>
		public static int ReadStream(Stream stream, ref byte[] buffer, int maxSize) {
			int read = 0;
			int total = 0;

			byte[] temp = new byte[maxSize];

			do {
				read = stream.Read(temp, total, temp.Length - total);
				total += read;

				if(total > maxSize) return -maxSize;

			} while(read > 0);

			buffer = new byte[total];
			Buffer.BlockCopy(temp, 0, buffer, 0, (int)total);

			return total;
		}

	}

}
