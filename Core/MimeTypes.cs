
using System;
using System.Collections.Generic;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains a list of MIME Types.
	/// </summary>
	public static class MimeTypes {
		
		private static Dictionary<string, string> types;

		/// <summary>
		/// Initializes the list of the MIME Types, with the most common media types.
		/// </summary>
		public static void Init() {
			types = new Dictionary<string, string>(100);

			// Images
			types.Add("jpg", "image/jpeg");
			types.Add("jpeg", "image/jpeg");
			types.Add("jpe", "image/jpeg");
			types.Add("gif", "image/gif");
			types.Add("png", "image/png");
			types.Add("bmp", "image/bmp");
			types.Add("tif", "image/tiff");
			types.Add("tiff", "image/tiff");
			types.Add("svg", "image/svg+xml");
			types.Add("ico", "image/x-icon");

			// Text
			types.Add("txt", "text/plain");
			types.Add("htm", "text/html");
			types.Add("html", "text/html");
			types.Add("xhtml", "text/xhtml");
			types.Add("xml", "text/xml");
			types.Add("xsl", "text/xsl");
			types.Add("dtd", "application/xml-dtd");
			types.Add("css", "text/css");
			types.Add("rtf", "text/rtf");
			
			// Archives
			types.Add("zip", "application/zip");
			types.Add("tar", "application/x-tar");

			// Multimedia
			types.Add("ogg", "application/ogg");
			types.Add("swf", "application/x-shockwave-flash");
			types.Add("mpga", "audio/mpeg");
			types.Add("mp2", "audio/mpeg");
			types.Add("mp3", "audio/mpeg");
			types.Add("m3u", "audio/x-mpegurl");
			types.Add("ram", "audio/x-pn-realaudio");
			types.Add("ra", "audio/x-pn-realaudio");
			types.Add("rm", "application/vnd.rn-realmedia");
			types.Add("wav", "application/x-wav");
			types.Add("mpg", "video/mpeg");
			types.Add("mpeg", "video/mpeg");
			types.Add("mpe", "video/mpeg");
			types.Add("mov", "video/quicktime");
			types.Add("qt", "video/quicktime");
			types.Add("avi", "video/x-msvideo");

			// Office
			types.Add("doc", "application/msword");
			types.Add("xls", "application/vnd.ms-excel");
			types.Add("ppt", "application/vnd.ms-powerpoint");
			// Map Office 2007 formats using the information found here:
			// http://www.therightstuff.de/2006/12/16/Office+2007+File+Icons+For+Windows+SharePoint+Services+20+And+SharePoint+Portal+Server+2003.aspx
			types.Add("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
			types.Add("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
			types.Add("pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");

			// Other
			types.Add("pdf", "application/pdf");
			types.Add("ai", "application/postscript");
			types.Add("ps", "application/postscript");
			types.Add("eps", "application/postscript");
		}

		/// <summary>
		/// Gets the list of the MIME Types.
		/// </summary>
		public static Dictionary<string, string> Types {
			get { return types; }
		}

	}
}
