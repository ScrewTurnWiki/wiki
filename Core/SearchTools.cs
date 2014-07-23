
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki {
	
	/// <summary>
	/// Implements tools for searching through the wiki.
	/// </summary>
	public static class SearchTools {

		/// <summary>
		/// Searches for pages with name or title similar to a specified value.
		/// </summary>
		/// <param name="name">The name to look for (<c>null</c> for the root).</param>
		/// <param name="nspace">The namespace to search into.</param>
		/// <returns>The similar pages, if any.</returns>
		public static PageInfo[] SearchSimilarPages(string name, string nspace) {
			if(string.IsNullOrEmpty(nspace)) nspace = null;

			SearchResultCollection searchResults = Search(name, false, false, SearchOptions.AtLeastOneWord);

			List<PageInfo> result = new List<PageInfo>(20);

			foreach(SearchResult res in searchResults) {
				PageDocument pageDoc = res.Document as PageDocument;
				if(pageDoc != null) {
					string pageNamespace = NameTools.GetNamespace(pageDoc.PageInfo.FullName);
					if(string.IsNullOrEmpty(pageNamespace)) pageNamespace = null;

					if(pageNamespace == nspace) {
						result.Add(pageDoc.PageInfo);
					}
				}
			}
			
			// Search page names for matches
			List<PageInfo> allPages = Pages.GetPages(Pages.FindNamespace(nspace));
			PageNameComparer comp = new PageNameComparer();
			string currentName = name.ToLowerInvariant();
			foreach(PageInfo page in allPages) {
				if(NameTools.GetLocalName(page.FullName).ToLowerInvariant().Contains(currentName)) {
					if(result.Find(delegate(PageInfo p) { return comp.Compare(p, page) == 0; }) == null) {
						result.Add(page);
					}
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Performs a search in the wiki.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="fullText">A value indicating whether to perform a full-text search.</param>
		/// <param name="searchFilesAndAttachments">A value indicating whether to search through files and attachments.</param>
		/// <param name="options">The search options.</param>
		/// <returns>The results collection.</returns>
		public static SearchResultCollection Search(string query, bool fullText, bool searchFilesAndAttachments, SearchOptions options) {

			// First, search regular page content...
			List<SearchResultCollection> allCollections = new List<SearchResultCollection>(3);

			foreach(IPagesStorageProviderV30 prov in Collectors.PagesProviderCollector.AllProviders) {
				SearchResultCollection currentResults = prov.PerformSearch(new SearchParameters(query, options));

				if(!fullText) {
					// All non title-related matches must be removed
					SearchResultCollection filteredResults = new SearchResultCollection(10);

					foreach(SearchResult res in currentResults) {
						foreach(WordInfo word in res.Matches) {
							if(word.Location == WordLocation.Title) {
								filteredResults.Add(res);
								break;
							}
						}
					}

					allCollections.Add(filteredResults);
				}
				else allCollections.Add(currentResults);
			}

			// ... normalize relevance based on the number of providers
			float providerNormalizationFactor = 1F / (float)Collectors.PagesProviderCollector.AllProviders.Length;
			foreach(SearchResultCollection coll in allCollections) {
				foreach(SearchResult result in coll) {
					result.Relevance.NormalizeAfterFinalization(providerNormalizationFactor);
				}
			}

			if(searchFilesAndAttachments) {
				// ... then build a temporary index for files and attachments...
				StandardIndex temporaryIndex = new StandardIndex();
				uint tempDocumentId = 1;
				uint tempWordId = 1;
				temporaryIndex.IndexChanged += delegate(object sender, IndexChangedEventArgs e) {
					if(e.Change == IndexChangeType.DocumentAdded) {
						List<WordId> ids = null;
						if(e.ChangeData.Words != null) {
							ids = new List<WordId>(20);
							foreach(DumpedWord d in e.ChangeData.Words) {
								ids.Add(new WordId(d.Text, tempWordId));
								tempWordId++;
							}
						}
						e.Result = new IndexStorerResult(tempDocumentId, ids);
						tempDocumentId++;
					}
				};
				temporaryIndex.SetBuildDocumentDelegate(DetectFileOrAttachment);

				foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
					TraverseDirectories(temporaryIndex, prov, null);

					string[] pagesWithAttachments = prov.GetPagesWithAttachments();
					foreach(string page in pagesWithAttachments) {
						// Store attachments for the current page in the index
						PageInfo pageInfo = Pages.FindPage(page);

						// pageInfo can be null if the index is corrupted
						if(pageInfo != null) {
							foreach(string attachment in prov.ListPageAttachments(pageInfo)) {
								FileDetails details = prov.GetPageAttachmentDetails(pageInfo, attachment);
								temporaryIndex.StoreDocument(new PageAttachmentDocument(pageInfo,
									attachment, prov.GetType().FullName, details.LastModified),
									new string[0], "", null);
							}
						}
					}
				}

				// ... then search in the temporary index and normalize relevance
				SearchResultCollection filesAndAttachments = temporaryIndex.Search(new SearchParameters(query, options));
				providerNormalizationFactor = 1F / (float)Collectors.FilesProviderCollector.AllProviders.Length;
				foreach(SearchResult result in filesAndAttachments) {
					result.Relevance.NormalizeAfterFinalization(providerNormalizationFactor);
				}

				allCollections.Add(filesAndAttachments);
			}

			return CombineCollections(allCollections);
		}

		/// <summary>
		/// Detects the document in a dumped instance for files and attachments.
		/// </summary>
		/// <param name="doc">The dumped document instance.</param>
		/// <returns>The proper document instance.</returns>
		private static IDocument DetectFileOrAttachment(DumpedDocument doc) {
			if(doc.TypeTag == FileDocument.StandardTypeTag) {
				return new FileDocument(doc);
			}
			else if(doc.TypeTag == PageAttachmentDocument.StandardTypeTag) {
				return new PageAttachmentDocument(doc);
			}
			else throw new NotSupportedException();
		}

		/// <summary>
		/// Traverses a directory tree, indexing all files.
		/// </summary>
		/// <param name="index">The output index.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="currentDir">The current directory.</param>
		private static void TraverseDirectories(InMemoryIndexBase index, IFilesStorageProviderV30 provider, string currentDir) {
			// Store files in the index
			foreach(string file in provider.ListFiles(currentDir)) {
				FileDetails details = provider.GetFileDetails(file);
				index.StoreDocument(new FileDocument(file, provider.GetType().FullName, details.LastModified),
					new string[0], "", null);
			}

			// Recursively process all sub-directories
			foreach(string directory in provider.ListDirectories(currentDir)) {
				TraverseDirectories(index, provider, directory);
			}
		}

		/// <summary>
		/// Combines a set of <see cref="T:SearchResultCollection" />s into a single object.
		/// </summary>
		/// <param name="collections">The collections.</param>
		/// <returns>The resulting <see cref="T:SearchResultCollection" />.</returns>
		private static SearchResultCollection CombineCollections(List<SearchResultCollection> collections) {
			List<SearchResult> tempResults = new List<SearchResult>(100);

			foreach(SearchResultCollection coll in collections) {
				tempResults.AddRange(coll);
			}

			tempResults.Sort(delegate(SearchResult x, SearchResult y) { return y.Relevance.Value.CompareTo(x.Relevance.Value); });

			SearchResultCollection resultCollection = new SearchResultCollection(50);
			foreach(SearchResult singleResult in tempResults) {
				resultCollection.Add(singleResult);
			}

			return resultCollection;
		}

	}

}
