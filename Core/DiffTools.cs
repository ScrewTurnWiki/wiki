
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Provides methods for diffing text and items.
	/// </summary>
	public static class DiffTools {

		/// <summary>
		/// Computes the difference between two revisions.
		/// </summary>
		/// <param name="rev1">The first revision.</param>
		/// <param name="rev2">The second revision.</param>
		/// <returns>The XHTML-formatted result.</returns>
		public static string DiffRevisions(string rev1, string rev2) {
			string[] aLines = rev1.Split('\n');
			string[] bLines = rev2.Split('\n');
			Difference.Item[] f = Difference.DiffText(rev1, rev2, true, false, false);

			StringBuilder result = new StringBuilder();

			result.Append(@"<table cellpadding=""0"" cellspacing=""0"" style=""color: #000000; background-color: #FFFFFF;"">");

			int n = 0;
			for(int fdx = 0; fdx < f.Length; fdx++) {
				Difference.Item aItem = f[fdx];
				// Write unchanged lines
				while((n < aItem.StartB) && (n < bLines.Length)) {
					result.Append(WriteLine(n, "", bLines[n]));
					n++;
				}

				// Write deleted lines
				for(int m = 0; m < aItem.deletedA; m++) {
					result.Append(WriteLine(-1, "d", aLines[aItem.StartA + m]));
				}

				// Write inserted lines
				while(n < aItem.StartB + aItem.insertedB) {
					result.Append(WriteLine(n, "i", bLines[n]));
					n++;
				}
			}

			// Write the rest of unchanged lines
			while(n < bLines.Length) {
				result.Append(WriteLine(n, "", bLines[n]));
				n++;
			}

			result.Append("</table>");

			return result.ToString();
		}

		private static string WriteLine(int n, string typ, string line) {
			StringBuilder sb = new StringBuilder();
			sb.Append("<tr>");

			sb.Append(@"<td valign=""top"" width=""30"" style=""font-family: Courier New, monospace;"">");
			if(n >= 0) sb.Append(((int)(n + 1)).ToString());
			else sb.Append("&nbsp;");
			sb.Append("</td>");

			sb.Append(@"<td valign=""top"" style=""font-family: Courier New, monospace;"">");
			sb.Append(@"<div style=""");
			switch(typ) {
				case "i":
					sb.Append("background-color: #88CC33;");
					break;
				case "d":
					sb.Append("background-color: #FFDF66;");
					break;
			}
			sb.Append(@""">" + HttpContext.Current.Server.HtmlEncode(line) + "</div>");
			sb.Append("</td>");

			sb.Append("</tr>");
			return sb.ToString();
		}

	}

	/// <summary>
	/// O(ND) Difference Algorithm for C#
	/// Created by Matthias Hertel, see http://www.mathertel.de
	/// This work is licensed under a Creative Commons Attribution 2.0 Germany License.
	/// see http://creativecommons.org/licenses/by/2.0/de/
	/// </summary>
	public class Difference {

		/// <summary>details of one difference.</summary>
		public struct Item {
			/// <summary>Start Line number in Data A.</summary>
			public int StartA;
			/// <summary>Start Line number in Data B.</summary>
			public int StartB;

			/// <summary>Number of changes in Data A.</summary>
			public int deletedA;
			/// <summary>Number of changes in Data A.</summary>
			public int insertedB;
		} // Item

		/// <summary>
		/// Shortest Middle Snake Return Data
		/// </summary>
		private struct SMSRD {
			internal int x, y;
			// internal int u, v;  // 2002.09.20: no need for 2 points 
		}

		/// <summary>
		/// Find the difference in 2 texts, comparing by textlines.
		/// </summary>
		/// <param name="TextA">A-version of the text (usualy the old one)</param>
		/// <param name="TextB">B-version of the text (usualy the new one)</param>
		/// <returns>Returns a array of Items that describe the differences.</returns>
		public Item[] DiffText(string TextA, string TextB) {
			return (DiffText(TextA, TextB, false, false, false));
		} // DiffText


		/// <summary>
		/// Find the difference in 2 text documents, comparing by textlines.
		/// The algorithm itself is comparing 2 arrays of numbers so when comparing 2 text documents
		/// each line is converted into a (hash) number. This hash-value is computed by storing all
		/// textlines into a common hashtable so i can find dublicates in there, and generating a 
		/// new number each time a new textline is inserted.
		/// </summary>
		/// <param name="TextA">A-version of the text (usualy the old one)</param>
		/// <param name="TextB">B-version of the text (usualy the new one)</param>
		/// <param name="trimSpace">When set to true, all leading and trailing whitespace characters are stripped out before the comparation is done.</param>
		/// <param name="ignoreSpace">When set to true, all whitespace characters are converted to a single space character before the comparation is done.</param>
		/// <param name="ignoreCase">When set to true, all characters are converted to their lowercase equivivalence before the comparation is done.</param>
		/// <returns>Returns a array of Items that describe the differences.</returns>
		public static Item[] DiffText(string TextA, string TextB, bool trimSpace, bool ignoreSpace, bool ignoreCase) {
			// prepare the input-text and convert to comparable numbers.
			Hashtable h = new Hashtable(TextA.Length + TextB.Length);

			// The A-Version of the data (original data) to be compared.
			DiffData DataA = new DiffData(DiffCodes(TextA, h, trimSpace, ignoreSpace, ignoreCase));

			// The B-Version of the data (modified data) to be compared.
			DiffData DataB = new DiffData(DiffCodes(TextB, h, trimSpace, ignoreSpace, ignoreCase));

			h = null; // free up hashtable memory (maybe)

			LCS(DataA, 0, DataA.Length, DataB, 0, DataB.Length);
			return CreateDiffs(DataA, DataB);
		} // DiffText


		/// <summary>
		/// Find the difference in 2 arrays of integers.
		/// </summary>
		/// <param name="ArrayA">A-version of the numbers (usualy the old one)</param>
		/// <param name="ArrayB">B-version of the numbers (usualy the new one)</param>
		/// <returns>Returns a array of Items that describe the differences.</returns>
		public static Item[] DiffInt(int[] ArrayA, int[] ArrayB) {
			// The A-Version of the data (original data) to be compared.
			DiffData DataA = new DiffData(ArrayA);

			// The B-Version of the data (modified data) to be compared.
			DiffData DataB = new DiffData(ArrayB);

			LCS(DataA, 0, DataA.Length, DataB, 0, DataB.Length);
			return CreateDiffs(DataA, DataB);
		} // Diff


		/// <summary>
		/// Converts all textlines of the text into unique numbers for every unique textline
		/// so further work can work only with simple numbers.
		/// </summary>
		/// <param name="aText">The input text</param>
		/// <param name="h">This extern initialized hashtable is used for storing all ever used textlines.</param>
		/// <param name="trimSpace">Ignore leading and trailing space characters</param>
		/// <param name="ignoreSpace">Ignore spaces.</param>
		/// <param name="ignoreCase">Ignore case.</param>
		/// <returns>An array of integers.</returns>
		private static int[] DiffCodes(string aText, Hashtable h, bool trimSpace, bool ignoreSpace, bool ignoreCase) {
			// get all codes of the text
			string[] Lines;
			int[] Codes;
			int lastUsedCode = h.Count;
			object aCode;
			string s;

			// strip off all cr, only use lf as textline separator.
			aText = aText.Replace("\r", "");
			Lines = aText.Split('\n');

			Codes = new int[Lines.Length];

			for(int i = 0; i < Lines.Length; ++i) {
				s = Lines[i];
				if(trimSpace)
					s = s.Trim();

				if(ignoreSpace) {
					s = Regex.Replace(s, "\\s+", " ");
				}

				if(ignoreCase)
					s = s.ToLowerInvariant();

				aCode = h[s];
				if(aCode == null) {
					lastUsedCode++;
					h[s] = lastUsedCode;
					Codes[i] = lastUsedCode;
				}
				else {
					Codes[i] = (int)aCode;
				} // if
			} // for
			return (Codes);
		} // DiffCodes


		/// <summary>
		/// This is the algorithm to find the Shortest Middle Snake (SMS).
		/// </summary>
		/// <param name="DataA">sequence A</param>
		/// <param name="LowerA">lower bound of the actual range in DataA</param>
		/// <param name="UpperA">upper bound of the actual range in DataA (exclusive)</param>
		/// <param name="DataB">sequence B</param>
		/// <param name="LowerB">lower bound of the actual range in DataB</param>
		/// <param name="UpperB">upper bound of the actual range in DataB (exclusive)</param>
		/// <returns>a MiddleSnakeData record containing x,y and u,v</returns>
		private static SMSRD SMS(DiffData DataA, int LowerA, int UpperA, DiffData DataB, int LowerB, int UpperB) {
			SMSRD ret;
			int MAX = DataA.Length + DataB.Length + 1;

			int DownK = LowerA - LowerB; // the k-line to start the forward search
			int UpK = UpperA - UpperB; // the k-line to start the reverse search

			int Delta = (UpperA - LowerA) - (UpperB - LowerB);
			bool oddDelta = (Delta & 1) != 0;

			// vector for the (0,0) to (x,y) search
			int[] DownVector = new int[2 * MAX + 2];

			// vector for the (u,v) to (N,M) search
			int[] UpVector = new int[2 * MAX + 2];

			// The vectors in the publication accepts negative indexes. the vectors implemented here are 0-based
			// and are access using a specific offset: UpOffset UpVector and DownOffset for DownVektor
			int DownOffset = MAX - DownK;
			int UpOffset = MAX - UpK;

			int MaxD = ((UpperA - LowerA + UpperB - LowerB) / 2) + 1;

			// Debug.Write(2, "SMS", String.Format("Search the box: A[{0}-{1}] to B[{2}-{3}]", LowerA, UpperA, LowerB, UpperB));

			// init vectors
			DownVector[DownOffset + DownK + 1] = LowerA;
			UpVector[UpOffset + UpK - 1] = UpperA;

			for(int D = 0; D <= MaxD; D++) {

				// Extend the forward path.
				for(int k = DownK - D; k <= DownK + D; k += 2) {
					// Debug.Write(0, "SMS", "extend forward path " + k.ToString());

					// find the only or better starting point
					int x, y;
					if(k == DownK - D) {
						x = DownVector[DownOffset + k + 1]; // down
					}
					else {
						x = DownVector[DownOffset + k - 1] + 1; // a step to the right
						if((k < DownK + D) && (DownVector[DownOffset + k + 1] >= x))
							x = DownVector[DownOffset + k + 1]; // down
					}
					y = x - k;

					// find the end of the furthest reaching forward D-path in diagonal k.
					while((x < UpperA) && (y < UpperB) && (DataA.data[x] == DataB.data[y])) {
						x++; y++;
					}
					DownVector[DownOffset + k] = x;

					// overlap ?
					if(oddDelta && (UpK - D < k) && (k < UpK + D)) {
						if(UpVector[UpOffset + k] <= DownVector[DownOffset + k]) {
							ret.x = DownVector[DownOffset + k];
							ret.y = DownVector[DownOffset + k] - k;
							// ret.u = UpVector[UpOffset + k];      // 2002.09.20: no need for 2 points 
							// ret.v = UpVector[UpOffset + k] - k;
							return (ret);
						} // if
					} // if

				} // for k

				// Extend the reverse path.
				for(int k = UpK - D; k <= UpK + D; k += 2) {
					// Debug.Write(0, "SMS", "extend reverse path " + k.ToString());

					// find the only or better starting point
					int x, y;
					if(k == UpK + D) {
						x = UpVector[UpOffset + k - 1]; // up
					}
					else {
						x = UpVector[UpOffset + k + 1] - 1; // left
						if((k > UpK - D) && (UpVector[UpOffset + k - 1] < x))
							x = UpVector[UpOffset + k - 1]; // up
					} // if
					y = x - k;

					while((x > LowerA) && (y > LowerB) && (DataA.data[x - 1] == DataB.data[y - 1])) {
						x--; y--; // diagonal
					}
					UpVector[UpOffset + k] = x;

					// overlap ?
					if(!oddDelta && (DownK - D <= k) && (k <= DownK + D)) {
						if(UpVector[UpOffset + k] <= DownVector[DownOffset + k]) {
							ret.x = DownVector[DownOffset + k];
							ret.y = DownVector[DownOffset + k] - k;
							// ret.u = UpVector[UpOffset + k];     // 2002.09.20: no need for 2 points 
							// ret.v = UpVector[UpOffset + k] - k;
							return (ret);
						} // if
					} // if

				} // for k

			} // for D

			throw new ApplicationException("the algorithm should never come here.");
		} // SMS


		/// <summary>
		/// This is the divide-and-conquer implementation of the longes common-subsequence (LCS) 
		/// algorithm.
		/// The published algorithm passes recursively parts of the A and B sequences.
		/// To avoid copying these arrays the lower and upper bounds are passed while the sequences stay constant.
		/// </summary>
		/// <param name="DataA">sequence A</param>
		/// <param name="LowerA">lower bound of the actual range in DataA</param>
		/// <param name="UpperA">upper bound of the actual range in DataA (exclusive)</param>
		/// <param name="DataB">sequence B</param>
		/// <param name="LowerB">lower bound of the actual range in DataB</param>
		/// <param name="UpperB">upper bound of the actual range in DataB (exclusive)</param>
		private static void LCS(DiffData DataA, int LowerA, int UpperA, DiffData DataB, int LowerB, int UpperB) {
			// Debug.Write(2, "LCS", String.Format("Analyse the box: A[{0}-{1}] to B[{2}-{3}]", LowerA, UpperA, LowerB, UpperB));

			// Fast walkthrough equal lines at the start
			while(LowerA < UpperA && LowerB < UpperB && DataA.data[LowerA] == DataB.data[LowerB]) {
				LowerA++; LowerB++;
			}

			// Fast walkthrough equal lines at the end
			while(LowerA < UpperA && LowerB < UpperB && DataA.data[UpperA - 1] == DataB.data[UpperB - 1]) {
				--UpperA; --UpperB;
			}

			if(LowerA == UpperA) {
				// mark as inserted lines.
				while(LowerB < UpperB)
					DataB.modified[LowerB++] = true;

			}
			else if(LowerB == UpperB) {
				// mark as deleted lines.
				while(LowerA < UpperA)
					DataA.modified[LowerA++] = true;

			}
			else {
				// Find the middle snakea and length of an optimal path for A and B
				SMSRD smsrd = SMS(DataA, LowerA, UpperA, DataB, LowerB, UpperB);
				// Debug.Write(2, "MiddleSnakeData", String.Format("{0},{1}", smsrd.x, smsrd.y));

				// The path is from LowerX to (x,y) and (x,y) ot UpperX
				LCS(DataA, LowerA, smsrd.x, DataB, LowerB, smsrd.y);
				LCS(DataA, smsrd.x, UpperA, DataB, smsrd.y, UpperB);  // 2002.09.20: no need for 2 points 
			}
		} // LCS()


		/// <summary>Scan the tables of which lines are inserted and deleted,
		/// producing an edit script in forward order.  
		/// </summary>
		/// dynamic array
		private static Item[] CreateDiffs(DiffData DataA, DiffData DataB) {
			ArrayList a = new ArrayList();
			Item aItem;
			Item[] result;

			int StartA, StartB;
			int LineA, LineB;

			LineA = 0;
			LineB = 0;
			while(LineA < DataA.Length || LineB < DataB.Length) {
				if((LineA < DataA.Length) && (!DataA.modified[LineA])
				  && (LineB < DataB.Length) && (!DataB.modified[LineB])) {
					// equal lines
					LineA++;
					LineB++;

				}
				else {
					// maybe deleted and/or inserted lines
					StartA = LineA;
					StartB = LineB;

					while(LineA < DataA.Length && (LineB >= DataB.Length || DataA.modified[LineA]))
						// while (LineA < DataA.Length && DataA.modified[LineA])
						LineA++;

					while(LineB < DataB.Length && (LineA >= DataA.Length || DataB.modified[LineB]))
						// while (LineB < DataB.Length && DataB.modified[LineB])
						LineB++;

					if((StartA < LineA) || (StartB < LineB)) {
						// store a new difference-item
						aItem = new Item();
						aItem.StartA = StartA;
						aItem.StartB = StartB;
						aItem.deletedA = LineA - StartA;
						aItem.insertedB = LineB - StartB;
						a.Add(aItem);
					} // if
				} // if
			} // while

			result = new Item[a.Count];
			a.CopyTo(result);

			return (result);
		}

	} // class Diff

	/// <summary>Data on one input file being compared.  
	/// </summary>
	internal class DiffData {

		/// <summary>Number of elements (lines).</summary>
		internal int Length;

		/// <summary>Buffer of numbers that will be compared.</summary>
		internal int[] data;

		/// <summary>
		/// Array of booleans that flag for modified data.
		/// This is the result of the diff.
		/// This means deletedA in the first Data or inserted in the second Data.
		/// </summary>
		internal bool[] modified;

		/// <summary>
		/// Initialize the Diff-Data buffer.
		/// </summary>
		/// <param name="initData">Reference to the buffer</param>
		internal DiffData(int[] initData) {
			data = initData;
			Length = initData.Length;
			modified = new bool[Length + 2];
		} // DiffData

	} // class DiffData

}
