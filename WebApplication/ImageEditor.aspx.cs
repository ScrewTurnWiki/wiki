
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using ScrewTurn.Wiki;
using ScrewTurn.Wiki.PluginFramework;
using System.Drawing.Imaging;

namespace ScrewTurn.Wiki {

	public partial class ImageEditor : BasePage {

		private MemoryStream resultMemStream = null;
		private string file = "";
		private string page = "";
		private IFilesStorageProviderV30 provider = null;

		protected void Page_Load(object sender, EventArgs e) {
			SetProvider();
			SetInputData();

			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames();
			string dir = Tools.GetDirectoryName(file);

			// Verify permissions
			bool canUpload = AuthChecker.CheckActionForDirectory(provider, dir,
				Actions.ForDirectories.UploadFiles, currentUser, currentGroups);
			bool canDeleteFiles = AuthChecker.CheckActionForDirectory(provider, dir,
				Actions.ForDirectories.DeleteFiles, currentUser, currentGroups);

			if(!canUpload || !canDeleteFiles) UrlTools.Redirect("AccessDenied.aspx");

			// Inject the proper stylesheet in page head
			Literal l = new Literal();
			l.Text = Tools.GetIncludes(DetectNamespace());
			Page.Header.Controls.Add(l);

			ResizeImage();
		}

		private void SetProvider() {
			string p = Request["Provider"];
			if(string.IsNullOrEmpty(p)) {
				p = Settings.DefaultFilesProvider;
			}
			provider = Collectors.FilesProviderCollector.GetProvider(p);
		}

		private void SetInputData() {
			page = Request["page"];

			file = Request["File"];
			if(string.IsNullOrEmpty(file)) {
				Response.Write("No file specified.");
				return;
			}
			file = file.Replace("..", "");
		}

		private int GetSelectedRotation() {
			if(rdo90CW.Checked) return 90;
			else if(rdo90CCW.Checked) return 270;
			else if(rdo180.Checked) return 180;
			else return 0;
		}

		private void ResizeImage() {
			SetProvider();
			SetInputData();

			// Contains the image bytes
			MemoryStream ms = new MemoryStream(1048576);

			// Load from provider
			if(string.IsNullOrEmpty(page)) {
				if(!provider.RetrieveFile(file, ms, false)) {
					Response.StatusCode = 404;
					Response.Write("File not found.");
					return;
				}
			}
			else {
				PageInfo info = Pages.FindPage(page);
				if(info == null) {
					Response.StatusCode = 404;
					Response.WriteFile("Page not found.");
					return;
				}

				if(!provider.RetrievePageAttachment(info, file, ms, false)) {
					Response.StatusCode = 404;
					Response.WriteFile("File not found.");
					return;
				}
			}

			// Setup new file name and image format
			if(!Page.IsPostBack) {
				string ext = Path.GetExtension(file);
				txtNewName.Text = Path.GetFileNameWithoutExtension(file) + "-2" + ext;
				switch(ext.ToLowerInvariant()) {
					case ".jpg":
					case ".jpeg":
						rdoPng.Checked = false;
						rdoJpegHigh.Checked = true;
						rdoJpegMedium.Checked = false;
						break;
					default:
						rdoPng.Checked = true;
						rdoJpegHigh.Checked = false;
						rdoJpegMedium.Checked = false;
						break;
				}

			}

			ms.Seek(0, SeekOrigin.Begin);

			// Load the source image
			System.Drawing.Image source = System.Drawing.Image.FromStream(ms);

			lblCurrentSize.Text = string.Format("{0}x{1}", source.Width, source.Height);
			if(!Page.IsPostBack) {
				txtWidth.Text = source.Width.ToString();
				txtHeight.Text = source.Height.ToString();
			}

			// Put dimension in script
			lblDimensions.Text = "<script type=\"text/javascript\"><!--\r\nvar width = " +
				source.Width.ToString() +
				";\r\nvar height = " + source.Height.ToString() +
				";\r\n// -->\r\n</script>";

			int resultWidth = source.Width;
			int resultHeight = source.Height;

			if(rdoPercentage.Checked) {
				// Resize by percentage
				int dim = 100;
				if(string.IsNullOrEmpty(txtPercentage.Text)) dim = 100;
				else {
					try {
						dim = int.Parse(txtPercentage.Text);
					}
					catch(FormatException) { }
				}

				// Possible final preview dimensions
				resultWidth = source.Width * dim / 100;
				resultHeight = source.Height * dim / 100;
			}
			else {
				// Resize by pixel
				if(!string.IsNullOrEmpty(txtWidth.Text) && !string.IsNullOrEmpty(txtHeight.Text)) {
					try {
						resultWidth = int.Parse(txtWidth.Text);
						resultHeight = int.Parse(txtHeight.Text);
					}
					catch(FormatException) { }
				}
			}

			int rotation = GetSelectedRotation();

			// Draw preview
			if(resultWidth > 290 || resultHeight > 290) {
				int previewWidth = resultWidth;
				int previewHeight = resultHeight;

				// Max preview dimension 290x290
				if(resultWidth > resultHeight) {
					previewWidth = 290;
					previewHeight = (int)((float)290 / (float)resultWidth * (float)resultHeight);
					lblScale.Text = string.Format("{0:N0}", (float)290 / (float)resultWidth * 100);
				}
				else {
					previewHeight = 290;
					previewWidth = (int)((float)290 / (float)resultHeight * (float)resultWidth);
					lblScale.Text = string.Format("{0:N0}", (float)290 / (float)resultHeight * 100);
				}
				imgPreview.ImageUrl = "Thumb.aspx?File=" + Request["File"] +
					@"&Size=imgeditprev&Width=" + previewWidth + @"&Height=" + previewHeight +
					@"&Page=" + (string.IsNullOrEmpty(page) ? "" : page) +
					(!rdoNoRotation.Checked ? ("&Rot=" + rotation.ToString()) : "");
			}
			else {
				lblScale.Text = "100";
				imgPreview.ImageUrl = "Thumb.aspx?File=" + Request["File"] +
					@"&Size=imgeditprev&Width=" + resultWidth + @"&Height=" + resultHeight +
					@"&Page=" + (string.IsNullOrEmpty(page) ? "" : page) +
					(!rdoNoRotation.Checked ? ("&Rot=" + rotation.ToString()) : "");
			}

			// Destination bitmap
			Bitmap result = new Bitmap(
				rotation != 90 && rotation != 270 ? resultWidth : resultHeight,
				rotation != 90 && rotation != 270 ? resultHeight : resultWidth,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			// Get Graphics object for destination bitmap
			Graphics g = Graphics.FromImage(result);
			g.Clear(Color.Transparent);

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

			// Prepare encoder parameters
			string format = GetCurrentFormat();
			resultMemStream = new MemoryStream(1048576);

			// Only JPEG and PNG images are editable
			if(format == "image/jpeg") {
				EncoderParameters encoderParams = new EncoderParameters(1);
				encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, rdoJpegHigh.Checked ? 100L : 60L);

				result.Save(resultMemStream, GetEncoderInfo(format), encoderParams);
			}
			else {
				result.Save(resultMemStream, ImageFormat.Png);
			}

			resultMemStream.Seek(0, SeekOrigin.Begin);

			// Dispose of source and result bitmaps
			source.Dispose();
			g.Dispose();
			result.Dispose();
		}

