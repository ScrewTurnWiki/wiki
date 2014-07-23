
using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;

namespace ScrewTurn.Wiki {

    /// <summary>
    /// Helps computing Hash codes.
    /// </summary>
    public static class Hash {

        /// <summary>
        /// Computes the Hash code of a string.
        /// </summary>
        /// <param name="input">The string.</param>
        /// <returns>The Hash code.</returns>
        public static byte[] ComputeBytes(string input) {
            MD5 md5 = MD5CryptoServiceProvider.Create();
            return md5.ComputeHash(Encoding.ASCII.GetBytes(input));
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

        /// <summary>
        /// Computes the Hash of a Username, mixing it with other data, in order to avoid illegal Account activations.
        /// </summary>
        /// <param name="username">The Username.</param>
		/// <param name="email">The email.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="otherData">The other data to mix into the input string.</param>
        /// <returns>The secured Hash of the Username.</returns>
        public static string ComputeSecurityHash(string username, string email, DateTime dateTime, string otherData) {
			// Use a salt (is this actually useful given that STW is opensource and everyone can see the salt?)
			return Compute(otherData + username + email + dateTime.ToString("yyyyMMddHHmmss") +"$kbfl?nfc4");
        }

    }

}
