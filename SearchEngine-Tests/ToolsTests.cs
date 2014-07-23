
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {
	
	[TestFixture]
	public class ToolsTests : TestsBase {

		[Test]
		public void RemoveDiacriticsAndPunctuation() {
			string testPhrase = "Wow, thìs thing sèems really cool!";
			string testWord = "Wòrd";

			Assert.AreEqual("wow this thing seems really cool", Tools.RemoveDiacriticsAndPunctuation(testPhrase, false), "Wrong normalized phrase");
			Assert.AreEqual("word", Tools.RemoveDiacriticsAndPunctuation(testWord, true), "Wrong normalized word");
		}

		[Test]
		public void IsSplitChar() {
			foreach(char c in ",.;:-\"'!?^=()<>\\|/[]{}«»*°§%&#@~©®±") {
				Assert.IsTrue(Tools.IsSplitChar(c), "Char is a split char");
			}
			foreach(char c in "abcdefghijklmnopqrstuvwxyz0123456789òçàùèéì€$£") {
				Assert.IsFalse(Tools.IsSplitChar(c), "Char is not a split char");
			}
		}

		[Test]
		public void SkipSplitChars() {
			Assert.AreEqual(0, Tools.SkipSplitChars(0, "hello"));
			Assert.AreEqual(1, Tools.SkipSplitChars(0, " hello"));
			Assert.AreEqual(7, Tools.SkipSplitChars(6, "Hello! How are you?"));
		}

		[Test]
		public void Tokenize() {
			string input = "Hello, there!";
			WordInfo[] expectedOutput = new WordInfo[] { new WordInfo("Hello", 0, 0, WordLocation.Content), new WordInfo("there", 7, 1, WordLocation.Content) };

			WordInfo[] output = Tools.Tokenize(input, WordLocation.Content);

			Assert.AreEqual(expectedOutput.Length, output.Length, "Wrong output length");

			for(int i = 0; i < output.Length; i++) {
				Assert.AreEqual(expectedOutput[i].Text, output[i].Text, "Wrong word text at index " + i.ToString());
				Assert.AreEqual(expectedOutput[i].FirstCharIndex, output[i].FirstCharIndex, "Wrong first char index at " + i.ToString());
				Assert.AreEqual(expectedOutput[i].WordIndex, output[i].WordIndex, "Wrong word index at " + i.ToString());
			}
		}

		[Test]
		public void Tokenize_OneWord() {
			string input = "todo";
			WordInfo[] expectedOutput = new WordInfo[] { new WordInfo("todo", 0, 0, WordLocation.Content) };

			WordInfo[] output = Tools.Tokenize(input, WordLocation.Content);

			Assert.AreEqual(expectedOutput.Length, output.Length, "Wrong output length");

			for(int i = 0; i < output.Length; i++) {
				Assert.AreEqual(expectedOutput[i].Text, output[i].Text, "Wrong word text at index " + i.ToString());
				Assert.AreEqual(expectedOutput[i].FirstCharIndex, output[i].FirstCharIndex, "Wrong first char index at " + i.ToString());
				Assert.AreEqual(expectedOutput[i].WordIndex, output[i].WordIndex, "Wrong word index at " + i.ToString());
			}
		}

		[Test]
		public void Tokenize_OneWordWithSplitChar() {
			string input = "todo.";
			WordInfo[] expectedOutput = new WordInfo[] { new WordInfo("todo", 0, 0, WordLocation.Content) };

			WordInfo[] output = Tools.Tokenize(input, WordLocation.Content);

			Assert.AreEqual(expectedOutput.Length, output.Length, "Wrong output length");

			for(int i = 0; i < output.Length; i++) {
				Assert.AreEqual(expectedOutput[i].Text, output[i].Text, "Wrong word text at index " + i.ToString());
				Assert.AreEqual(expectedOutput[i].FirstCharIndex, output[i].FirstCharIndex, "Wrong first char index at " + i.ToString());
				Assert.AreEqual(expectedOutput[i].WordIndex, output[i].WordIndex, "Wrong word index at " + i.ToString());
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Tokenize_NullText() {
			Tools.Tokenize(null, WordLocation.Content);
		}

		[Test]
		public void RemoveStopWords() {
			WordInfo[] input = new WordInfo[] { new WordInfo("I", 0, 0, WordLocation.Content), new WordInfo("like", 7, 1, WordLocation.Content),
				new WordInfo("the", 15, 2, WordLocation.Content), new WordInfo("cookies", 22, 3, WordLocation.Content) };
			WordInfo[] expectedOutput = new WordInfo[] { new WordInfo("I", 0, 0, WordLocation.Content), new WordInfo("like", 7, 1, WordLocation.Content),
				new WordInfo("cookies", 22, 3, WordLocation.Content) };

			WordInfo[] output = Tools.RemoveStopWords(input, new string[] { "the", "in", "of" });

			Assert.AreEqual(expectedOutput.Length, output.Length, "Wrong output length");

			for(int i = 0; i < output.Length; i++) {
				Assert.AreEqual(expectedOutput[i].Text, output[i].Text, "Wrong word text at index " + i.ToString());
				Assert.AreEqual(expectedOutput[i].FirstCharIndex, output[i].FirstCharIndex, "Wrong word position at index " + i.ToString());
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveStopWords_NullInputWords() {
			Tools.RemoveStopWords(null, new string[0]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveStopWords_NullStopWords() {
			Tools.RemoveStopWords(new WordInfo[0], null);
		}

	}

}
