
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ScrewTurn.Wiki.SearchEngine {
	
	/// <summary>
	/// Implements useful methods.
	/// </summary>
	public static class Tools {

		#region Main Search Algorithms

		/// <summary>
		/// Performs a search in the index.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="documentTypeTags">The document type tags to include in the search.</param>
		/// <param name="filterDocumentType"><c>true</c> to apply the filter on the document type.</param>
		/// <param name="options">The search options.</param>
		/// <param name="fetcher">An object that is able to fetch words.</param>
		/// <returns>The results.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="query"/> or <paramref name="fetcher"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="query"/> is empty.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="filterDocumentType"/> is <c>true</c> and <paramref name="documentTypeTags"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filterDocumentType"/> is <c>true</c> and <paramref name="documentTypeTags"/> is empty.</exception>
		public static SearchResultCollection SearchInternal(string query, string[] documentTypeTags, bool filterDocumentType, SearchOptions options, IWordFetcher fetcher) {
			if(query == null) throw new ArgumentNullException("query");
			if(query.Length == 0) throw new ArgumentException("Query cannot be empty", "query");

			if(filterDocumentType && documentTypeTags == null) throw new ArgumentNullException("documentTypeTags");
			if(filterDocumentType && documentTypeTags.Length == 0) throw new ArgumentException("documentTypeTags cannot be empty", "documentTypeTags");

			if(fetcher == null) throw new ArgumentNullException("fetcher");

			SearchResultCollection results = new SearchResultCollection();

			query = query.ToLowerInvariant();
			string[] queryWords = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			float totalRelevance = 0;

			Word word = null;
			foreach(string q in queryWords) {
				if(fetcher.TryGetWord(q, out word)) {
					foreach(IDocument doc in word.Occurrences.Keys) {
						// Skip documents with excluded tags
						if(filterDocumentType &&
							!IsDocumentTypeTagIncluded(doc.TypeTag, documentTypeTags)) continue;
						foreach(BasicWordInfo info in word.Occurrences[doc]) {
							// If a search result is already present, add a new match to it,
							// otherwise create a new search result object
							WordInfo mi = new WordInfo(q, info.FirstCharIndex, info.WordIndex, info.Location);
							SearchResult res = results.GetSearchResult(doc);
							if(res == null) {
								res = new SearchResult(doc);
								res.Relevance.SetValue(info.Location.RelativeRelevance);
								res.Matches.Add(mi);
								results.Add(res);
							}
							else {
								// Avoid adding duplicate matches (happens when query contains the same word multiple times)
								if(!res.Matches.ContainsOccurrence(mi.Text, mi.FirstCharIndex)) {
									res.Matches.Add(mi);
								}
								res.Relevance.SetValue(res.Relevance.Value + info.Location.RelativeRelevance);
							}
							totalRelevance += info.Location.RelativeRelevance;
						}
					}
				}
			}

			if(options == SearchOptions.AllWords) {
				totalRelevance -= PurgeResultsForAllWords(results, queryWords);
			}
			else if(options == SearchOptions.ExactPhrase) {
				totalRelevance -= PurgeResultsForExactPhrase(results, queryWords);
			}
			else if(options == SearchOptions.AtLeastOneWord) {
				// Nothing to do
			}
			else throw new InvalidOperationException("Unsupported SearchOptions");

			// Finalize relevance values
			for(int i = 0; i < results.Count; i++) {
				results[i].Relevance.Finalize(totalRelevance);
			}

			return results;
		}

		/// <summary>
		/// Purges the invalid results when SearchOptions is AllWords.
		/// </summary>
		/// <param name="results">The results to purge.</param>
		/// <param name="queryWords">The query words.</param>
		/// <returns>The relevance value of the removed matches.</returns>
		public static float PurgeResultsForAllWords(SearchResultCollection results, string[] queryWords) {
			// Remove results that do not contain all the searched words
			float relevanceToRemove = 0;
			List<SearchResult> toRemove = new List<SearchResult>();
			foreach(SearchResult r in results) {
				if(r.Matches.Count < queryWords.Length) toRemove.Add(r);
				else {
					foreach(string w in queryWords) {
						if(!r.Matches.Contains(w)) {
							toRemove.Add(r);
							break;
						}
					}
				}
			}
			foreach(SearchResult r in toRemove) {
				results.Remove(r);
				relevanceToRemove += r.Relevance.Value;
			}
			return relevanceToRemove;
		}

		/// <summary>
		/// Purges the invalid results when SearchOptions is ExactPhrase.
		/// </summary>
		/// <param name="results">The results to purge.</param>
		/// <param name="queryWords">The query words.</param>
		/// <returns>The relevance value of the removed matches.</returns>
		public static float PurgeResultsForExactPhrase(SearchResultCollection results, string[] queryWords) {
			// Remove results that do not contain the exact phrase
			float relevanceToRemove = 0;
			List<SearchResult> toRemove = new List<SearchResult>();
			foreach(SearchResult r in results) {
				// Shortcut
				if(r.Matches.Count < queryWords.Length) toRemove.Add(r);
				else {
					// Verify that all matches are in the same order as in the query
					// and that their indices make up contiguous words,
					// re-iterating from every word in the result, for example:
					// query = 'repeated content', result = 'content repeated content'
					// result must be tested with 'content repeated' (failing) and with 'repeated content' (succeeding)

					int maxTestShift = 0;
					if(queryWords.Length < r.Matches.Count) {
						maxTestShift = r.Matches.Count - queryWords.Length;
					}

					bool sequenceFound = false;

					for(int shift = 0; shift <= maxTestShift; shift++) {
						int firstWordIndex = r.Matches[shift].WordIndex;
						bool allOk = true;

						for(int i = 0; i < queryWords.Length; i++) {
							if(queryWords[i] != r.Matches[i + shift].Text.ToLowerInvariant() ||
								r.Matches[i + shift].WordIndex != firstWordIndex + i) {
								//toRemove.Add(r);
								allOk = false;
								break;
							}
						}

						if(allOk) {
							sequenceFound = true;
							break;
						}
					}

					if(!sequenceFound) {
						toRemove.Add(r);
					}
				}
			}
			foreach(SearchResult r in toRemove) {
				results.Remove(r);
				relevanceToRemove += r.Relevance.Value;
			}
			return relevanceToRemove;
		}

		/// <summary>
		/// Determines whether a document tag is contained in a tag array.
		/// </summary>
		/// <param name="currentTag">The tag to check for.</param>
		/// <param name="includedTags">The tag array.</param>
		/// <returns><c>true</c> if <b>currentTag</b> is contained in <b>includedTags</b>, <c>false</c> otherwise.</returns>
		/// <remarks>The comparison is case-insensitive.</remarks>
		public static bool IsDocumentTypeTagIncluded(string currentTag, string[] includedTags) {
			currentTag = currentTag.ToLowerInvariant();
			foreach(string s in includedTags) {
				if(s.ToLowerInvariant() == currentTag) return true;
			}
			return false;
		}

		#endregion

		/// <summary>
		/// Cleans up keyworks from invalid characters.
		/// </summary>
		/// <param name="keywords">The keywords to cleanup.</param>
		/// <returns>The clean keywords.</returns>
		public static string[] CleanupKeywords(string[] keywords) {
			if(keywords == null || keywords.Length == 0) return keywords;

			List<string> result = new List<string>(keywords.Length);
			foreach(string k in keywords) {
				string temp = RemoveDiacriticsAndPunctuation(k.Replace(" ", ""), true);
				if(temp.Length > 0) {
					result.Add(temp);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Removes "accents" and punctuation from a string, transforming it to lowercase (culture invariant).
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="isSingleWord">A value indicating whether the input string is a single word.</param>
		/// <returns>The normalized string (lowercase, culture invariant). <b>Can be empty.</b></returns>
		public static string RemoveDiacriticsAndPunctuation(string input, bool isSingleWord) {
			// Code partially borrowed from:
			// http://weblogs.asp.net/fmarguerie/archive/2006/10/30/removing-diacritics-accents-from-strings.aspx

			string normalizedString = input.Normalize(NormalizationForm.FormD);
			StringBuilder stringBuilder = new StringBuilder(input.Length);

			for(int i = 0; i < normalizedString.Length; i++) {
				char c = normalizedString[i];
				if(char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) {
					if(char.IsLetterOrDigit(c)) stringBuilder.Append(c);
					else if(!isSingleWord) stringBuilder.Append(" ");
				}
			}

			if(!isSingleWord) {
				while(stringBuilder.ToString().Contains("  ")) stringBuilder.Replace("  ", " ");
			}

			return stringBuilder.ToString().ToLowerInvariant().Trim(' ', '\'', '"');
		}

		/// <summary>
		/// Determines whether a char is a split char.
		/// </summary>
		/// <param name="current">The current char.</param>
		/// <returns><c>true</c> if the char is a split char, <c>false</c> otherwise.</returns>
		public static bool IsSplitChar(char current) {
			UnicodeCategory cat = char.GetUnicodeCategory(current);

			// http://msdn.microsoft.com/en-us/library/system.globalization.unicodecategory.aspx
			// A split char is anything but the following categories
			return
				cat != UnicodeCategory.UppercaseLetter &&
				cat != UnicodeCategory.LowercaseLetter &&
				cat != UnicodeCategory.TitlecaseLetter &&
				cat != UnicodeCategory.ModifierLetter &&
				cat != UnicodeCategory.OtherLetter &&
				cat != UnicodeCategory.NonSpacingMark &&
				cat != UnicodeCategory.DecimalDigitNumber &&
				cat != UnicodeCategory.LetterNumber &&
				cat != UnicodeCategory.OtherNumber &&
				cat != UnicodeCategory.CurrencySymbol;
		}

		/// <summary>
		/// Computes the index of the first non-split char given a start index.
		/// </summary>
		/// <param name="startIndex">The start index.</param>
		/// <param name="content">The content.</param>
		/// <returns>The index of the first non-split char.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="content"/> is <c>null</c>.</exception>
		public static ushort SkipSplitChars(ushort startIndex, string content) {
			if(content == null) throw new ArgumentNullException("content");

			// startIndex < 0 is not actually a problem, so it's possible to set it to zero
			if(startIndex < 0) startIndex = 0;

			int currentIndex = startIndex;
			while(currentIndex < content.Length && IsSplitChar(content[currentIndex])) currentIndex++;
			return (ushort)currentIndex;
		}

		/// <summary>
		/// Tokenizes a string.
		/// </summary>
		/// <param name="text">The text to tokenize.</param>
		/// <param name="location">The location of the words that are extracted.</param>
		/// <returns>The tokens.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> is <c>null</c>.</exception>
		public static WordInfo[] Tokenize(string text, WordLocation location) {
			if(text == null) throw new ArgumentNullException("text");

			List<WordInfo> words = new List<WordInfo>(text.Length / 5); // Average 5 chars/word

			ushort currentIndex = 0, currentWordStart;

			// Skip all trailing splitChars
			currentIndex = SkipSplitChars(0, text);

			currentWordStart = currentIndex;

			while(currentIndex < text.Length && currentIndex < 65500) {
				while(currentIndex < text.Length && !Tools.IsSplitChar(text[currentIndex])) currentIndex++;
				string w = text.Substring(currentWordStart, currentIndex - currentWordStart);
				w = Tools.RemoveDiacriticsAndPunctuation(w, true);
				if(!string.IsNullOrEmpty(w)) {
					words.Add(new WordInfo(w, currentWordStart, (ushort)words.Count, location));
				}
				currentIndex = SkipSplitChars((ushort)(currentIndex + 1), text);
				currentWordStart = currentIndex;
			}

			return words.ToArray();
		}

		/// <summary>
		/// Tokenizes a string.
		/// </summary>
		/// <param name="text">The text to tokenize.</param>
		public static WordInfo[] Tokenize(string text) {
			return Tokenize(text, WordLocation.Content);
		}

		/// <summary>
		/// Removes stop words from a set of words (case insensitive).
		/// </summary>
		/// <param name="words">The input words.</param>
		/// <param name="stopWords">The array of stop words.</param>
		/// <returns>The input words without the stop words.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="words"/> or <paramref name="stopWords"/> are <c>null</c>.</exception>
		public static WordInfo[] RemoveStopWords(WordInfo[] words, string[] stopWords) {
			if(words == null) throw new ArgumentNullException("words");
			if(stopWords == null) throw new ArgumentNullException("stopWords");

			List<WordInfo> result = new List<WordInfo>(words.Length);

			foreach(WordInfo current in words) {
				bool found = false;
				foreach(string sw in stopWords) {
					if(string.Compare(current.Text, sw, true, CultureInfo.InvariantCulture) == 0) {
						found = true;
						break;
					}
				}
				if(!found) result.Add(current);
			}

			return result.ToArray();
		}

	}

	/// <summary>
	/// Defines the interface for a component that fetches words.
	/// </summary>
	public interface IWordFetcher : IDisposable {

		/// <summary>
		/// Tries to get a word.
		/// </summary>
		/// <param name="text">The text of the word.</param>
		/// <param name="word">The found word, if any, <c>null</c> otherwise.</param>
		/// <returns><c>true</c> if the word is found, <c>false</c> otherwise.</returns>
		bool TryGetWord(string text, out Word word);

	}

}
