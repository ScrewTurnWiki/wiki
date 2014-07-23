
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Describes a User.
	/// </summary>
	public class UserInfo {

		/// <summary>
		/// The Username of the User.
		/// </summary>
		protected string username;
		/// <summary>
		/// The display name of the User.
		/// </summary>
		protected string displayName;
		/// <summary>
		/// The Email address of the User.
		/// </summary>
		protected string email;
		/// <summary>
		/// A value indicating whether the user account is active.
		/// </summary>
		protected bool active;
		/// <summary>
		/// The account creation date/time.
		/// </summary>
		protected DateTime dateTime;
		/// <summary>
		/// The names of the groups the user is member of.
		/// </summary>
		protected string[] groups;
		/// <summary>
		/// The Provider that handles the User.
		/// </summary>
		protected IUsersStorageProviderV30 provider;

		/// <summary>
		/// Initializes a new instance of the <b>UserInfo</b> class.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="displayName">The display name.</param>
		/// <param name="email">The Email.</param>
		/// <param name="active">Specifies whether the Account is active or not.</param>
		/// <param name="dateTime">The creation DateTime.</param>
		/// <param name="provider">The Users Storage Provider that manages the User.</param>
		public UserInfo(string username, string displayName, string email, bool active, DateTime dateTime, IUsersStorageProviderV30 provider) {
			this.username = username;
			this.displayName = displayName;
			this.email = email;
			this.active = active;
			this.dateTime = dateTime;
			this.provider = provider;
		}

		/// <summary>
		/// Gets the Username.
		/// </summary>
		public string Username {
			get { return username; }
		}

		/// <summary>
		/// Gets or sets the display name.
		/// </summary>
		public string DisplayName {
			get { return displayName; }
			set { displayName = value; }
		}

		/// <summary>
		/// Gets or sets the Email.
		/// </summary>
		public string Email {
			get { return email; }
			set { email = value; }
		}

		/// <summary>
		/// Gets or sets a value specifying whether the Account is active or not.
		/// </summary>
		public bool Active {
			get { return active; }
			set { active = value; }
		}

		/// <summary>
		/// Gets the creation DateTime.
		/// </summary>
		public DateTime DateTime {
			get { return dateTime; }
			set { dateTime = value; }
		}

		/// <summary>
		/// Gets or sets the names of the groups the user is member of.
		/// </summary>
		public string[] Groups {
			get { return groups; }
			set { groups = value; }
		}

		/// <summary>
		/// Gets or sets the Users Storage Provider.
		/// </summary>
		public IUsersStorageProviderV30 Provider {
			get { return provider; }
			set { provider = value; }
		}

		/// <summary>
		/// Converts the current instance to a string.
		/// </summary>
		/// <returns>The string.</returns>
		public override string ToString() {
			return username;
		}

	}

	/// <summary>
	/// Provides a method for comparing two <b>UserInfo</b> objects, comparing their Username.
	/// </summary>
	/// <remarks>The comparison is <b>case unsensitive</b>.</remarks>
	public class UsernameComparer : IComparer<UserInfo> {

		/// <summary>
		/// Compares two UserInfo objects, comparing their Username.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The comparison result (-1, 0 or 1).</returns>
		public int Compare(UserInfo x, UserInfo y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Username, y.Username);
		}

	}

}
