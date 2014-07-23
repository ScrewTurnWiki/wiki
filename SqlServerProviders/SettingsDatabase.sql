
create table [Setting] (
	[Name] varchar(100) not null,
	[Value] nvarchar(4000) not null,
	constraint [PK_Setting] primary key clustered ([Name])
)

create table [Log] (
	[Id] int not null identity,
	[DateTime] datetime not null,
	[EntryType] char not null,
	[User] nvarchar(100) not null,
	[Message] nvarchar(4000) not null,
	constraint [PK_Log] primary key clustered ([Id])
)

create table [MetaDataItem] (
	[Name] varchar(100) not null,
	[Tag] nvarchar(100) not null,
	[Data] nvarchar(4000) not null,
	constraint [PK_MetaDataItem] primary key clustered ([Name], [Tag])
)

create table [RecentChange] (
	[Id] int not null identity,
	[Page] nvarchar(200) not null,
	[Title] nvarchar(200) not null,
	[MessageSubject] nvarchar(200),
	[DateTime] datetime not null,
	[User] nvarchar(100) not null,
	[Change] char not null,
	[Description] nvarchar(4000),
	constraint [PK_RecentChange] primary key clustered ([Id])
)

create table [PluginAssembly] (
	[Name] varchar(100) not null,
	[Assembly] varbinary(max) not null,
	constraint [PK_PluginAssembly] primary key clustered ([Name])
)

create table [PluginStatus] (
	[Name] varchar(150) not null,
	[Enabled] bit not null,
	[Configuration] nvarchar(4000) not null,
	constraint [PK_PluginStatus] primary key clustered ([Name])
)

create table [OutgoingLink] (
	[Source] nvarchar(100) not null,
	[Destination] nvarchar(100) not null,
	constraint [PK_OutgoingLink] primary key clustered ([Source], [Destination])
)

create table [AclEntry] (
	[Resource] nvarchar(200) not null,
	[Action] varchar(50) not null,
	[Subject] nvarchar(100) not null,
	[Value] char not null,
	constraint [PK_AclEntry] primary key clustered ([Resource], [Action], [Subject])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Settings') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Settings', 3000)
end
