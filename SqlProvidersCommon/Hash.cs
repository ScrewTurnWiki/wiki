
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Computes hashes.
	/// </summary>
	public static class Hash {

		/// <summary>
		/// Computes the Hash code of a string.
		/// </summary>
		/// <param name="input">The string.</param>
		/// <returns>The Hash code.</returns>
		private static byte[] ComputeBytes(string input) {
			SHA1 sha1 = SHA1CryptoServiceProvider.Create();
			return sha1.ComputeHash(Encoding.ASCII.GetBytes(input));
		}

		/// <summary>
		/// Computes the Hash code of a string and converts it into a Hex string.
		/// </summary>
		/// <param name="input">The string.</param>
		/// <returns>The Hash code, converted into a Hex string.</returns>
		public static string Compute(string input) {
			byte[] bytes = ComputeBytes(input);
			string result = "";
			for(int i = 0; i < bytes.Length; i++) {
				result += string.Format("{0:X2}", bytes[i]);
			}
			return result;
		}

	}

}
