
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages content templates.
	/// </summary>
	public static class Templates {

		/// <summary>
		/// Gets all the content templates.
		/// </summary>
		/// <returns>The content templates, sorted by name.</returns>
		public static List<ContentTemplate> GetTemplates() {
			List<ContentTemplate> result = new List<ContentTemplate>(20);

			// Retrieve templates from all providers
			foreach(IPagesStorageProviderV30 prov in Collectors.PagesProviderCollector.AllProviders) {
				result.AddRange(prov.GetContentTemplates());
			}

			result.Sort(new ContentTemplateNameComparer());

			return result;
		}

		/// <summary>
		/// Finds a content template.
		/// </summary>
		/// <param name="name">The name of the template to find.</param>
		/// <returns>The content template, or <c>null</c>.</returns>
		public static ContentTemplate Find(string name) {
			List<ContentTemplate> templates = GetTemplates();

			int index = templates.BinarySearch(new ContentTemplate(name, "", null), new ContentTemplateNameComparer());

			if(templates.Count > 0 && index >= 0) return templates[index];
			else return null;
		}

		/// <summary>
		/// Adds a new content template.
		/// </summary>
		/// <param name="name">The name of the template.</param>
		/// <param name="content">The content of the template.</param>
		/// <param name="provider">The target provider (<c>null</c> for the default provider).</param>
		/// <returns><c>true</c> if the template is added, <c>false</c> otherwise.</returns>
		public static bool AddTemplate(string name, string content, IPagesStorageProviderV30 provider) {
			if(Find(name) != null) return false;

			if(provider == null) provider = Collectors.PagesProviderCollector.GetProvider(Settings.DefaultPagesProvider);

			ContentTemplate result = provider.AddContentTemplate(name, content);

			if(result != null) Log.LogEntry("Content Template " + name + " created", EntryType.General, Log.SystemUsername);
			else Log.LogEntry("Creation failed for Content Template " + name, EntryType.Error, Log.SystemUsername);

			return result != null;
		}

		/// <summary>
		/// Removes a content template.
		/// </summary>
		/// <param name="template">The template to remove.</param>
		/// <returns><c>true</c> if the template is removed, <c>false</c> otherwise.</returns>
		public static bool RemoveTemplate(ContentTemplate template) {
			bool done = template.Provider.RemoveContentTemplate(template.Name);

			if(done) Log.LogEntry("Content Template " + template.Name + " deleted", EntryType.General, Log.SystemUsername);
			else Log.LogEntry("Deletion failed for Content Template " + template.Name, EntryType.Error, Log.SystemUsername);

			return done;
		}

		/// <summary>
		/// Modifies a content template.
		/// </summary>
		/// <param name="template">The template to modify.</param>
		/// <param name="content">The new content of the template.</param>
		/// <returns><c>true</c> if the template is modified, <c>false</c> otherwise.</returns>
		public static bool ModifyTemplate(ContentTemplate template, string content) {
			ContentTemplate result = template.Provider.ModifyContentTemplate(template.Name, content);

			if(result != null) Log.LogEntry("Content Template " + template.Name + " updated", EntryType.General, Log.SystemUsername);
			else Log.LogEntry("Update failed for Content Template " + template.Name, EntryType.Error, Log.SystemUsername);

			return result != null;
		}

	}

}
