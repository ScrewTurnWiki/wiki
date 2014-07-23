
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Globalization;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	// No BasePage because compression/language selection are not needed
	public partial class GetFile : Page {

		protected void Page_Load(object sender, EventArgs e) {
			string filename = Request["File"];

			if(filename == null) {
				Response.StatusCode = 404;
				Response.Write(Properties.Messages.FileNotFound);
				return;
			}

			// Remove ".." sequences that might be a security issue
			filename = filename.Replace("..", "");

			bool isPageAttachment = !string.IsNullOrEmpty(Request["Page"]);
			PageInfo pageInfo = isPageAttachment ? Pages.FindPage(Request["Page"]) : null;
			if(isPageAttachment && pageInfo == null) {
				Response.StatusCode = 404;
				Response.Write(Properties.Messages.FileNotFound);
				return;
			}

			IFilesStorageProviderV30 provider;

			if(!string.IsNullOrEmpty(Request["Provider"])) provider = Collectors.FilesProviderCollector.GetProvider(Request["Provider"]);
			else {
				if(isPageAttachment) provider = FilesAndAttachments.FindPageAttachmentProvider(pageInfo, filename);
				else provider = FilesAndAttachments.FindFileProvider(filename);
			}

			if(provider == null) {
				Response.StatusCode = 404;
				Response.Write("File not found.");
				return;
			}

			// Use canonical path format (leading with /)
			if(!isPageAttachment) {
				if(!filename.StartsWith("/")) filename = "/" + filename;
				filename = filename.Replace("\\", "/");
			}

			bool countHit = CountHit(filename);

			// Verify permissions
			bool canDownload = false;

			if(isPageAttachment) {
				canDownload = AuthChecker.CheckActionForPage(pageInfo, Actions.ForPages.DownloadAttachments,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames());
			}
			else {
				string dir = Tools.GetDirectoryName(filename);
				canDownload = AuthChecker.CheckActionForDirectory(provider, dir,
					 Actions.ForDirectories.DownloadFiles, SessionFacade.GetCurrentUsername(),
					 SessionFacade.GetCurrentGroupNames());
			}
			if(!canDownload) {
				Response.StatusCode = 401;
				return;
			}

			long size = -1;

			FileDetails details = null;
			if(isPageAttachment) details = provider.GetPageAttachmentDetails(pageInfo, filename);
			else details = provider.GetFileDetails(filename);

			if(details != null) size = details.Size;
			else {
				Log.LogEntry("Attempted to download an inexistent file/attachment (" + (pageInfo != null ? pageInfo.FullName + "/" : "") + filename + ")", EntryType.Warning, Log.SystemUsername);
				Response.StatusCode = 404;
				Response.Write("File not found.");
				return;
			}

			string mime = "";
			try {
				string ext = Path.GetExtension(filename);
				if(ext.StartsWith(".")) ext = ext.Substring(1).ToLowerInvariant(); // Remove trailing dot
				mime = GetMimeType(ext);
			}
			catch {
				// ext is null -> no mime type -> abort
				Response.Write(filename + "<br />");
				Response.StatusCode = 404;
				Response.Write("File not found.");
				//mime = "application/octet-stream";
				return;
			}

			// Prepare response
			Response.Clear();
			Response.AddHeader("content-type", mime);
			if(Request["AsStreamAttachment"] != null) {
				Response.AddHeader("content-disposition", "attachment;filename=\"" + Path.GetFileName(filename) + "\"");
			}
			else {
				Response.AddHeader("content-disposition", "inline;filename=\"" + Path.GetFileName(filename) + "\"");
			}
			Response.AddHeader("content-length", size.ToString());

			bool retrieved = false;
			if(isPageAttachment) {
				try {
					retrieved = provider.RetrievePageAttachment(pageInfo, filename, Response.OutputStream, countHit);
				}
				catch(ArgumentException ex) {
					Log.LogEntry("Attempted to download an inexistent attachment (" + pageInfo.FullName + "/" + filename + ")\n" + ex.ToString(), EntryType.Warning, Log.SystemUsername);
				}
			}
			else {
				try {
					retrieved = provider.RetrieveFile(filename, Response.OutputStream, countHit);
				}
				catch(ArgumentException ex) {
					Log.LogEntry("Attempted to download an inexistent file/attachment (" + filename + ")\n" + ex.ToString(), EntryType.Warning, Log.SystemUsername);
				}
			}

			if(!retrieved) {
				Response.StatusCode = 404;
				Response.Write("File not found.");
				return;
			}

			// Set the cache duration accordingly to the file date/time
			//Response.AddFileDependency(filename);
			//Response.Cache.SetETagFromFileDependencies();
			//Response.Cache.SetLastModifiedFromFileDependencies();
			Response.Cache.SetETag(filename.GetHashCode().ToString() + "-" + size.ToString());
			Response.Cache.SetCacheability(HttpCacheability.Public);
			Response.Cache.SetSlidingExpiration(true);
			Response.Cache.SetValidUntilExpires(true);
			Response.Cache.VaryByParams["File"] = true;
			Response.Cache.VaryByParams["Provider"] = true;
			Response.Cache.VaryByParams["Page"] = true;
			Response.Cache.VaryByParams["IsPageAttachment"] = true;
		}

		private string GetMimeType(string ext) {
			string mime = "";
			if(MimeTypes.Types.TryGetValue(ext, out mime)) return mime;
			else return "application/octet-stream";
		}

		/// <summary>
		/// Gets a value indicating whether or not to count the hit.
		/// </summary>
		/// <param name="file">The name of the file.</param>
		/// <returns><c>true</c> if the hit must be counted, <c>false</c> otherwise.</returns>
		private bool CountHit(string file) {
			bool result = Request["NoHit"] != "1";

			if(!result) return false;
			else {
				FileDownloadCountFilterMode mode = Settings.FileDownloadCountFilterMode;
				if(mode == FileDownloadCountFilterMode.CountAll) return true;
				else {
					string[] allowedExtensions = Settings.FileDownloadCountFilter;
					string extension = Path.GetExtension(file);
					if(string.IsNullOrEmpty(extension)) return false;
					else extension = extension.Trim('.').ToLowerInvariant();

					bool found = false;
					foreach(string ex in allowedExtensions) {
						if(ex == extension) {
							found = true;
							break;
						}
					}

					if(found && mode == FileDownloadCountFilterMode.CountSpecifiedExtensions) return true;
					else if(!found && mode == FileDownloadCountFilterMode.ExcludeSpecifiedExtensions) return true;
					else return false;
				}
			}
		}

	}

}
