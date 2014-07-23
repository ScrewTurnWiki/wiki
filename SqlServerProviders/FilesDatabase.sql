
create table [Directory] (
	[FullPath] nvarchar(250) not null,
	[Parent] nvarchar(250),
	constraint [PK_Directory] primary key clustered ([FullPath])
)

create table [File] (
	[Name] nvarchar(200) not null,
	[Directory] nvarchar(250) not null
		constraint [FK_File_Directory] references [Directory]([FullPath])
		on delete cascade on update cascade,
	[Size] bigint not null,
	[Downloads] int not null,
	[LastModified] datetime not null,
	[Data] varbinary(max) not null,
	constraint [PK_File] primary key clustered ([Name], [Directory])
)

create table [Attachment] (
	[Name] nvarchar(200) not null,
	[Page] nvarchar(200) not null,
	[Size] bigint not null,
	[Downloads] int not null,
	[LastModified] datetime not null,
	[Data] varbinary(max) not null,
	constraint[PK_Attachment] primary key clustered ([Name], [Page])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Files') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Files', 3000)
end

if (select count([FullPath]) from [Directory] where [FullPath] = '/') = 0
begin
	insert into [Directory] ([FullPath], [Parent]) values ('/', NULL)
end
