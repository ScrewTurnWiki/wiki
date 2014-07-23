
create table [User] (
	[Username] nvarchar(100) not null,
	[PasswordHash] varchar(100) not null,
	[DisplayName] nvarchar(150),
	[Email] varchar(100) not null,
	[Active] bit not null,
	[DateTime] datetime not null,
	constraint [PK_User] primary key clustered ([Username])
)

create table [UserGroup] (
	[Name] nvarchar(100) not null,
	[Description] nvarchar(150),
	constraint [PK_UserGroup] primary key clustered ([Name])
)

create table [UserGroupMembership] (
	[User] nvarchar(100) not null
		constraint [FK_UserGroupMembership_User] references [User]([Username])
		on delete cascade on update cascade,
	[UserGroup] nvarchar(100) not null
		constraint [FK_UserGroupMembership_UserGroup] references [UserGroup]([Name])
		on delete cascade on update cascade,
	constraint [PK_UserGroupMembership] primary key clustered ([User], [UserGroup])
)

create table [UserData] (
	[User] nvarchar(100) not null
		constraint [FK_UserData_User] references [User]([Username])
		on delete cascade on update cascade,
	[Key] nvarchar(100) not null,
	[Data] nvarchar(4000) not null,
	constraint [PK_UserData] primary key clustered ([User], [Key])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Users') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Users', 3000)
end