		private static ImageCodecInfo GetEncoderInfo(string mimeType) {
			int j;
			ImageCodecInfo[] encoders;
			encoders = ImageCodecInfo.GetImageEncoders();
			for(j = 0; j < encoders.Length; ++j) {
				if(encoders[j].MimeType == mimeType)
					return encoders[j];
			}
			return null;
		}

		private string GetCurrentFormat() {
			if(chkNewName.Checked) {
				if(rdoPng.Checked) return "image/png";
				else return "image/jpeg";
			}
			else {
				switch(Path.GetExtension(file).ToLowerInvariant()) {
					case ".jpg":
					case ".jpeg":
						return "image/jpeg";
					default:
						return "image/png";
				}
			}
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

		protected void btnPreview_Click(object sender, EventArgs e) {
			//ResizeImage();
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			ResizeImage();
			bool done = false;

			txtNewName.Text = txtNewName.Text.Trim();

			string targetName = chkNewName.Checked ? txtNewName.Text : Path.GetFileName(file);
			bool overwrite = !chkNewName.Checked;

			if(targetName.Length == 0 || Path.GetFileNameWithoutExtension(targetName).Length == 0) {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.InvalidFileName;
				return;
			}

			if(!overwrite) {
				// Force extension
				targetName = Path.ChangeExtension(targetName,
					rdoPng.Checked ? "png" : "jpg");
			}

			if(string.IsNullOrEmpty(page)) {
				string path = (file.LastIndexOf('/') > 0) ? file.Substring(0, file.LastIndexOf('/') + 1) : "";
				done = provider.StoreFile(path + targetName, resultMemStream, overwrite);
			}
			else {
				done = provider.StorePageAttachment(Pages.FindPage(page), targetName, resultMemStream, overwrite);
			}

			if(done) {
				lblResult.Text = Properties.Messages.FileSaved;
				lblResult.CssClass = "resultok";
			}
			else {
				lblResult.Text = Properties.Messages.CouldNotSaveFile;
				lblResult.CssClass = "resulterror";
			}
		}

		protected void chkNewName_CheckedChanged(object sender, EventArgs e) {
			txtNewName.Enabled = chkNewName.Checked;
			rdoPng.Enabled = chkNewName.Checked;
			rdoJpegHigh.Enabled = chkNewName.Checked;
			rdoJpegMedium.Enabled = chkNewName.Checked;
		}

		protected void rdoFormat_CheckedChanged(object sender, EventArgs e) {
			string ext = "";
			if(rdoPng.Checked) {
				ext = ".png";
			}
			else {
				ext = ".jpg";
			}
			txtNewName.Text = Path.ChangeExtension(txtNewName.Text, ext);
		}

	}

}
