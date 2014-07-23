
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// It is the interface that must be implemented in order to create a custom Users Storage Provider for ScrewTurn Wiki.
	/// </summary>
	/// <remarks>A class that implements this class <b>should not</b> have any kind of data caching.</remarks>
	public interface IUsersStorageProviderV30 : IProviderV30 {

		/// <summary>
		/// Tests a Password for a User account.
		/// </summary>
		/// <param name="user">The User account.</param>
		/// <param name="password">The Password to test.</param>
		/// <returns>True if the Password is correct.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="password"/> are <c>null</c>.</exception>
		bool TestAccount(UserInfo user, string password);

		/// <summary>
		/// Gets the complete list of Users.
		/// </summary>
		/// <returns>All the Users, sorted by username.</returns>
		UserInfo[] GetUsers();

		/// <summary>
		/// Adds a new User.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="displayName">The display name (can be <c>null</c>).</param>
		/// <param name="password">The Password.</param>
		/// <param name="email">The Email address.</param>
		/// <param name="active">A value indicating whether the account is active.</param>
		/// <param name="dateTime">The Account creation Date/Time.</param>
		/// <returns>The correct <see cref="T:UserInfo" /> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/>, <paramref name="password"/> or <paramref name="email"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/>, <paramref name="password"/> or <paramref name="email"/> are empty.</exception>
		UserInfo AddUser(string username, string displayName, string password, string email, bool active, DateTime dateTime);

		/// <summary>
		/// Modifies a User.
		/// </summary>
		/// <param name="user">The Username of the user to modify.</param>
		/// <param name="newDisplayName">The new display name (can be <c>null</c>).</param>
		/// <param name="newPassword">The new Password (<c>null</c> or blank to keep the current password).</param>
		/// <param name="newEmail">The new Email address.</param>
		/// <param name="newActive">A value indicating whether the account is active.</param>
		/// <returns>The correct <see cref="T:UserInfo" /> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="newEmail"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newEmail"/> is empty.</exception>
		UserInfo ModifyUser(UserInfo user, string newDisplayName, string newPassword, string newEmail, bool newActive);

		/// <summary>
		/// Removes a User.
		/// </summary>
		/// <param name="user">The User to remove.</param>
		/// <returns>True if the User has been removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		bool RemoveUser(UserInfo user);

		/// <summary>
		/// Gets all the user groups.
		/// </summary>
		/// <returns>All the groups, sorted by name.</returns>
		UserGroup[] GetUserGroups();

		/// <summary>
		/// Adds a new user group.
		/// </summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="description">The description of the group.</param>
		/// <returns>The correct <see cref="T:UserGroup" /> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="description"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		UserGroup AddUserGroup(string name, string description);

		/// <summary>
		/// Modifies a user group.
		/// </summary>
		/// <param name="group">The group to modify.</param>
		/// <param name="description">The new description of the group.</param>
		/// <returns>The correct <see cref="T:UserGroup" /> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="group"/> or <paramref name="description"/> are <c>null</c>.</exception>
		UserGroup ModifyUserGroup(UserGroup group, string description);

		/// <summary>
		/// Removes a user group.
		/// </summary>
		/// <param name="group">The group to remove.</param>
		/// <returns><c>true</c> if the group is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="group"/> is <c>null</c>.</exception>
		bool RemoveUserGroup(UserGroup group);

		/// <summary>
		/// Sets the group memberships of a user account.
		/// </summary>
		/// <param name="user">The user account.</param>
		/// <param name="groups">The groups the user account is member of.</param>
		/// <returns>The correct <see cref="T:UserGroup" /> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="groups"/> are <c>null</c>.</exception>
		UserInfo SetUserMembership(UserInfo user, string[] groups);

		/// <summary>
		/// Tries to login a user directly through the provider.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> or <paramref name="password"/> are <c>null</c>.</exception>
		UserInfo TryManualLogin(string username, string password);

		/// <summary>
		/// Tries to login a user directly through the provider using 
		/// the current HttpContext and without username/password.
		/// </summary>
		/// <param name="context">The current HttpContext.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="context"/> is <c>null</c>.</exception>
		UserInfo TryAutoLogin(HttpContext context);

		/// <summary>
		/// Gets a user account.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The <see cref="T:UserInfo" />, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> is empty.</exception>
		UserInfo GetUser(string username);

		/// <summary>
		/// Gets a user account.
		/// </summary>
		/// <param name="email">The email address.</param>
		/// <returns>The first user found with the specified email address, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="email"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="email"/> is empty.</exception>
		UserInfo GetUserByEmail(string email);

		/// <summary>
		/// Notifies the provider that a user has logged in through the authentication cookie.
		/// </summary>
		/// <param name="user">The user who has logged in.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		void NotifyCookieLogin(UserInfo user);

		/// <summary>
		/// Notifies the provider that a user has logged out.
		/// </summary>
		/// <param name="user">The user who has logged out.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		void NotifyLogout(UserInfo user);

		/// <summary>
		/// Stores a user data element, overwriting the previous one if present.
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element (case insensitive).</param>
		/// <param name="value">The value of the data element, <c>null</c> for deleting the data.</param>
		/// <returns><c>true</c> if the data element is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="key"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		bool StoreUserData(UserInfo user, string key, string value);

		/// <summary>
		/// Gets a user data element, if any.
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element.</param>
		/// <returns>The value of the data element, or <c>null</c> if the element is not found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="key"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		string RetrieveUserData(UserInfo user, string key);

		/// <summary>
		/// Retrieves all the user data elements for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The user data elements (key->value).</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		IDictionary<string, string> RetrieveAllUserData(UserInfo user);

		/// <summary>
		/// Gets all the users that have the specified element in their data.
		/// </summary>
		/// <param name="key">The key of the data.</param>
		/// <returns>The users and the data.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		IDictionary<UserInfo, string> GetUsersWithData(string key);

		/// <summary>
		/// Gets a value indicating whether user accounts are read-only.
		/// </summary>
		bool UserAccountsReadOnly { get; }

		/// <summary>
		/// Gets a value indicating whether user groups are read-only. If so, the provider 
		/// should support default user groups as defined in the wiki configuration.
		/// </summary>
		bool UserGroupsReadOnly { get; }

		/// <summary>
		/// Gets a value indicating whether group membership is read-only (if <see cref="UserAccountsReadOnly" /> 
		/// is <c>false</c>, then this property must be <c>false</c>). If this property is <c>true</c>, the provider 
		/// should return membership data compatible with default user groups.
		/// </summary>
		bool GroupMembershipReadOnly { get; }

		/// <summary>
		/// Gets a value indicating whether users' data is read-only.
		/// </summary>
		bool UsersDataReadOnly { get; }

	}

}
