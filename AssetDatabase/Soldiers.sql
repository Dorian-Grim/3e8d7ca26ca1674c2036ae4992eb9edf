CREATE TABLE [dbo].[Soldiers]
(
	[Id] INT IDENTITY (1, 1) NOT NULL,
	[Name] nvarchar(50),
	[Rank] nvarchar(50),
	[Country] nvarchar(50),
	[TrainingInfo] nvarchar(max)
)
