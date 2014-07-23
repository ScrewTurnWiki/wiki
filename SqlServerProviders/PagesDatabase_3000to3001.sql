
drop table [IndexWordMapping]
drop table [IndexWord]
drop table [IndexDocument]

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

update [Version] set [Version] = 3001 where [Component] = 'Pages'
