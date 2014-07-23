
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements methods for sorting pages.
	/// </summary>
	public static class PageSortingTools {

		/// <summary>
		/// Sorts pages.
		/// </summary>
		/// <param name="pages">The pages list to sort.</param>
		/// <param name="sortBy">The sorting method.</param>
		/// <param name="reverse"><c>true</c> to sort in reverse order.</param>
		/// <returns>The sorted list, divided in relevant groups.</returns>
		public static SortedDictionary<SortingGroup, List<ExtendedPageInfo>> Sort(ExtendedPageInfo[] pages, SortingMethod sortBy, bool reverse) {
			switch(sortBy) {
				case SortingMethod.Title:
					return SortByTitle(pages, reverse);
				case SortingMethod.Creator:
					return SortByCreator(pages, reverse);
				case SortingMethod.User:
					return SortByUser(pages, reverse);
				case SortingMethod.DateTime:
					return SortByDateTime(pages, reverse);
				case SortingMethod.Creation:
					return SortByCreation(pages, reverse);
				default:
					throw new NotSupportedException("Invalid sorting method");
			}
		}

		/// <summary>
		/// Sorts pages by title.
		/// </summary>
		/// <param name="pages">The pages.</param>
		/// <param name="reverse"><c>true</c> to sort in reverse order.</param>
		/// <returns>The sorted list, divided in relevant groups (#, A, B, etc.).</returns>
		private static SortedDictionary<SortingGroup, List<ExtendedPageInfo>> SortByTitle(ExtendedPageInfo[] pages, bool reverse) {
			ExtendedPageInfo[] temp = new ExtendedPageInfo[pages.Length];
			Array.Copy(pages, temp, pages.Length);
			Array.Sort(temp, delegate(ExtendedPageInfo p1, ExtendedPageInfo p2) {
				string t1 = p1.Title, t2 = p2.Title;
				if(!reverse) return string.Compare(t1, t2, false, CultureInfo.CurrentCulture);
				else return string.Compare(t2, t1, false, CultureInfo.CurrentCulture);
			});

			SortedDictionary<char, List<ExtendedPageInfo>> result =
				new SortedDictionary<char, List<ExtendedPageInfo>>(new CharComparer(reverse));

			foreach(ExtendedPageInfo p in temp) {
				char first = GetFirstChar(p.Title);
				if(!char.IsLetter(first)) {
					if(!result.ContainsKey('#')) result.Add('#', new List<ExtendedPageInfo>(20));
					result['#'].Add(p);
				}
				else {
					if(!result.ContainsKey(first)) result.Add(first, new List<ExtendedPageInfo>(20));
					result[first].Add(p);
				}
			}

			SortedDictionary<SortingGroup, List<ExtendedPageInfo>> finalResult =
				new SortedDictionary<SortingGroup, List<ExtendedPageInfo>>(new SortingGroupComparer(reverse));
			foreach(char key in result.Keys) {
				finalResult.Add(new SortingGroup(GetLetterNumber(key), key.ToString(), key), result[key]);
			}
			return finalResult;
		}

		private static char GetFirstChar(string value) {
			if(string.IsNullOrEmpty(value)) return '0';

			// First we normalize the value to separate diacritics
			string normalized = value.ToUpper(CultureInfo.CurrentCulture).Normalize(System.Text.NormalizationForm.FormD);

			int count = normalized.Length;
			for(int i = 0; i < count; i++) {
				char c = normalized[i];
				if(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) {
					// We return the first non-spacing mark
					return c;
				}
			}

			return '0';
		}

		/// <summary>
		/// Sorts pages by last author.
		/// </summary>
		/// <param name="pages">The pages.</param>
		/// <param name="reverse"><c>true</c> to sort in reverse order.</param>
		/// <returns>The sorted list, divided in relevant groups (#, A, B, etc.).</returns>
		private static SortedDictionary<SortingGroup, List<ExtendedPageInfo>> SortByUser(ExtendedPageInfo[] pages, bool reverse) {
			ExtendedPageInfo[] temp = new ExtendedPageInfo[pages.Length];
			Array.Copy(pages, temp, pages.Length);
			Array.Sort(temp, delegate(ExtendedPageInfo p1, ExtendedPageInfo p2) {
				string u1 = p1.LastAuthor, u2 = p2.LastAuthor;
				if(!reverse) return string.Compare(u1, u2, false, CultureInfo.CurrentCulture);
				else return string.Compare(u2, u1, false, CultureInfo.CurrentCulture);
			});

			SortedDictionary<char, List<ExtendedPageInfo>> result =
				new SortedDictionary<char, List<ExtendedPageInfo>>(new CharComparer(reverse));

			foreach(ExtendedPageInfo p in temp) {
				char first = GetFirstChar(p.LastAuthor);
				if(!char.IsLetter(first)) {
					if(!result.ContainsKey('#')) result.Add('#', new List<ExtendedPageInfo>(20));
					result['#'].Add(p);
				}
				else {
					if(!result.ContainsKey(first)) result.Add(first, new List<ExtendedPageInfo>(20));
					result[first].Add(p);
				}
			}

			SortedDictionary<SortingGroup, List<ExtendedPageInfo>> finalResult =
				new SortedDictionary<SortingGroup, List<ExtendedPageInfo>>(new SortingGroupComparer(reverse));
			foreach(char key in result.Keys) {
				finalResult.Add(new SortingGroup(GetLetterNumber(key), key.ToString(), key), result[key]);
			}
			return finalResult;
		}

		/// <summary>
		/// Sorts pages by creator.
		/// </summary>
		/// <param name="pages">The pages.</param>
		/// <param name="reverse"><c>true</c> to sort in reverse order.</param>
		/// <returns>The sorted list, divided in relevant groups (#, A, B, etc.).</returns>
		private static SortedDictionary<SortingGroup, List<ExtendedPageInfo>> SortByCreator(ExtendedPageInfo[] pages, bool reverse) {
			ExtendedPageInfo[] temp = new ExtendedPageInfo[pages.Length];
			Array.Copy(pages, temp, pages.Length);
			Array.Sort(temp, delegate(ExtendedPageInfo p1, ExtendedPageInfo p2) {
				string u1 = p1.Creator, u2 = p2.Creator;
				if(!reverse) return string.Compare(u1, u2, false, CultureInfo.CurrentCulture);
				else return string.Compare(u2, u1, false, CultureInfo.CurrentCulture);
			});

			SortedDictionary<char, List<ExtendedPageInfo>> result =
				new SortedDictionary<char, List<ExtendedPageInfo>>(new CharComparer(reverse));

			foreach(ExtendedPageInfo p in temp) {
				char first = GetFirstChar(p.Creator);
				if(!char.IsLetter(first)) {
					if(!result.ContainsKey('#')) result.Add('#', new List<ExtendedPageInfo>(20));
					result['#'].Add(p);
				}
				else {
					if(!result.ContainsKey(first)) result.Add(first, new List<ExtendedPageInfo>(20));
					result[first].Add(p);
				}
			}

			SortedDictionary<SortingGroup, List<ExtendedPageInfo>> finalResult =
				new SortedDictionary<SortingGroup, List<ExtendedPageInfo>>(new SortingGroupComparer(reverse));
			foreach(char key in result.Keys) {
				finalResult.Add(new SortingGroup(GetLetterNumber(key), key.ToString(), key), result[key]);
			}
			return finalResult;
		}

		/// <summary>
		/// Sorts pages by modification date/time.
		/// </summary>
		/// <param name="pages">The pages.</param>
		/// <param name="reverse"><c>true</c> to sort in reverse order.</param>
		/// <returns>The sorted list, divided in relevant groups.</returns>
		private static SortedDictionary<SortingGroup, List<ExtendedPageInfo>> SortByDateTime(ExtendedPageInfo[] pages, bool reverse) {
			ExtendedPageInfo[] temp = new ExtendedPageInfo[pages.Length];
			Array.Copy(pages, temp, pages.Length);
			Array.Sort(temp, delegate(ExtendedPageInfo p1, ExtendedPageInfo p2) {
				if(!reverse) return p1.ModificationDateTime.CompareTo(p2.ModificationDateTime);
				else return p2.ModificationDateTime.CompareTo(p1.ModificationDateTime);
			});

			SortedDictionary<DateTime, List<ExtendedPageInfo>> result =
				new SortedDictionary<DateTime, List<ExtendedPageInfo>>(new DateTimeComparer(reverse));

			Dictionary<DateTime, string> labels = new Dictionary<DateTime, string>();

			foreach(ExtendedPageInfo p in temp) {
				string label;
				DateTime marker = GetMarkerDate(p.ModificationDateTime, out label);
				if(!result.ContainsKey(marker)) {
					result.Add(marker, new List<ExtendedPageInfo>(20));
					labels.Add(marker, label);
				}
				result[marker].Add(p);
			}

			SortedDictionary<SortingGroup, List<ExtendedPageInfo>> finalResult =
				new SortedDictionary<SortingGroup, List<ExtendedPageInfo>>(new SortingGroupComparer(reverse));
			foreach(DateTime key in result.Keys) {
				finalResult.Add(new SortingGroup(key.DayOfYear + key.Year * 1000, labels[key], key), result[key]);
			}
			return finalResult;
		}

		/// <summary>
		/// Sorts pages by creation date/time.
		/// </summary>
		/// <param name="pages">The pages.</param>
		/// <param name="reverse"><c>true</c> to sort in reverse order.</param>
		/// <returns>The sorted list, divided in relevant groups.</returns>
		private static SortedDictionary<SortingGroup, List<ExtendedPageInfo>> SortByCreation(ExtendedPageInfo[] pages, bool reverse) {
			ExtendedPageInfo[] temp = new ExtendedPageInfo[pages.Length];
			Array.Copy(pages, temp, pages.Length);
			Array.Sort(temp, delegate(ExtendedPageInfo p1, ExtendedPageInfo p2) {
				if(!reverse) return p1.CreationDateTime.CompareTo(p2.CreationDateTime);
				else return p2.CreationDateTime.CompareTo(p1.CreationDateTime);
			});

			SortedDictionary<DateTime, List<ExtendedPageInfo>> result =
				new SortedDictionary<DateTime, List<ExtendedPageInfo>>(new DateTimeComparer(reverse));

			Dictionary<DateTime, string> labels = new Dictionary<DateTime, string>();

			foreach(ExtendedPageInfo p in temp) {
				string label;
				DateTime marker = GetMarkerDate(p.CreationDateTime, out label);
				if(!result.ContainsKey(marker)) {
					result.Add(marker, new List<ExtendedPageInfo>(20));
					labels.Add(marker, label);
				}
				result[marker].Add(p);
			}

			SortedDictionary<SortingGroup, List<ExtendedPageInfo>> finalResult =
				new SortedDictionary<SortingGroup, List<ExtendedPageInfo>>(new SortingGroupComparer(reverse));
			foreach(DateTime key in result.Keys) {
				finalResult.Add(new SortingGroup(key.DayOfYear + key.Year * 1000, labels[key], key), result[key]);
			}
			return finalResult;
		}

		private static int GetLetterNumber(char c) {
			// Only # and letters allowed
			c = char.ToUpperInvariant(c);
			if(c == '#') return 0;
			else return c - 64;
		}

		private static DateTime GetMarkerDate(DateTime dt, out string label) {
			DateTime now = DateTime.Now;

			// Today
			if(dt.Date == now.Date) {
				label = Properties.Messages.Today;
				return new DateTime(now.Year, now.Month, now.Day);
			}

			// Yesterday
			DateTime yesterday = now.AddDays(-1);
			if(dt.Date == yesterday.Date) {
				label = Properties.Messages.Yesterday;
				return new DateTime(yesterday.Year, yesterday.Month, yesterday.Day);
			}

			// Earlier this week
			DateTime thisWeek = now;
			while(thisWeek.DayOfWeek != DayOfWeek.Monday) thisWeek = thisWeek.AddDays(-1);
			if(dt.Year == thisWeek.Year && dt.Month == thisWeek.Month && dt.Day >= thisWeek.Day) {
				label = Properties.Messages.EarlierThisWeek;
				return new DateTime(thisWeek.Year, thisWeek.Month, thisWeek.Day);
			}

			// Earlier this month
			DateTime thisMonth = now;
			while(thisMonth.Day != 1) thisMonth = thisMonth.AddDays(-1);
			if(dt.Year == thisMonth.Year && dt.Month == thisMonth.Month) {
				label = Properties.Messages.EarlierThisMonth + " (" + thisMonth.ToString("MMMM") + ")";
				return new DateTime(thisMonth.Year, thisMonth.Month, thisMonth.Day);
			}

			// Last month
			DateTime lastMonth = now.AddMonths(-1);
			while(lastMonth.Day != 1) lastMonth = lastMonth.AddDays(-1);
			if(dt.Year == lastMonth.Year && dt.Month == lastMonth.Month) {
				label = Properties.Messages.LastMonth + " (" + lastMonth.ToString("MMMM") + ")";
				return new DateTime(lastMonth.Year, lastMonth.Month, lastMonth.Day);
			}

			label = Properties.Messages.Older;
			return DateTime.MinValue;
		}

	}

	/// <summary>
	/// Lists legal page sorting methods.
	/// </summary>
	public enum SortingMethod {
		/// <summary>
		/// Sort by title.
		/// </summary>
		Title,
		/// <summary>
		/// Sort by creator.
		/// </summary>
		Creator,
		/// <summary>
		/// Sort by last author.
		/// </summary>
		User,
		/// <summary>
		/// Sort by creation date/time.
		/// </summary>
		Creation,
		/// <summary>
		/// Sort by modification date/time.
		/// </summary>
		DateTime
	}

	/// <summary>
	/// Describes a sorting group.
	/// </summary>
	public class SortingGroup {

		private int number;
		private string label;
		private object tag;

		/// <summary>
		/// Initializes a new instance of the <b>SortingGroup</b> class.
		/// </summary>
		/// <param name="number">The group number.</param>
		/// <param name="label">The group label.</param>
		/// <param name="tag">The group tag.</param>
		public SortingGroup(int number, string label, object tag) {
			this.number = number;
			this.label = label;
			this.tag = tag;
		}

		/// <summary>
		/// Gets the group number.
		/// </summary>
		public int Number {
			get { return number; }
		}

		/// <summary>
		/// Gets the group label.
		/// </summary>
		public string Label {
			get { return label; }
		}

		/// <summary>
		/// Gets the group tag.
		/// </summary>
		public object Tag {
			get { return tag; }
		}

	}

	/// <summary>
	/// Implements a Sorting Group comparer.
	/// </summary>
	public class SortingGroupComparer : IComparer<SortingGroup> {

		private bool reverse;

		/// <summary>
		/// Initializes a new instance of the <b>SortingGroupComparer</b> class.
		/// </summary>
		/// <param name="reverse"><c>true</c> to perform the comparison in reverse order.</param>
		public SortingGroupComparer(bool reverse) {
			this.reverse = reverse;
		}

		/// <summary>
		/// Compares two Sorting Groups.
		/// </summary>
		/// <param name="x">The first Sorting Group.</param>
		/// <param name="y">The second Sorting Group.</param>
		/// <returns>The comparison result.</returns>
		public int Compare(SortingGroup x, SortingGroup y) {
			if(!reverse) return x.Number.CompareTo(y.Number);
			else return y.Number.CompareTo(x.Number);
		}

	}

	/// <summary>
	/// Implements a char comparer.
	/// </summary>
	public class CharComparer : IComparer<char> {

		private bool reverse;

		/// <summary>
		/// Initializes a new instance of the <b>CharComparer</b> class.
		/// </summary>
		/// <param name="reverse"><c>true</c> to perform a reverse comparison.</param>
		public CharComparer(bool reverse) {
			this.reverse = reverse;
		}

		/// <summary>
		/// Compares two chars.
		/// </summary>
		/// <param name="x">The first char.</param>
		/// <param name="y">The second char.</param>
		/// <returns>The comparison result.</returns>
		public int Compare(char x, char y) {
			if(!reverse) return x.CompareTo(y);
			else return y.CompareTo(x);
		}

	}

	/// <summary>
	/// Implements a date/time comparer.
	/// </summary>
	public class DateTimeComparer : IComparer<DateTime> {

		private bool reverse;

		/// <summary>
		/// Initializes a new instance of the <b>DateTimeComparer</b> class.
		/// </summary>
		/// <param name="reverse"><c>true</c> to perform reverse comparison.</param>
		public DateTimeComparer(bool reverse) {
			this.reverse = reverse;
		}

		/// <summary>
		/// Compares two date/times.
		/// </summary>
		/// <param name="x">The first date/time.</param>
		/// <param name="y">The second date/time.</param>
		/// <returns>The comparison result.</returns>
		public int Compare(DateTime x, DateTime y) {
			if(!reverse) return x.CompareTo(y);
			else return y.CompareTo(x);
		}

	}

}
