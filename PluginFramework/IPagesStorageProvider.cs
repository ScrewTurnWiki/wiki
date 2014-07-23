
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// It is the interface that must be implemented in order to create a custom Pages Storage Provider for ScrewTurn Wiki.
	/// </summary>
	/// <remarks>A class that implements this interface <b>should not</b> have any kind of data caching.</remarks>
	public interface IPagesStorageProviderV30 : IStorageProviderV30 {

		/// <summary>
		/// Gets a namespace.
		/// </summary>
		/// <param name="name">The name of the namespace.</param>
		/// <returns>The <see cref="T:NamespaceInfo" />, or <c>null</c> if no namespace is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		NamespaceInfo GetNamespace(string name);

		/// <summary>
		/// Gets all the sub-namespaces.
		/// </summary>
		/// <returns>The sub-namespaces, sorted by name.</returns>
		NamespaceInfo[] GetNamespaces();

		/// <summary>
		/// Adds a new namespace.
		/// </summary>
		/// <param name="name">The name of the namespace.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		NamespaceInfo AddNamespace(string name);

		/// <summary>
		/// Renames a namespace.
		/// </summary>
		/// <param name="nspace">The namespace to rename.</param>
		/// <param name="newName">The new name of the namespace.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="nspace"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		NamespaceInfo RenameNamespace(NamespaceInfo nspace, string newName);

		/// <summary>
		/// Sets the default page of a namespace.
		/// </summary>
		/// <param name="nspace">The namespace of which to set the default page.</param>
		/// <param name="page">The page to use as default page, or <c>null</c>.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="nspace"/> is <c>null</c>.</exception>
		NamespaceInfo SetNamespaceDefaultPage(NamespaceInfo nspace, PageInfo page);

		/// <summary>
		/// Removes a namespace.
		/// </summary>
		/// <param name="nspace">The namespace to remove.</param>
		/// <returns><c>true</c> if the namespace is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="nspace"/> is <c>null</c>.</exception>
		bool RemoveNamespace(NamespaceInfo nspace);

		/// <summary>
		/// Moves a page from its namespace into another.
		/// </summary>
		/// <param name="page">The page to move.</param>
		/// <param name="destination">The destination namespace (<c>null</c> for the root).</param>
		/// <param name="copyCategories">A value indicating whether to copy the page categories in the destination 
		/// namespace, if not already available.</param>
		/// <returns>The correct instance of <see cref="T:PageInfo" />.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		PageInfo MovePage(PageInfo page, NamespaceInfo destination, bool copyCategories);

		/// <summary>
		/// Gets a category.
		/// </summary>
		/// <param name="fullName">The full name of the category.</param>
		/// <returns>The <see cref="T:CategoryInfo" />, or <c>null</c> if no category is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName" /> is empty.</exception>
		CategoryInfo GetCategory(string fullName);

		/// <summary>
		/// Gets all the Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace.</param>
		/// <returns>All the Categories in the namespace, sorted by name.</returns>
		CategoryInfo[] GetCategories(NamespaceInfo nspace);

		/// <summary>
		/// Gets all the categories of a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The categories, sorted by name.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		CategoryInfo[] GetCategoriesForPage(PageInfo page);

		/// <summary>
		/// Adds a Category.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Category name.</param>
		/// <returns>The correct CategoryInfo object.</returns>
		/// <remarks>The method should set category's Pages to an empty array.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		CategoryInfo AddCategory(string nspace, string name);

		/// <summary>
		/// Renames a Category.
		/// </summary>
		/// <param name="category">The Category to rename.</param>
		/// <param name="newName">The new Name.</param>
		/// <returns>The correct CategoryInfo object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="category"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		CategoryInfo RenameCategory(CategoryInfo category, string newName);

		/// <summary>
		/// Removes a Category.
		/// </summary>
		/// <param name="category">The Category to remove.</param>
		/// <returns>True if the Category has been removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="category"/> is <c>null</c>.</exception>
		bool RemoveCategory(CategoryInfo category);

		/// <summary>
		/// Merges two Categories.
		/// </summary>
		/// <param name="source">The source Category.</param>
		/// <param name="destination">The destination Category.</param>
		/// <returns>The correct <see cref="T:CategoryInfo" /> object.</returns>
		/// <remarks>The destination Category remains, while the source Category is deleted, and all its Pages re-bound 
		/// in the destination Category.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> are <c>null</c>.</exception>
		CategoryInfo MergeCategories(CategoryInfo source, CategoryInfo destination);

		/// <summary>
		/// Performs a search in the index.
		/// </summary>
		/// <param name="parameters">The search parameters.</param>
		/// <returns>The results.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="parameters"/> is <c>null</c>.</exception>
		SearchResultCollection PerformSearch(SearchParameters parameters);

		/// <summary>
		/// Rebuilds the search index.
		/// </summary>
		void RebuildIndex();

		/// <summary>
		/// Gets some statistics about the search engine index.
		/// </summary>
		/// <param name="documentCount">The total number of documents.</param>
		/// <param name="wordCount">The total number of unique words.</param>
		/// <param name="occurrenceCount">The total number of word-document occurrences.</param>
		/// <param name="size">The approximated size, in bytes, of the search engine index.</param>
		void GetIndexStats(out int documentCount, out int wordCount, out int occurrenceCount, out long size);

		/// <summary>
		/// Gets a value indicating whether the search engine index is corrupted and needs to be rebuilt.
		/// </summary>
		bool IsIndexCorrupted { get; }

		/// <summary>
		/// Gets a page.
		/// </summary>
		/// <param name="fullName">The full name of the page.</param>
		/// <returns>The <see cref="T:PageInfo" />, or <c>null</c> if no page is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		PageInfo GetPage(string fullName);

		/// <summary>
		/// Gets all the Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>All the Pages in the namespace, sorted by name.</returns>
		PageInfo[] GetPages(NamespaceInfo nspace);

		/// <summary>
		/// Gets all the pages in a namespace that are bound to zero categories.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages, sorted by name.</returns>
		PageInfo[] GetUncategorizedPages(NamespaceInfo nspace);

		/// <summary>
		/// Gets the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Page Content object, <c>null</c> if the page does not exist or <paramref name="page"/> is <c>null</c>,
		/// or an empty instance if the content could not be retrieved (<seealso cref="PageContent.GetEmpty"/>).</returns>
		PageContent GetContent(PageInfo page);

		/// <summary>
		/// Gets the content of a draft of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The draft, or <c>null</c> if no draft exists.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		PageContent GetDraft(PageInfo page);

		/// <summary>
		/// Deletes a draft of a Page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the draft is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		bool DeleteDraft(PageInfo page);

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="page">The Page to get the Backups of.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		int[] GetBackups(PageInfo page);

		/// <summary>
		/// Gets the Content of a Backup of a Page.
		/// </summary>
		/// <param name="page">The Page to get the backup of.</param>
		/// <param name="revision">The Backup/Revision number.</param>
		/// <returns>The Page Backup.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		PageContent GetBackupContent(PageInfo page, int revision);

		/// <summary>
		/// Forces to overwrite or create a Backup.
		/// </summary>
		/// <param name="content">The Backup content.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>True if the Backup has been created successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="content"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		bool SetBackupContent(PageContent content, int revision);

		/// <summary>
		/// Adds a Page.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page Name.</param>
		/// <param name="creationDateTime">The creation Date/Time.</param>
		/// <returns>The correct PageInfo object or null.</returns>
		/// <remarks>This method should <b>not</b> create the content of the Page.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		PageInfo AddPage(string nspace, string name, DateTime creationDateTime);

		/// <summary>
		/// Renames a Page.
		/// </summary>
		/// <param name="page">The Page to rename.</param>
		/// <param name="newName">The new Name.</param>
		/// <returns>The correct <see cref="T:PageInfo" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		PageInfo RenamePage(PageInfo page, string newName);

		/// <summary>
		/// Modifies the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="title">The Title of the Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="comment">The Comment of the editor, about this revision.</param>
		/// <param name="content">The Page Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO.</param>
		/// <param name="description">The description, usually used for SEO.</param>
		/// <param name="saveMode">The save mode for this modification.</param>
		/// <returns><c>true</c> if the Page has been modified successfully, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>saveMode</b> equals <b>Draft</b> and a draft already exists, it is overwritten.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="title"/> <paramref name="username"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="title"/> or <paramref name="username"/> are empty.</exception>
		bool ModifyPage(PageInfo page, string title, string username, DateTime dateTime, string comment, string content,
			string[] keywords, string description, SaveMode saveMode);

		/// <summary>
		/// Performs the rollback of a Page to a specified revision.
		/// </summary>
		/// <param name="page">The Page to rollback.</param>
		/// <param name="revision">The Revision to rollback the Page to.</param>
		/// <returns><c>true</c> if the rollback succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		bool RollbackPage(PageInfo page, int revision);

		/// <summary>
		/// Deletes the Backups of a Page, up to a specified revision.
		/// </summary>
		/// <param name="page">The Page to delete the backups of.</param>
		/// <param name="revision">The newest revision to delete (newer revision are kept) or -1 to delete all the Backups.</param>
		/// <returns><c>true</c> if the deletion succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than -1.</exception>
		bool DeleteBackups(PageInfo page, int revision);

		/// <summary>
		/// Removes a Page.
		/// </summary>
		/// <param name="page">The Page to remove.</param>
		/// <returns>True if the Page is removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		bool RemovePage(PageInfo page);

		/// <summary>
		/// Binds a Page with one or more Categories.
		/// </summary>
		/// <param name="page">The Page to bind.</param>
		/// <param name="categories">The Categories to bind the Page with.</param>
		/// <returns>True if the binding succeeded.</returns>
		/// <remarks>After a successful operation, the Page is bound with all and only the categories passed as argument.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="categories"/> are <c>null</c>.</exception>
		bool RebindPage(PageInfo page, string[] categories);

		/// <summary>
		/// Gets the Page Messages.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The list of the <b>first-level</b> Messages, containing the replies properly nested, sorted by date/time.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		Message[] GetMessages(PageInfo page);

		/// <summary>
		/// Gets the total number of Messages in a Page Discussion.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The number of messages.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		int GetMessageCount(PageInfo page);

		/// <summary>
		/// Removes all messages for a page and stores the new messages.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="messages">The new messages to store.</param>
		/// <returns><c>true</c> if the messages are stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="messages"/> are <c>null</c>.</exception>
		bool BulkStoreMessages(PageInfo page, Message[] messages);

		/// <summary>
		/// Adds a new Message to a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <param name="parent">The Parent Message ID, or -1.</param>
		/// <returns>True if the Message is added successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="username"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> or <paramref name="subject"/> are empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="parent"/> is less than -1.</exception>
		bool AddMessage(PageInfo page, string username, string subject, DateTime dateTime, string body, int parent);

		/// <summary>
		/// Removes a Message.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to remove.</param>
		/// <param name="removeReplies">A value specifying whether or not to remove the replies.</param>
		/// <returns>True if the Message is removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="id"/> is less than zero.</exception>
		bool RemoveMessage(PageInfo page, int id, bool removeReplies);

		/// <summary>
		/// Modifies a Message.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to modify.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <returns>True if the Message is modified successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="username"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="id"/> is less than zero.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> or <paramref name="subject"/> are empty.</exception>
		bool ModifyMessage(PageInfo page, int id, string username, string subject, DateTime dateTime, string body);

		/// <summary>
		/// Gets all the Navigation Paths in a Namespace.
		/// </summary>
		/// <param name="nspace">The Namespace.</param>
		/// <returns>All the Navigation Paths, sorted by name.</returns>
		NavigationPath[] GetNavigationPaths(NamespaceInfo nspace);

		/// <summary>
		/// Adds a new Navigation Path.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Path.</param>
		/// <param name="pages">The Pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> or <paramref name="pages"/> are empty.</exception>
		NavigationPath AddNavigationPath(string nspace, string name, PageInfo[] pages);

		/// <summary>
		/// Modifies an existing navigation path.
		/// </summary>
		/// <param name="path">The navigation path to modify.</param>
		/// <param name="pages">The new pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pages"/> is empty.</exception>
		NavigationPath ModifyNavigationPath(NavigationPath path, PageInfo[] pages);

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="path">The navigation path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> is <c>null</c>.</exception>
		bool RemoveNavigationPath(NavigationPath path);

		/// <summary>
		/// Gets all the snippets.
		/// </summary>
		/// <returns>All the snippets, sorted by name.</returns>
		Snippet[] GetSnippets();

		/// <summary>
		/// Adds a new snippet.
		/// </summary>
		/// <param name="name">The name of the snippet.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		Snippet AddSnippet(string name, string content);

		/// <summary>
		/// Modifies an existing snippet.
		/// </summary>
		/// <param name="name">The name of the snippet to modify.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		Snippet ModifySnippet(string name, string content);

		/// <summary>
		/// Removes a new Snippet.
		/// </summary>
		/// <param name="name">The Name of the Snippet to remove.</param>
		/// <returns><c>true</c> if the snippet is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		bool RemoveSnippet(string name);

		/// <summary>
		/// Gets all the content templates.
		/// </summary>
		/// <returns>All the content templates, sorted by name.</returns>
		ContentTemplate[] GetContentTemplates();

		/// <summary>
		/// Adds a new content template.
		/// </summary>
		/// <param name="name">The name of template.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		ContentTemplate AddContentTemplate(string name, string content);

		/// <summary>
		/// Modifies an existing content template.
		/// </summary>
		/// <param name="name">The name of the template to modify.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate" /> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		ContentTemplate ModifyContentTemplate(string name, string content);

		/// <summary>
		/// Removes a content template.
		/// </summary>
		/// <param name="name">The name of the template to remove.</param>
		/// <returns><c>true</c> if the template is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		bool RemoveContentTemplate(string name);

	}

	/// <summary>
	/// Lists legal saving modes.
	/// </summary>
	public enum SaveMode {
		/// <summary>
		/// Save the content.
		/// </summary>
		Normal,
		/// <summary>
		/// Backup the previous content, then save the current content.
		/// </summary>
		Backup,
		/// <summary>
		/// Save the content as draft.
		/// </summary>
		Draft
	}

}
