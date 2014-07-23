
create table [Namespace] (
	[Name] nvarchar(100) not null,
	[DefaultPage] nvarchar(200),
	constraint [PK_Namespace] primary key clustered ([Name])
)

create table [Category](
	[Name] nvarchar(100) not null,
	[Namespace] nvarchar(100) not null
		constraint [FK_Category_Namespace] references [Namespace]([Name])
		on delete cascade on update cascade,
	constraint [PK_Category] primary key clustered ([Name], [Namespace])
)

create table [Page] (
	[Name] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null
		constraint [FK_Page_Namespace] references [Namespace]([Name])
		on delete cascade on update cascade,
	[CreationDateTime] datetime not null,
	constraint [PK_Page] primary key clustered ([Name], [Namespace])
)

-- Deleting/Renaming/Moving a page requires manually updating the binding
create table [CategoryBinding] (
	[Namespace] nvarchar(100) not null
		constraint [FK_CategoryBinding_Namespace] references [Namespace]([Name]),
	[Category] nvarchar(100) not null,
	[Page] nvarchar(200) not null,
	constraint [FK_CategoryBinding_Category] foreign key ([Category], [Namespace]) references [Category]([Name], [Namespace])
		on delete cascade on update cascade,
	constraint [FK_CategoryBinding_Page] foreign key ([Page], [Namespace]) references [Page]([Name], [Namespace])
		on delete no action on update no action,
	constraint [PK_CategoryBinding] primary key clustered ([Namespace], [Page], [Category])
)

create table [PageContent] (
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Revision] smallint not null,
	[Title] nvarchar(200) not null,
	[User] nvarchar(100) not null,
	[LastModified] datetime not null,
	[Comment] nvarchar(300),
	[Content] nvarchar(max) not null,
	[Description] nvarchar(200),
	constraint [FK_PageContent_Page] foreign key ([Page], [Namespace]) references [Page]([Name], [Namespace])
		on delete cascade on update cascade,
	constraint [PK_PageContent] primary key clustered ([Page], [Namespace], [Revision])
)

create table [PageKeyword] (
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Revision] smallint not null,
	[Keyword] nvarchar(50) not null,
	constraint [FK_PageKeyword_PageContent] foreign key ([Page], [Namespace], [Revision]) references [PageContent]([Page], [Namespace], [Revision])
		on delete cascade on update cascade,
	constraint [PK_PageKeyword] primary key clustered ([Page], [Namespace], [Revision], [Keyword])
)

create table [Message] (
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Id] smallint not null,
	[Parent] smallint,
	[Username] nvarchar(100) not null,
	[Subject] nvarchar(200) not null,
	[DateTime] datetime not null,
	[Body] nvarchar(max) not null,
	constraint [FK_Message_Page] foreign key ([Page], [Namespace]) references [Page]([Name], [Namespace])
		on delete cascade on update cascade,
	constraint [PK_Message] primary key clustered ([Page], [Namespace], [Id])
)

create table [NavigationPath] (
	[Name] nvarchar(100) not null,
	[Namespace] nvarchar(100) not null,
	[Page] nvarchar(200) not null,
	[Number] smallint not null,
	constraint [FK_NavigationPath_Page] foreign key ([Page], [Namespace]) references [Page]([Name], [Namespace])	
		on delete cascade on update cascade,
	constraint [PK_NavigationPath] primary key clustered ([Name], [Namespace], [Page])
)

create table [Snippet] (
	[Name] nvarchar(200) not null,
	[Content] nvarchar(max) not null,
	constraint [PK_Snippet] primary key clustered ([Name])
)

create table [ContentTemplate] (
	[Name] nvarchar(200) not null,
	[Content] nvarchar(max) not null,
	constraint [PK_ContentTemplate] primary key clustered ([Name])
)

create table [IndexDocument] (
	[Id] int not null,
	[Name] nvarchar(200) not null
		constraint [UQ_IndexDocument] unique,
	[Title] nvarchar(200) not null,
	[TypeTag] varchar(10) not null,
	[DateTime] datetime not null,
	constraint [PK_IndexDocument] primary key clustered ([Id])
)

create table [IndexWord] (
	[Id] int not null,
	[Text] nvarchar(200) not null
		constraint [UQ_IndexWord] unique,
	constraint [PK_IndexWord] primary key clustered ([Id])
)

create table [IndexWordMapping] (
	[Word] int not null
		constraint [FK_IndexWordMapping_IndexWord] references [IndexWord]([Id])
		on delete cascade on update cascade,
	[Document] int not null
		constraint [FK_IndexWordMapping_IndexDocument] references [IndexDocument]([Id])
		on delete cascade on update cascade,
	[FirstCharIndex] smallint not null,
	[WordIndex] smallint not null,
	[Location] tinyint not null,
	constraint [PK_IndexWordMapping] primary key clustered ([Word], [Document], [FirstCharIndex], [WordIndex], [Location])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Pages') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Pages', 3001)
end

if (select count([Name]) from [Namespace] where [Name] = '') = 0
begin
	insert into [Namespace] ([Name], [DefaultPage]) values ('', null)
end
