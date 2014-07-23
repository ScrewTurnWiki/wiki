
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class SearchParametersTests {

		[Test]
		public void Constructor_QueryOnly() {
			SearchParameters par = new SearchParameters("query");
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.AtLeastOneWord, par.Options);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_QueryOnly_InvalidQuery(string q) {
			SearchParameters par = new SearchParameters(q);
		}

		[Test]
		public void Constructor_QueryDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", "blah", "doc");
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.AreEqual(2, par.DocumentTypeTags.Length, "Wrong DocumentTypeTag count");
			Assert.AreEqual("blah", par.DocumentTypeTags[0], "Wrong type tag");
			Assert.AreEqual("doc", par.DocumentTypeTags[1], "Wrong type tag");
			Assert.AreEqual(SearchOptions.AtLeastOneWord, par.Options);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_QueryDocumentTypeTags_InvalidQuery(string q) {
			SearchParameters par = new SearchParameters(q, "blah", "doc");
		}

		[Test]
		public void Constructor_QueryDocumentTypeTags_NullDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", null);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.AtLeastOneWord, par.Options);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Constructor_QueryDocumentTypeTags_EmptyDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", new string[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_QueryDocumentTypeTags_InvalidDocumentTypeTagsElement(string e) {
			SearchParameters par = new SearchParameters("query", new string[] { "blah", e });
		}

		[Test]
		public void Constructor_QueryOptions() {
			SearchParameters par = new SearchParameters("query", SearchOptions.ExactPhrase);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.ExactPhrase, par.Options);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_QueryOptions_InvalidQuery(string q) {
			SearchParameters par = new SearchParameters(q, SearchOptions.ExactPhrase);
		}

		[Test]
		public void Constructor_Full() {
			SearchParameters par = new SearchParameters("query", new string[] { "blah", "doc" }, SearchOptions.AllWords);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.AreEqual(2, par.DocumentTypeTags.Length, "Wrong DocumentTypeTag count");
			Assert.AreEqual("blah", par.DocumentTypeTags[0], "Wrong type tag");
			Assert.AreEqual("doc", par.DocumentTypeTags[1], "Wrong type tag");
			Assert.AreEqual(SearchOptions.AllWords, par.Options);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_Full_InvalidQuery(string q) {
			SearchParameters par = new SearchParameters(q, new string[] { "blah", "doc" }, SearchOptions.AllWords);
		}

		[Test]
		public void Constructor_Full_NullDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", null, SearchOptions.AllWords);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.AllWords, par.Options);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Constructor_Full_EmptyDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", new string[0], SearchOptions.AllWords);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_Full_InvalidDocumentTypeTagsElement(string e) {
			SearchParameters par = new SearchParameters("query", new string[] { "blah", e }, SearchOptions.ExactPhrase);
		}

	}

}
