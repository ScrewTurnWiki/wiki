using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {
	
	[TestFixture]
	public class ReverseFormatterTests {

		private MockRepository mocks;

		[Test]
		[TestCase("<b>text</b>", "'''text'''")]
		[TestCase("<strong>text</strong>", "'''text'''")]
		[TestCase("<i>text</i>", "''text''")]
		[TestCase("<ul><li>prova <b>pippo</b></li><li>riga2</li></ul>", "* prova '''pippo'''\n* riga2\n")]
		[TestCase("<em>text</em>", "''text''")]
		[TestCase("<u>text</u>", "__text__")]
		[TestCase("<s>text</s>", "--text--")]
		[TestCase("<html><table border=\"1\" bgcolor=\"LightBlue\"><thead><tr><th>Cells x.1</th><th>Cells x.2</th></tr></thead><tbody><tr><td>Cell 1.1</td><td>Cell 1.2</td></tr><tr><td>Cell 2.1</td><td>Cell 2.2</td></tr></tbody></table></html>", "{| border=\"1\" bgcolor=\"LightBlue\" \n|- \n! Cells x.1\n! Cells x.2\n|- \n| Cell 1.1\n| Cell 1.2\n|- \n| Cell 2.1\n| Cell 2.2\n|}\n")]
		[TestCase("<ol><li><a class=\"internallink\" target=\"_blank\" href=\"www.try.com\" title=\"try\">try</a></li><li><a class=\"internallink\" target=\"_blank\" href=\"www.secondtry.com\" title=\"www.secondtry.com\">www.secondtry.com</a><br></li></ol>","# [^www.try.com|try]\n# [^www.secondtry.com]\n")]
		[TestCase("<table><tbody><tr><td bgcolor=\"Blue\">Styled Cell</td><td>Normal cell</td></tr><tr><td>Normal cell</td><td bgcolor=\"Yellow\">Styled cell</td></tr></tbody></table>", "{| \n|- \n|  bgcolor=\"Blue\"  | Styled Cell\n| Normal cell\n|- \n| Normal cell\n|  bgcolor=\"Yellow\"  | Styled cell\n|}\n")]
		[TestCase("<h1>text</h1>", "==text==\n")]
		[TestCase("<h2>text</h2>", "===text===\n")]
		[TestCase("<h3>text</h3>", "====text====\n")]
		[TestCase("<h4>text</s>", "=====text=====\n")]
		[TestCase("<code>inline code - monospace font</code>", "{{inline code - monospace font}}")]
		[TestCase("<h1></h1>", "----\n")]
		[TestCase("<h1> </h1>", "----\n")]
		[TestCase("<sup>text</sup>", "<sup>text</sup>")]
		[TestCase("<sub>text</sub>", "<sub>text</sub>")]
		[TestCase("<pre><b>text</b></pre>", "@@text@@")]
		[TestCase("<div class=\"indent\" style=\"margin: 0px; padding: 0px; padding-left: 15px\">text</div>", ": text\n")]
		[TestCase("<a href=\"Help.AllPages.aspx?Cat=Help.Wiki\" class=\"systemlink\" title=\"Help.Wiki\">Help.Wiki</a>", "[c:Help.Wiki|Help.Wiki]")]
		[TestCase("<code><b>text</b></code>", "{{'''text'''}}")]
		[TestCase("<div class=\"box\">text</div>", "(((text)))")]
		[TestCase("<div>text</div>", "\ntext\n")]
		[TestCase("<html>riga1<br /><b>riga2</b><br />riga3</html>", "riga1\n'''riga2'''\nriga3")]
		[TestCase("<html><ol><li>1</li><li>2</li><li>3<ol><li>3.1</li><li>3.2<ol><li>3.2.1</li></ol></li><li>3.3</li></ol></li><li>4 ciao</li></ol><br /></html>", "# 1\n# 2\n# 3\n## 3.1\n## 3.2\n### 3.2.1\n## 3.3\n# 4 ciao\n\n")]
		[TestCase("<ol><li>1</li><li>2</li></ol>", "# 1\n# 2\n")]
		[TestCase("<ul><li><img src=\"GetFile.aspx?File=/AmanuensMicro.png\" alt=\"Image\"></li><li><div class=\"imageleft\"><img class=\"image\" src=\"GetFile.aspx?File=/DownloadButton.png\" alt=\"Image\"></div></li><li><div class=\"imageright\"><a target=\"_blank\" href=\"www.tututu.tu\" title=\"guihojk\"><img class=\"image\" src=\"GetFile.aspx?File=/Checked.png\" alt=\"guihojk\"></a><p class=\"imagedescription\">guihojk</p></div></li><li><table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><img class=\"image\" src=\"GetFile.aspx?File=/Alert.png\" alt=\"auto\"><p class=\"imagedescription\">auto</p></td></tr></tbody></table><br></li></ul>", "* [image|Image|{UP}/AmanuensMicro.png]\n* [imageleft||{UP}/DownloadButton.png]\n* [imageright|guihojk|{UP}/Checked.png|^www.tututu.tu]\n* [imageauto|auto|{UP}/Alert.png]\n")]
		[TestCase("<ul><li>1</li><li>2</li></ul>", "* 1\n* 2\n")]
		[TestCase("<div class=\"imageright\"><img class=\"image\" src=\"GetFile.aspx?File=/Help/Desktop/image.png\"><p class=\"imagedescription\">description</p></div>", "[imageright|description|{UP}/Help/Desktop/image.png]")]
		[TestCase("<html><ul><li>Punto 1</li><li>Punto 2</li><li>Punto 3</li><li>Punto 4</li><li>Punto 5</li></ul></html>", "* Punto 1\n* Punto 2\n* Punto 3\n* Punto 4\n* Punto 5\n")]
		[TestCase("<ul><li>it 1<ul><li>1.1</li><li>1.2</li></ul></li><li>it2</li></ul>", "* it 1\n** 1.1\n** 1.2\n* it2\n")]
		[TestCase("<ul><li>it 1<ol><li>1.1</li><li>1.2</li></ol></li><li>it2</li></ul>", "* it 1\n*# 1.1\n*# 1.2\n* it2\n")]
		[TestCase("<ul><li><b>1</b></li><li>2</li></ul>", "* '''1'''\n* 2\n")]
		[TestCase("<html><a id=\"Init\" />I'm an anchor</html>", "[anchor|#Init]I'm an anchor")]
		[TestCase("<html><a class=\"internallink\" href=\"#init\" title=\"This recall an anchor\">This recall an anchor</a></html>", "[#init|This recall an anchor]")]
		[TestCase("<html><a class=\"externallink\" href=\"google.com\" title=\"BIG TITLE\" target=\"_blank\">BIG TITLE</a></html>", "[^google.com|BIG TITLE]")]
		[TestCase("<esc>try to esc tag</esc>", "<esc>try to esc tag</esc>")]
		[TestCase("<div class=\"imageleft\"><a target=\"_blank\" href=\"www.link.com\" title=\"left Align\"><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"left Align\" /></a><p class=\"imagedescription\">leftalign</p></div>", "[imageleft|leftalign|{UP(MainPage)}image.png|^www.link.com]")]
		[TestCase("<img src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"inlineimage\" />", "[image|inlineimage|{UP(MainPage)}image.png]\n")]
		[TestCase("<a target=\"_blank\" href=\"www.google.it\" title=\"description\"><img src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"description\" /></a>", "[image|description|{UP(MainPage)}image.png|^www.google.it]")]
		[TestCase("<table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"autoalign\" /><p class=\"imagedescription\">autoalign</p></td></tr></tbody></table>", "[imageauto|autoalign|{UP(MainPage)}image.png]")]
		[TestCase("<table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><a href=\"www.link.com\" title=\"Auto align\"><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"Auto align\" /></a><p class=\"imagedescription\">Auto align</p></td></tr></tbody></table>", "[imageauto|Auto align|{UP(MainPage)}image.png|www.link.com]")]
		[TestCase("<table cellspacing=\"0\" cellpadding=\"2\" style=\"background-color: #EEEEEE; margin: 0px auto;\"><caption>Styled Table</caption><tbody><tr style=\"background-color: #990000; color: #FFFFFF;\"><td>This is a cell</td><td>This is a cell</td><td>This is a cell</td></tr><tr><td style=\"background-color: #000000; color: #CCCCCC;\">Styled cell</td><td style=\"border: solid 1px #FF0000;\">Styled cell</td><td><b>Normal cell</b></td></tr><tr><td>Normal</td><td>Normal</td><td><a class=\"internallink\" href=\"Download.ashx\" title=\"Download\">Download</a></td></tr></tbody></table>","{| cellspacing=\"0\" cellpadding=\"2\" style=\"background-color: #EEEEEE; margin: 0px auto;\" \n|+ Styled Table\n|- style=\"background-color: #990000; color: #FFFFFF;\" \n| This is a cell\n| This is a cell\n| This is a cell\n|- \n|  style=\"background-color: #000000; color: #CCCCCC;\"  | Styled cell\n|  style=\"border: solid 1px #FF0000;\"  | Styled cell\n| '''Normal cell'''\n|- \n| Normal\n| Normal\n| [Download.ashx|Download]\n|}\n")]
		[TestCase("<pre>block code - [WikiMarkup] is ignored</pre>", "@@block code - [WikiMarkup] is ignored@@")]
		[TestCase(@"<a class=""unknownlink"" href=""test.ashx"" title=""test"">test</a>", "[test|test]")]
		[TestCase(@"<a class=""pagelink"" href=""MainPage.ashx"" title=""Main Page"">Main Page</a>", "[MainPage|Main Page]")]
		public void PlainTest(string input, string output) {
			Assert.AreEqual(output, ReverseFormatter.ReverseFormat(input));
		}

		[SetUp]
		public void SetUp() {
			mocks = new MockRepository();

			ISettingsStorageProviderV30 settingsProvider = mocks.StrictMock<ISettingsStorageProviderV30>();
			Expect.Call(settingsProvider.GetSetting("ProcessSingleLineBreaks")).Return("true").Repeat.Any();

			Collectors.SettingsProvider = settingsProvider;

			mocks.Replay(settingsProvider);
		}

		[TearDown]
		public void TearDown() {
			mocks.VerifyAll();
		}
	}
}