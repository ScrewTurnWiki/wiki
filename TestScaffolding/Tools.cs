
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.Tests {

	/// <summary>
	/// Implement some useful testing tool.s
	/// </summary>
	public static class Tools {

		private const string DateTimeFormat = "yyyy-MM-dd-HH-mm-ss";

		/// <summary>
		/// Prints a date/time in the "yyyy/MM/dd HH:mm:ss" format.
		/// </summary>
		/// <param name="dt">The date/time to print.</param>
		/// <returns>The string value.</returns>
		private static string PrintDateTime(DateTime dt) {
			return dt.ToString(DateTimeFormat);
		}

		/// <summary>
		/// Asserts that two date/time values are equal.
		/// </summary>
		/// <param name="expected">The expected date/time value.</param>
		/// <param name="actual">The actual date/time value.</param>
		/// <param name="ignoreUpToOneSecond">A value indicating whether to ignore a difference up to 10 seconds.</param>
		public static void AssertDateTimesAreEqual(DateTime expected, DateTime actual, bool ignoreUpToTenSecondsDifference) {
			if(ignoreUpToTenSecondsDifference) {
				TimeSpan span = expected - actual;
				Assert.IsTrue(Math.Abs(span.TotalSeconds) <= 10, "Wrong date/time value");
				/*Assert.AreEqual(
					PrintDateTime(expected).Substring(0, DateTimeFormat.Length - 1),
					PrintDateTime(actual).Substring(0, DateTimeFormat.Length - 1),
					"Wrong date/time value");*/
			}
			else {
				Assert.AreEqual(PrintDateTime(expected), PrintDateTime(actual), "Wrong date/time value");
			}
		}

		/// <summary>
		/// Asserts that two date/time values are equal.
		/// </summary>
		/// <param name="expected">The expected date/time value.</param>
		/// <param name="actual">The actual date/time value.</param>
		public static void AssertDateTimesAreEqual(DateTime expected, DateTime actual) {
			AssertDateTimesAreEqual(expected, actual, false);
		}

	}

}
