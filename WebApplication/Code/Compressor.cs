
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Allows to compress and decompress byte streams in memory.
	/// </summary>
	public static class Compressor {

		/// <summary>
		/// Compresses data.
		/// </summary>
		/// <param name="data">The data to compress.</param>
		/// <returns>The compressed data.</returns>
		public static byte[] Compress(byte[] data) {
			using(MemoryStream output = new MemoryStream()) {
				using(GZipStream gzip = new GZipStream(output, CompressionMode.Compress, true)) {
					gzip.Write(data, 0, data.Length);
					gzip.Close();
				}
				return output.ToArray();
			}
		}

		/// <summary>
		/// Decompresses data.
		/// </summary>
		/// <param name="data">The data to decompress.</param>
		/// <returns>The decompressed data.</returns>
		public static byte[] Decompress(byte[] data) {
			using(MemoryStream input = new MemoryStream()) {
				input.Write(data, 0, data.Length);
				input.Position = 0;
				using(GZipStream gzip = new GZipStream(input, CompressionMode.Decompress, true)) {
					using(MemoryStream output = new MemoryStream()) {
						byte[] buff = new byte[64];
						int read = -1;
						read = gzip.Read(buff, 0, buff.Length);
						while(read > 0) {
							output.Write(buff, 0, read);
							read = gzip.Read(buff, 0, buff.Length);
						}
						gzip.Close();
						return output.ToArray();
					}
				}
			}
		}

	}

}
