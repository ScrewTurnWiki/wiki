
using System;
using System.Collections.Generic;
using System.IO;
using ScrewTurn.Wiki.PluginFramework;
using System.Text.RegularExpressions;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages snippets.
	/// </summary>
	public static class Snippets {

		/// <summary>
		/// Gets the complete list of the Snippets.
		/// </summary>
		/// <returns>The snippets, sorted by name.</returns>
		public static List<Snippet> GetSnippets() {
			List<Snippet> allSnippets = new List<Snippet>(50);

			// Retrieve all snippets from Pages Provider
			foreach(IPagesStorageProviderV30 provider in Collectors.PagesProviderCollector.AllProviders) {
				allSnippets.AddRange(provider.GetSnippets());
			}

			allSnippets.Sort(new SnippetNameComparer());

			return allSnippets;
		}

		/// <summary>
		/// Finds a Snippet.
		/// </summary>
		/// <param name="name">The Name of the Snippet to find.</param>
		/// <returns>The Snippet or null if it is not found.</returns>
		public static Snippet Find(string name) {
			List<Snippet> allSnippets = GetSnippets();

			int result = allSnippets.BinarySearch(new Snippet(name, "", null), new SnippetNameComparer());

			if(allSnippets.Count > 0 && result >= 0) return allSnippets[result];
			else return null;
		}

		/// <summary>
		/// Creates a new Snippet.
		/// </summary>
		/// <param name="name">The name of the Snippet.</param>
		/// <param name="content">The content of the Snippet.</param>
		/// <param name="provider">The Provider to use to store the Snippet (<c>null</c> for the default provider).</param>
		/// <returns>True if the Snippets has been addedd successfully.</returns>
		public static bool AddSnippet(string name, string content, IPagesStorageProviderV30 provider) {
			if(Find(name) != null) return false;

			if(provider == null) provider = Collectors.PagesProviderCollector.GetProvider(Settings.DefaultPagesProvider);

			Snippet newSnippet = provider.AddSnippet(name, content);

			if(newSnippet != null) {
				Log.LogEntry("Snippet " + name + " created", EntryType.General, Log.SystemUsername);
				Content.ClearPseudoCache();
				Content.InvalidateAllPages();
			}
			else Log.LogEntry("Creation failed for Snippet " + name, EntryType.Error, Log.SystemUsername);

			return newSnippet != null;
		}

		/// <summary>
		/// Removes a Snippet.
		/// </summary>
		/// <param name="snippet">The Snippet to remove.</param>
		/// <returns>True if the Snippet has been removed successfully.</returns>
		public static bool RemoveSnippet(Snippet snippet) {
			bool done = snippet.Provider.RemoveSnippet(snippet.Name);

			if(done) {
				Log.LogEntry("Snippet " + snippet.Name + " deleted", EntryType.General, Log.SystemUsername);
				Content.ClearPseudoCache();
				Content.InvalidateAllPages();
			}
			else Log.LogEntry("Deletion failed for Snippet " + snippet.Name, EntryType.Error, Log.SystemUsername);

			return done;
		}

		/// <summary>
		/// Modifies the Content of a Snippet.
		/// </summary>
		/// <param name="snippet">The Snippet to update.</param>
		/// <param name="content">The new Content.</param>
		/// <returns>True if the Snippet has been updated successfully.</returns>
		public static bool ModifySnippet(Snippet snippet, string content) {
			Snippet newSnippet = snippet.Provider.ModifySnippet(snippet.Name, content);

			if(newSnippet != null) {
				Log.LogEntry("Snippet " + snippet.Name + " updated", EntryType.General, Log.SystemUsername);
				Content.ClearPseudoCache();
				Content.InvalidateAllPages();
			}
			else Log.LogEntry("Modification failed for Snippet " + snippet.Name, EntryType.Error, Log.SystemUsername);

			return newSnippet != null;
		}

		/// <summary>
		/// The regular expression to use for extracting parameters.
		/// </summary>
		public static readonly Regex ParametersRegex = new Regex("\\?[a-zA-Z0-9_-]+\\?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		/// <summary>
		/// Counts the parameters in a snippet.
		/// </summary>
		/// <param name="snippet">The snippet.</param>
		/// <returns>The number of parameters.</returns>
		public static int CountParameters(Snippet snippet) {
			return ExtractParameterNames(snippet).Length;
		}

		/// <summary>
		/// Finds the parameters in a snippet.
		/// </summary>
		/// <param name="snippet">The snippet.</param>
		/// <returns>The parameter names.</returns>
		public static string[] ExtractParameterNames(Snippet snippet) {
			List<string> parms = new List<string>();
			foreach(Match m in ParametersRegex.Matches(snippet.Content)) {
				string value = m.Value.Substring(1, m.Value.Length - 2);
				if(m.Success && !parms.Contains(value)) parms.Add(value);
			}
			return parms.ToArray();
		}

	}

}
