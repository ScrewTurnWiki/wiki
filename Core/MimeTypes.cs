using System.Collections.Generic;

namespace ScrewTurn.Wiki
{

	/// <summary>
	/// Contains a list of MIME Types.
	/// </summary>
	public static class MimeTypes
	{

		private static Dictionary<string, string> _types;

		/// <summary>
		/// Initializes the list of the MIME Types, with the most common media types.
		/// </summary>
		public static void Init( )
		{
			_types = new Dictionary<string, string>
			         {
				         { "jpg", "image/jpeg" },
				         { "jpeg", "image/jpeg" },
				         { "jpe", "image/jpeg" },
				         { "gif", "image/gif" },
				         { "png", "image/png" },
				         { "bmp", "image/bmp" },
				         { "tif", "image/tiff" },
				         { "tiff", "image/tiff" },
				         { "svg", "image/svg+xml" },
				         { "ico", "image/x-icon" },
				         { "txt", "text/plain" },
				         { "htm", "text/html" },
				         { "html", "text/html" },
				         { "xhtml", "text/xhtml" },
				         { "xml", "text/xml" },
				         { "xsl", "text/xsl" },
				         { "dtd", "application/xml-dtd" },
				         { "css", "text/css" },
				         { "rtf", "text/rtf" },
				         { "zip", "application/zip" },
				         { "tar", "application/x-tar" },
				         { "ogg", "application/ogg" },
				         { "swf", "application/x-shockwave-flash" },
				         { "mpga", "audio/mpeg" },
				         { "mp2", "audio/mpeg" },
				         { "mp3", "audio/mpeg" },
				         { "m3u", "audio/x-mpegurl" },
				         { "ram", "audio/x-pn-realaudio" },
				         { "ra", "audio/x-pn-realaudio" },
				         { "rm", "application/vnd.rn-realmedia" },
				         { "wav", "application/x-wav" },
				         { "mpg", "video/mpeg" },
				         { "mpeg", "video/mpeg" },
				         { "mpe", "video/mpeg" },
				         { "mov", "video/quicktime" },
				         { "qt", "video/quicktime" },
				         { "avi", "video/x-msvideo" },
				         { "doc", "application/msword" },
				         { "xls", "application/vnd.ms-excel" },
				         { "ppt", "application/vnd.ms-powerpoint" },
				         { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
				         { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
				         { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
				         { "pdf", "application/pdf" },
				         { "ai", "application/postscript" },
				         { "ps", "application/postscript" },
				         { "eps", "application/postscript" }
			         };

		}

		/// <summary>
		/// Gets the list of the MIME Types.
		/// </summary>
		public static Dictionary<string, string> Types
		{
			get { return _types; }
		}

	}
}
