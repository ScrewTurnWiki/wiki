
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Stores a Local UserInfo object.
	/// </summary>
	public class LocalUserInfo : UserInfo {

		private string passwordHash;

		/// <summary>
		/// Initializes a new instance of the <b>LocalUserInfo</b> class.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="displayName">The display name.</param>
		/// <param name="email">The Email.</param>
		/// <param name="active">Specifies whether the Account is active or not.</param>
		/// <param name="dateTime">The creation DateTime.</param>
		/// <param name="provider">The Users Storage Provider that manages the User.</param>
		/// <param name="passwordHash">The Password Hash.</param>
		public LocalUserInfo(string username, string displayName, string email, bool active, DateTime dateTime,
			IUsersStorageProviderV30 provider, string passwordHash)
			: base(username, displayName, email, active, dateTime, provider) {
			this.passwordHash = passwordHash;
		}

		/// <summary>
		/// Gets or sets the Password Hash.
		/// </summary>
		public string PasswordHash {
			get { return passwordHash; }
			set { passwordHash = value; }
		}

	}

}
