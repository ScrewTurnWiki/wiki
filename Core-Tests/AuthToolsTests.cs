
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class AuthToolsTests {

		[Test]
		public void Static_PrepareUsername() {
			Assert.AreEqual("U.User", AuthTools.PrepareUsername("User"), "Wrong result");
			Assert.AreEqual("U.U.User", AuthTools.PrepareUsername("U.User"), "Wrong result");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Static_PrepareUsername_InvalidUsername(string s) {
			AuthTools.PrepareUsername(s);
		}

		[Test]
		public void Static_PrepareGroups() {
			Assert.AreEqual(0, AuthTools.PrepareGroups(new string[0]).Length, "Wrong result length");

			string[] input = new string[] { "Group", "G.Group" };
			string[] output = AuthTools.PrepareGroups(input);

			Assert.AreEqual(input.Length, output.Length, "Wrong result length");
			for(int i = 0; i < input.Length; i++) {
				Assert.AreEqual("G." + input[i], output[i], "Wrong value");
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Static_PrepareGroups_NullGroups() {
			AuthTools.PrepareGroups(null);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Static_PrepareGroups_InvalidElement(string e) {
			AuthTools.PrepareGroups(new string[] { e });
		}

		[TestCase(null, false, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", false, ExpectedException = typeof(ArgumentException))]
		[TestCase("G", false, ExpectedException = typeof(ArgumentException))]
		[TestCase("G.", true)]
		[TestCase("g.", true)]
		[TestCase("G.Blah", true)]
		[TestCase("g.Blah", true)]
		[TestCase("U.", false)]
		[TestCase("u.", false)]
		[TestCase("U.Blah", false)]
		[TestCase("u.Blah", false)]
		public void Static_IsGroup(string subject, bool result) {
			Assert.AreEqual(result, AuthTools.IsGroup(subject), "Wrong result");
		}

	}

}
