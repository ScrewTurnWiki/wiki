
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Stores index data to disk.
	/// </summary>
	/// <remarks>Instance and static members are <b>thread-safe</b>.</remarks>
	public class IndexStorer : IndexStorerBase {

		private static readonly byte[] ReservedBytes = new byte[] { 2, 0, 0, 0, 0, 0, 0, 0 };
		private static readonly int Zero = 0;

		private string documentsFile, wordsFile, mappingsFile;

		private uint firstFreeDocumentId = 1;
		private uint firstFreeWordId = 1;

		// Documents file binary format
		// Reserved(8bytes) Count(int) Entries...
		//		ID(int) Name(string) Title(string) TypeTag(string) DateTime(long)

		// Words file binary format
		// Reserved(8bytes) Count(int) Entries...
		//		ID(int) Text(string)

		// Mappings file binary format
		// Reserved(8bytes) Count(int) Entries...
		//		WordID(int) DocumentID(int) FirstCharIndex(int) WordIndex(int) Location(int)

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexStorer" /> class.
		/// </summary>
		/// <param name="documentsFile">The file that contains the documents list.</param>
		/// <param name="wordsFile">The file that contains the words list.</param>
		/// <param name="mappingsFile">The file that contains the index mappings data.</param>
		/// <param name="index">The index to manage.</param>
		public IndexStorer(string documentsFile, string wordsFile, string mappingsFile, IInMemoryIndex index)
			: base(index) {

			if(documentsFile == null) throw new ArgumentNullException("documentsFile");
			if(wordsFile == null) throw new ArgumentNullException("wordsFile");
			if(mappingsFile == null) throw new ArgumentNullException("mappingsFile");

			if(documentsFile.Length == 0) throw new ArgumentException("Documents File cannot be empty", "documentsFile");
			if(wordsFile.Length == 0) throw new ArgumentException("Words File cannot be emtpy", "wordsFile");
			if(mappingsFile.Length == 0) throw new ArgumentException("Mappings File cannot be empty", "mappingsFile");

			this.documentsFile = documentsFile;
			this.wordsFile = wordsFile;
			this.mappingsFile = mappingsFile;
			
			InitFiles();
		}

		/// <summary>
		/// Gets the approximate size, in bytes, of the search engine index.
		/// </summary>
		public override long Size {
			get {
				lock(this) {
					long size = 0;
					FileInfo fi;

					fi = new FileInfo(documentsFile);
					size += fi.Length;

					fi = new FileInfo(wordsFile);
					size += fi.Length;

					fi = new FileInfo(mappingsFile);
					size += fi.Length;

					return size;
				}
			}
		}

		/// <summary>
		/// Loads the index from the data store the first time.
		/// </summary>
		/// <param name="documents">The dumped documents.</param>
		/// <param name="words">The dumped words.</param>
		/// <param name="mappings">The dumped word mappings.</param>
		protected override void LoadIndexInternal(out DumpedDocument[] documents, out DumpedWord[] words, out DumpedWordMapping[] mappings) {
			uint maxDocumentId = 0;
			uint maxWordId = 0;

			// 1. Load Documents
			using(FileStream fs = new FileStream(documentsFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
				int count = ReadCount(fs);
				BinaryReader reader = new BinaryReader(fs, Encoding.UTF8);
				documents = new DumpedDocument[count];
				for(int i = 0; i < count; i++) {
					documents[i] = ReadDumpedDocument(reader);
					if(documents[i].ID > maxDocumentId) maxDocumentId = documents[i].ID;
				}
				firstFreeDocumentId = maxDocumentId + 1;
			}

			// 2. Load Words
			using(FileStream fs = new FileStream(wordsFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
				int count = ReadCount(fs);
				BinaryReader reader = new BinaryReader(fs, Encoding.UTF8);
				words = new DumpedWord[count];
				for(int i = 0; i < count; i++) {
					words[i] = ReadDumpedWord(reader);
					if(words[i].ID > maxWordId) maxWordId = words[i].ID;
				}
				firstFreeWordId = maxWordId + 1;
			}

			// 3. Load Mappings
			using(FileStream fs = new FileStream(mappingsFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
				int count = ReadCount(fs);
				BinaryReader reader = new BinaryReader(fs, Encoding.UTF8);
				mappings = new DumpedWordMapping[count];
				for(int i = 0; i < count; i++) {
					mappings[i] = ReadDumpedWordMapping(reader);
				}
			}
		}

		/// <summary>
		/// Reads the reserved bytes.
		/// </summary>
		/// <param name="reader">The <see cref="BinaryReader" /> to read from.</param>
		/// <returns><c>true</c> if read bytes are equal to expected bytes, <c>false</c> otherwise.</returns>
		private static bool ReadReserved(BinaryReader reader) {
			bool allEqual = true;
			for(int i = 0; i < ReservedBytes.Length; i++) {
				int r = reader.ReadByte();
				if(r != ReservedBytes[i]) allEqual = false;
			}
			return allEqual;
		}

		/// <summary>
		/// Initializes the data files, if needed.
		/// </summary>
		private void InitFiles() {
			if(!File.Exists(documentsFile)) {
				using(FileStream fs = new FileStream(documentsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
					BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
					WriteHeader(writer);
				}
			}

			if(!File.Exists(wordsFile)) {
				using(FileStream fs = new FileStream(wordsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
					BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
					WriteHeader(writer);
				}
			}

			if(!File.Exists(mappingsFile)) {
				using(FileStream fs = new FileStream(mappingsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
					BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
					WriteHeader(writer);
				}
			}
		}

		/// <summary>
		/// Initializes the data storage.
		/// </summary>
		/// <param name="state">A state object passed from the index.</param>
		protected override void InitDataStore(object state) {
			using(FileStream fs = new FileStream(documentsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
				BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
				WriteHeader(writer);
			}

			using(FileStream fs = new FileStream(wordsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
				BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
				WriteHeader(writer);
			}

			using(FileStream fs = new FileStream(mappingsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
				BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
				WriteHeader(writer);
			}
		}

		/// <summary>
		/// Writes the binary file header.
		/// </summary>
		/// <param name="writer">The <see cref="BinaryWriter" /> to write into.</param>
		private static void WriteHeader(BinaryWriter writer) {
			writer.Write(ReservedBytes);
			writer.Write(Zero);
		}

		/// <summary>
		/// Reads a <see cref="DumpedDocument" /> from a <see cref="BinaryReader" />.
		/// </summary>
		/// <param name="reader">The <see cref="BinaryReader" />.</param>
		/// <returns>The <see cref="DumpedDocument" />.</returns>
		private static DumpedDocument ReadDumpedDocument(BinaryReader reader) {
			uint id;
			string name, title, typeTag;
			DateTime dateTime;

			id = reader.ReadUInt32();
			name = reader.ReadString();
			title = reader.ReadString();
			typeTag = reader.ReadString();
			dateTime = DateTime.FromBinary(reader.ReadInt64());

			return new DumpedDocument(id, name, title, typeTag, dateTime);
		}

		/// <summary>
		/// Reads a <see cref="DumpedWord" /> from a <see cref="BinaryReader" />.
		/// </summary>
		/// <param name="reader">The <see cref="BinaryReader" />.</param>
		/// <returns>The <see cref="DumpedWord" />.</returns>
		private static DumpedWord ReadDumpedWord(BinaryReader reader) {
			uint id;
			string text;

			id = reader.ReadUInt32();
			text = reader.ReadString();

			return new DumpedWord(id, text);
		}

		/// <summary>
		/// Reads a <see cref="DumpedWordMapping" /> from a <see cref="BinaryReader" />.
		/// </summary>
		/// <param name="reader">The <see cref="BinaryReader" />.</param>
		/// <returns>The <see cref="DumpedWordMapping" />.</returns>
		private static DumpedWordMapping ReadDumpedWordMapping(BinaryReader reader) {
			uint wordId;
			uint documentId;
			ushort firstCharIndex, wordIndex;
			byte location;

			wordId = reader.ReadUInt32();
			documentId = reader.ReadUInt32();
			firstCharIndex = reader.ReadUInt16();
			wordIndex = reader.ReadUInt16();
			location = reader.ReadByte();

			return new DumpedWordMapping(wordId, documentId, firstCharIndex, wordIndex, location);
		}

		/// <summary>
		/// Reads the count in a <see cref="FileStream" />.
		/// </summary>
		/// <param name="fs">The <see cref="FileStream" />, at position <b>zero</b>.</param>
		/// <returns>The count.</returns>
		/// <remarks>The caller must properly seek the stream after calling the method.</remarks>
		private static int ReadCount(FileStream fs) {
			BinaryReader reader = new BinaryReader(fs, Encoding.UTF8);
			if(!ReadReserved(reader)) {
				throw new InvalidOperationException("Invalid index file header");
			}
			return reader.ReadInt32();
		}

		/// <summary>
		/// Stores new data into the data storage.
		/// </summary>
		/// <param name="data">The data to store.</param>
		/// <param name="state">A state object passed from the index.</param>
		/// <returns>The storer result, if any.</returns>
		/// <remarks>When saving a new document, the document ID in data.Mappings must be
		/// replaced with the currect document ID, generated by the concrete implementation of
		/// this method. data.Words should have IDs numbered from uint.MaxValue downwards. 
		/// The method re-numbers the words appropriately.</remarks>
		protected override IndexStorerResult SaveData(DumpedChange data, object state) {
			IndexStorerResult result = new IndexStorerResult(null, null);

			// 1. Save Document
			using(FileStream fs = new FileStream(documentsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
				int count = ReadCount(fs);
				// Update count and append document
				BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
				fs.Seek(-4, SeekOrigin.Current);
				writer.Write(count + 1);
				writer.Seek(0, SeekOrigin.End);
				data.Document.ID = firstFreeDocumentId;
				WriteDumpedDocument(writer, data.Document);

				result.DocumentID = firstFreeDocumentId;
				firstFreeDocumentId++;
			}

			// 2. Save Words
			Dictionary<uint, WordId> wordIds = null;
			using(FileStream fs = new FileStream(wordsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
				int count = ReadCount(fs);
				// Update count and append words
				BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
				fs.Seek(-4, SeekOrigin.Current);
				writer.Write(count + data.Words.Count);
				fs.Seek(0, SeekOrigin.End);

				wordIds = new Dictionary<uint, WordId>(data.Words.Count);
				foreach(DumpedWord dw in data.Words) {
					wordIds.Add(dw.ID, new WordId(dw.Text, firstFreeWordId));
					dw.ID = firstFreeWordId;
					WriteDumpedWord(writer, dw);
					firstFreeWordId++;
				}
				result.WordIDs = new List<WordId>(wordIds.Values);
			}

			// 3. Save Mappings
			using(FileStream fs = new FileStream(mappingsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
				int count = ReadCount(fs);
				// Update count and append mappings
				BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8);
				fs.Seek(-4, SeekOrigin.Current);
				writer.Write(count + data.Mappings.Count);
				fs.Seek(0, SeekOrigin.End);
				foreach(DumpedWordMapping map in data.Mappings) {
					// Words are autonumbered from uint.MaxValue downwards by IndexBase so that
					// IndexStorer can identify the DumpedWordMappings easily and
					// fix the IDs with the ones actually stored
					WordId newMappingWordId;
					if(wordIds != null && wordIds.TryGetValue(map.WordID, out newMappingWordId)) {
						map.WordID = newMappingWordId.ID;
					}
					WriteDumpedWordMapping(writer,
						new DumpedWordMapping(map.WordID, result.DocumentID.Value,
							map.FirstCharIndex, map.WordIndex, map.Location));
				}
			}

			return result;
		}

		/// <summary>
		/// Gets a tempDumpedWord file name given an original name.
		/// </summary>
		/// <param name="file">The original name.</param>
		/// <returns>The tempDumpedWord file name.</returns>
		private static string GetTempFile(string file) {
			string folder = Path.GetDirectoryName(file);
			string name = Path.GetFileNameWithoutExtension(file) + "_Temp" + Path.GetExtension(file);
			return Path.Combine(folder, name);
		}

		/// <summary>
		/// Deletes data from the data storage.
		/// </summary>
		/// <param name="data">The data to delete.</param>
		/// <param name="state">A state object passed from the index.</param>
		protected override void DeleteData(DumpedChange data, object state) {
			// Files are regenerated in a tempDumpedWord location and copied back
			string tempDocumentsFile = GetTempFile(documentsFile);
			string tempWordsFile = GetTempFile(wordsFile);
			string tempMappingsFile = GetTempFile(mappingsFile);

			// 1. Remove Mappings
			using(FileStream fsi = new FileStream(mappingsFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
				int count = ReadCount(fsi);
				int countLocation = (int)fsi.Position - 4;
				int writeCount = 0;
				BinaryReader reader = new BinaryReader(fsi, Encoding.UTF8);
				using(FileStream fso = new FileStream(tempMappingsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
					BinaryWriter writer = new BinaryWriter(fso, Encoding.UTF8);
					WriteHeader(writer);
					DumpedWordMapping m;
					for(int i = 0; i < count; i++) {
						m = ReadDumpedWordMapping(reader);
						// If m is not contained in data.Mappings, store it in tempDumpedWord file
						if(!Find(m, data.Mappings)) {
							WriteDumpedWordMapping(writer, m);
							writeCount++;
						}
					}
					writer.Seek(countLocation, SeekOrigin.Begin);
					writer.Write(writeCount);
				}
			}
			// Replace the file
			File.Copy(tempMappingsFile, mappingsFile, true);
			File.Delete(tempMappingsFile);

			// 2. Remove Words
			using(FileStream fsi = new FileStream(wordsFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
				int count = ReadCount(fsi);
				int countLocation = (int)fsi.Position - 4;
				int writeCount = 0;
				BinaryReader reader = new BinaryReader(fsi, Encoding.UTF8);
				using(FileStream fso = new FileStream(tempWordsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
					BinaryWriter writer = new BinaryWriter(fso, Encoding.UTF8);
					WriteHeader(writer);
					DumpedWord w;
					for(int i = 0; i < count; i++) {
						w = ReadDumpedWord(reader);
						// If w is not contained in data.Words, store it in tempDumpedWord file
						if(!Find(w, data.Words)) {
							WriteDumpedWord(writer, w);
							writeCount++;
						}
					}
					writer.Seek(countLocation, SeekOrigin.Begin);
					writer.Write(writeCount);
				}
			}
			// Replace the file
			File.Copy(tempWordsFile, wordsFile, true);
			File.Delete(tempWordsFile);

			// 3. Remove Document
			using(FileStream fsi = new FileStream(documentsFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
				int count = ReadCount(fsi);
				int countLocation = (int)fsi.Position - 4;
				BinaryReader reader = new BinaryReader(fsi, Encoding.UTF8);
				using(FileStream fso = new FileStream(tempDocumentsFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
					BinaryWriter writer = new BinaryWriter(fso, Encoding.UTF8);
					WriteHeader(writer);
					DumpedDocument d;
					for(int i = 0; i < count; i++) {
						d = ReadDumpedDocument(reader);
						// If d is not equal to data.Document (to be deleted), then copy it to the result file
						if(!EqualDumpedDocument(d, data.Document)) {
							WriteDumpedDocument(writer, d);
						}
					}
					writer.Seek(countLocation, SeekOrigin.Begin);
					writer.Write(count - 1);
				}
			}
			File.Copy(tempDocumentsFile, documentsFile, true);
			File.Delete(tempDocumentsFile);
		}

		/// <summary>
		/// Writes a <see cref="DumpedDocument" /> to a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="writer">The <see cref="BinaryWriter" />.</param>
		/// <param name="document">The <see cref="DumpedDocument" />.</param>
		private static void WriteDumpedDocument(BinaryWriter writer, DumpedDocument document) {
			writer.Write(document.ID);
			writer.Write(document.Name);
			writer.Write(document.Title);
			writer.Write(document.TypeTag);
			writer.Write(document.DateTime.ToBinary());
		}

		/// <summary>
		/// Writes a <see cref="DumpedWord" /> to a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="writer">The <see cref="BinaryWriter" />.</param>
		/// <param name="word">The <see cref="DumpedWord" />.</param>
		private static void WriteDumpedWord(BinaryWriter writer, DumpedWord word) {
			//if(word.Text.Length == 0) throw new InvalidOperationException();

			writer.Write(word.ID);
			writer.Write(word.Text);
		}

		/// <summary>
		/// Writes a <see cref="DumpedWordMapping" /> to a <see cref="BinaryWriter" />.
		/// </summary>
		/// <param name="writer">The <see cref="BinaryWriter" />.</param>
		/// <param name="mapping">The <see cref="DumpedWordMapping" />.</param>
		private static void WriteDumpedWordMapping(BinaryWriter writer, DumpedWordMapping mapping) {
			writer.Write(mapping.WordID);
			writer.Write(mapping.DocumentID);
			writer.Write(mapping.FirstCharIndex);
			writer.Write(mapping.WordIndex);
			writer.Write(mapping.Location);
		}

		/// <summary>
		/// Determines whether two <see cref="DumpedDocument" />s are equal.
		/// </summary>
		/// <param name="d1">The first document.</param>
		/// <param name="d2">The second document.</param>
		/// <returns><c>true</c> if the documents are equal, <c>false</c> otherwise.</returns>
		private static bool EqualDumpedDocument(DumpedDocument d1, DumpedDocument d2) {
			// Only consider ID, Name and TypeTag
			//return d1.ID == d2.ID && d1.Name == d2.Name && d1.Title == d2.Title &&
			//	d1.TypeTag == d2.TypeTag && d1.DateTime == d2.DateTime;
			return d1.ID == d2.ID && d1.Name == d2.Name && d1.TypeTag == d2.TypeTag;
		}

	}

}
