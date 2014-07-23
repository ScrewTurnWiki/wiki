
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
using System.Drawing;

namespace ScrewTurn.Wiki {

	// No BasePage because compression/language selection are not needed
	public partial class CaptchaPage : Page {

		protected void Page_Load(object sender, EventArgs e) {
			Response.Clear();
			Response.ContentType = "image/jpeg";

			string s = (string)Session["__Captcha"];

			Font f = new Font("Times New Roman", 30, FontStyle.Italic | FontStyle.Bold, GraphicsUnit.Pixel);
			StringFormat format = new StringFormat();
			format.Alignment = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;

			Bitmap bmp = new Bitmap(150, 40, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			Graphics g = Graphics.FromImage(bmp);
			g.Clear(Color.White);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

			g.DrawString(s, f, Brushes.Black, new RectangleF(0, 0, bmp.Width, bmp.Height), format);

			for(int i = -2; i < bmp.Width / 10; i++) {
				g.DrawLine(Pens.OrangeRed, i * 10, 0, i * 10 + 20, bmp.Height);
			}
			for(int i = -2; i < bmp.Width / 10 + 10; i++) {
				g.DrawLine(Pens.Blue, i * 10, 0, i * 10 - 60, bmp.Height);
			}

			g.Dispose();

			bmp.Save(Response.OutputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
		}

	}

}
