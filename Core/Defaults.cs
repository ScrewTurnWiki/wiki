
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains default values.
	/// </summary>
	public static class Defaults {

		/// <summary>
		/// The default content of the main page.
		/// </summary>
		public const string MainPageContent = @"Welcome to '''{WIKITITLE}'''!{BR}
This is the main page of your new ScrewTurn Wiki, created for you by the system.

You should edit this page, using the ''Edit'' button in the top-right corner of the screen. You can also create a new page, using the ''Create a new Page'' link in the sidebar on the left.

If you need help, try to visit [http://www.screwturn.eu|our website] or [http://www.screwturn.eu/forum|our forum].

'''Warning''': remember to setup the ''admin'' account by editing the {{Web.config}} file placed in the root directory of the Wiki. It is ''extremely dangerous'' to keep the default password.";

		/// <summary>
		/// The default content of the main page of a sub-namespace.
		/// </summary>
		public const string MainPageContentForSubNamespace = @"Welcome to the '''{NAMESPACE}''' namespace of '''{WIKITITLE}'''!{BR}
This is the main page of the namespace, created for you by the system.

You should edit this page, using the ''Edit'' button in the top-right corner of the screen. You can also create a new page, using the ''Create a new Page'' link in the sidebar on the left.

If you need help, try to visit [http://www.screwturn.eu|our website] or [http://www.screwturn.eu/forum|our forum].";

		/// <summary>
		/// The default content of the account activation message.
		/// </summary>
		public const string AccountActivationMessageContent = @"Hi ##USERNAME## and welcome to ##WIKITITLE##!
You must activate your new ##WIKITITLE## Account within 24 hours, following the link below.

##ACTIVATIONLINK##

If you have any trouble, please contact us at our Email address, ##EMAILADDRESS## .

Thank you.

Best regards,
The ##WIKITITLE## Team.";

		/// <summary>
		/// The default content of the edit notice.
		/// </summary>
		public const string EditNoticeContent = @"Please '''do not''' include contents covered by copyright without the explicit permission of the Author. Always preview the result before saving.{BR}
If you are having trouble, please visit the [http://www.screwturn.eu/Help.ashx|Help section] at the [http://www.screwturn.eu|ScrewTurn Wiki Website].";

		/// <summary>
		/// The default content of the footer.
		/// </summary>
		public const string FooterContent = @"<p class=""small"">[http://www.screwturn.eu|ScrewTurn Wiki] version {WIKIVERSION}. Some of the icons created by [http://www.famfamfam.com|FamFamFam].</p>";

		/// <summary>
		/// The default content of the header.
		/// </summary>
		public const string HeaderContent = @"<div style=""float: right;"">Welcome {USERNAME}, you are in: {NAMESPACEDROPDOWN} &bull; {LOGINLOGOUT}</div><h1>{WIKITITLE}</h1>";

		/// <summary>
		/// The default content of the password reset message.
		/// </summary>
		public const string PasswordResetProcedureMessageContent = @"Hi ##USERNAME##!
Your can change your password following the instructions you will see at this link:
    ##LINK##

If you have any trouble, please contact us at our Email address, ##EMAILADDRESS## .

Thank you.

Best regards,
The ##WIKITITLE## Team.";

		/// <summary>
		/// The default content of the sidebar.
		/// </summary>
		public const string SidebarContent = @"<div style=""float: right;"">
<a href=""RSS.aspx"" title=""Update notifications for {WIKITITLE} (RSS 2.0)""><img src=""{THEMEPATH}Images/RSS.png"" alt=""RSS"" /></a>
<a href=""RSS.aspx?Discuss=1"" title=""Update notifications for {WIKITITLE} Discussions (RSS 2.0)""><img src=""{THEMEPATH}Images/RSS-Discussion.png"" alt=""RSS"" /></a></div>
====Navigation====
* '''[MainPage|Main Page]'''

* [RandPage.aspx|Random Page]
* [Edit.aspx|Create a new Page]
* [AllPages.aspx|All Pages]
* [Category.aspx|Categories]
* [NavPath.aspx|Navigation Paths]

* [AdminHome.aspx|Administration]
* [Upload.aspx|File Management]

* [Register.aspx|Create Account]

<small>'''Search the wiki'''</small>{BR}
{SEARCHBOX}

[image|PoweredBy|Images/PoweredBy.png|http://www.screwturn.eu]";

		/// <summary>
		/// The default content of the sidebar of a sub-namespace.
		/// </summary>
		public const string SidebarContentForSubNamespace = @"<div style=""float: right;"">
<a href=""{NAMESPACE}.RSS.aspx"" title=""Update notifications for {WIKITITLE} ({NAMESPACE}) (RSS 2.0)""><img src=""{THEMEPATH}Images/RSS.png"" alt=""RSS"" /></a>
<a href=""{NAMESPACE}.RSS.aspx?Discuss=1"" title=""Update notifications for {WIKITITLE} Discussions ({NAMESPACE}) (RSS 2.0)""><img src=""{THEMEPATH}Images/RSS-Discussion.png"" alt=""RSS"" /></a></div>
====Navigation ({NAMESPACE})====
* '''[MainPage|Main Page]'''
* [++MainPage|Main Page (root)]

* [RandPage.aspx|Random Page]
* [Edit.aspx|Create a new Page]
* [AllPages.aspx|All Pages]
* [Category.aspx|Categories]
* [NavPath.aspx|Navigation Paths]

* [AdminHome.aspx|Administration]
* [Upload.aspx|File Management]

* [Register.aspx|Create Account]

<small>'''Search the wiki'''</small>{BR}
{SEARCHBOX}

[image|PoweredBy|Images/PoweredBy.png|http://www.screwturn.eu]";

		/// <summary>
		/// The default content of the page change email message.
		/// </summary>
		public const string PageChangeMessage = @"The page ""##PAGE##"" was modified by ##USER## on ##DATETIME##.
Author's comment: ##COMMENT##.

The page can be found at the following address:
##LINK##

Thank you.

Best regards,
The ##WIKITITLE## Team.";

		/// <summary>
		/// The default content of the discussion change email message.
		/// </summary>
		public const string DiscussionChangeMessage = @"A new message was posted on the page ""##PAGE##"" by ##USER## on ##DATETIME##.

The subject of the message is ""##SUBJECT##"" and it can be found at the following address:
##LINK##

Thank you.

Best regards,
The ##WIKITITLE## Team.";

		/// <summary>
		/// The default content of the approve draft email message.
		/// </summary>
		public const string ApproveDraftMessage = @"A draft for the page ""##PAGE##"" was created or modified by ##USER## on ##DATETIME## and is currently held for **approval**.
Author's comment: ##COMMENT##.

The draft can be found and edited at the following address:
##LINK##
You can directly approve or reject the draft at the following address:
##LINK2##

Please note that the draft will not be displayed until it is approved.

Thank you.

Best regards,
The ##WIKITITLE## Team.";

	}

}
