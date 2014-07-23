
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Represents a word structured for easy dumping to disk or database.
	/// </summary>
	/// <remarks>The class is <b>not thread-safe</b>.</remarks>
	public class DumpedWord {

		/// <summary>
		/// The word unique ID.
		/// </summary>
		protected uint id;
		/// <summary>
		/// The word culture-invariant lowercase text.
		/// </summary>
		protected string text;

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpedWord" /> class.
		/// </summary>
		/// <param name="id">The unique word ID.</param>
		/// <param name="text">The word culture-invariant lowercase text.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="text"/> is empty.</exception>
		public DumpedWord(uint id, string text) {
			if(text == null) throw new ArgumentNullException("text");
			if(text.Length == 0) throw new ArgumentException("Text cannot be empty", "text");

			this.id = id;
			this.text = text;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpedWord" /> class.
		/// </summary>
		/// <param name="word">The word to extract the information from.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="word"/> is <c>null</c>.</exception>
		public DumpedWord(Word word) {
			if(word == null) throw new ArgumentNullException("word");

			this.id = word.ID;
			this.text = word.Text;
		}

		/// <summary>
		/// Gets or sets the word unique ID.
		/// </summary>
		public uint ID {
			get { return id; }
			set { id = value; }
		}

		/// <summary>
		/// Gets the word culture-invariant lowercase text.
		/// </summary>
		public string Text {
			get { return text; }
		}

	}

}
