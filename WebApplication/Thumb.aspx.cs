
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScrewTurn.Wiki {

	// No BasePage because compression/language selection are not needed
	public partial class Thumb : Page {

		protected void Page_Load(object sender, EventArgs e) {

			string filename = Request["File"];
			if(string.IsNullOrEmpty(filename)) {
				Response.Write("No file specified.");
				return;
			}

			// Remove ".." sequences that might be a security issue
			filename = filename.Replace("..", "");

			string page = Request["Page"];
			PageInfo pageInfo = Pages.FindPage(page);
			bool isPageAttachment = !string.IsNullOrEmpty(page);

			if(isPageAttachment && pageInfo == null) {
				Response.StatusCode = 404;
				Response.Write("File not found.");
				return;
			}

			IFilesStorageProviderV30 provider = null;

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

			string size = Request["Size"];
			if(string.IsNullOrEmpty(size)) size = "small";
			size = size.ToLowerInvariant();

			// Verify permissions
			bool canDownload = false;

			if(pageInfo != null) {
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
			}

			// Contains the image bytes
			MemoryStream ms = new MemoryStream(1048576);
			long fileSize = 0;

			// Load from provider
			if(string.IsNullOrEmpty(page)) {
				bool retrieved = false;
				try {
					retrieved = provider.RetrieveFile(filename, ms, false);
				}
				catch(ArgumentException ex) {
					Log.LogEntry("Attempted to create thumb of inexistent file (" + filename + ")\n" + ex.ToString(), EntryType.Warning, Log.SystemUsername);
				}

				if(!retrieved) {
					Response.StatusCode = 404;
					Response.Write("File not found.");
					return;
				}

				fileSize = provider.GetFileDetails(filename).Size;
			}
			else {
				if(pageInfo == null) {
					Response.StatusCode = 404;
					Response.Write("Page not found.");
					return;
				}

				bool retrieved = false;
				try {
					retrieved = provider.RetrievePageAttachment(pageInfo, filename, ms, false);
				}
				catch(ArgumentException ex) {
					Log.LogEntry("Attempted to create thumb of inexistent attachment (" + page + "/" + filename + ")\n" + ex.ToString(), EntryType.Warning, Log.SystemUsername);
				}

				if(!retrieved) {
					Response.StatusCode = 404;
					Response.Write("File not found.");
					return;
				}

				fileSize = provider.GetPageAttachmentDetails(pageInfo, filename).Size;
			}

			ms.Seek(0, SeekOrigin.Begin);

			int rotation = 0;
			int.TryParse(Request["Rot"], out rotation);

			// Load the source image
			System.Drawing.Image source = System.Drawing.Image.FromStream(ms);

			// Destination bitmap
			Bitmap result = null;
			System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

			if(size == "big") {
				// Big thumb (outer size 200x200)
				result = new Bitmap(200, 200, pixelFormat);
			}
            else if(size == "imgeditprev") {
                // Image Editor Preview thumb (outer size from Request["dim"], if null 200x200)
                if(!string.IsNullOrEmpty(Request["Width"]) && !string.IsNullOrEmpty(Request["Height"])) {
                    try {
                        result = new Bitmap(
							rotation != 90 && rotation != 270 ? int.Parse(Request["Width"]) : int.Parse(Request["Height"]),
							rotation != 90 && rotation != 270 ? int.Parse(Request["Height"]) : int.Parse(Request["Width"]),
							pixelFormat);
                    }
                    catch(FormatException) {
                        result = new Bitmap(200, 200, pixelFormat);
                    }
                }
                else result = new Bitmap(200, 200, pixelFormat);
            }
            else {
                // Small thumb (outer size 48x48)
                result = new Bitmap(48, 48, pixelFormat);
            }

			// Get Graphics object for destination bitmap
			Graphics g = Graphics.FromImage(result);

			if(source.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb) {
				g.Clear(Color.Transparent);
			}
			else {
				g.Clear(Color.White);
			}

			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

			g.TranslateTransform(result.Width / 2, result.Height / 2);
			g.RotateTransform(rotation);
			g.TranslateTransform(-result.Width / 2, -result.Height / 2);

			// Draw bitmap
			g.DrawImage(source, GetImageRectangle(result.Width, result.Height,
				rotation != 90 && rotation != 270 ? source.Width : source.Height,
				rotation != 90 && rotation != 270 ? source.Height : source.Width,
				rotation == 90 || rotation == 270));

			if(!string.IsNullOrEmpty(Request["Info"]) && size == "big") {
				// Draw image information
				RectangleF r = new RectangleF(0, 0, result.Width, 20);
				StringFormat f = new StringFormat();
				f.Alignment = StringAlignment.Center;
				//f.LineAlignment = StringAlignment.Center;
				GraphicsPath path = new GraphicsPath();
				path.AddString(string.Format("{0}x{1} - {2}", source.Width, source.Height,
					Tools.BytesToString(fileSize)),
					new FontFamily("Verdana"), 0, 12, new Point(result.Width / 2, 2), f);
				Pen pen = new Pen(Brushes.Black, 2F);
				g.DrawPath(pen, path);
				g.FillPath(Brushes.White, path);
			}

			// Write result in output stream in JPEG or PNG format
			if(source.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb) {
				Response.ContentType = "image/png";
			}
			else {
				Response.ContentType = "image/jpeg";
			}
			
			// This invariably throws an exception (A generic error occurred in GDI+) - an intermediate buffer is needed
			// The possible cause is that PNG format requires to read from the output stream, and Response.OutputStream does not support reading
			//result.Save(Response.OutputStream, System.Drawing.Imaging.ImageFormat.Png);

			MemoryStream tempStream = new MemoryStream(65536); // 32 KB
			if(source.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb) {
				result.Save(tempStream, System.Drawing.Imaging.ImageFormat.Png);
			}
			else {
				result.Save(tempStream, System.Drawing.Imaging.ImageFormat.Jpeg);
			}
			Response.OutputStream.Write(tempStream.ToArray(), 0, (int)tempStream.Length);
			tempStream.Dispose();

			ms.Dispose();

			source.Dispose();
			g.Dispose();
			result.Dispose();
		}

		private Rectangle GetImageRectangle(int targetW, int targetH, int w, int h, bool swapped) {
			if(w == h) {
				// Square
				return new Rectangle(0, 0, targetW, targetH);
			}
			else if(w > h) {
				// Landscape
				float scale = (float)targetW / (float)w;
				if(targetW > w) scale = 1;
				int width = (int)(w * scale);
				int height = (int)(h * scale);

				if(swapped) {
					int temp = width;
					width = height;
					height = temp;
				}

				return new Rectangle((targetW - width) / 2, (targetH - height) / 2, width, height);
			}
			else {
				// Portrait
				float scale = (float)targetH / (float)h;
				if(targetH > h) scale = 1;
				int width = (int)(w * scale);
				int height = (int)(h * scale);

				if(swapped) {
					int temp = width;
					width = height;
					height = temp;
				}

				return new Rectangle((targetW - width) / 2, (targetH - height) / 2, width, height);
			}
		}

	}

}
