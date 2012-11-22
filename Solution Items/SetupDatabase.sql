USE [master]
GO
/****** Object:  Database [Brakel-Login]    Script Date: 11/22/2012 15:13:34 ******/
CREATE DATABASE [Brakel-Login] ON  PRIMARY 
( NAME = N'Brakel-Login', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\MSSQL\DATA\Brakel-Login.mdf' , SIZE = 3072KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'Brakel-Login_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\MSSQL\DATA\Brakel-Login_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [Brakel-Login] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Brakel-Login].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Brakel-Login] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [Brakel-Login] SET ANSI_NULLS OFF
GO
ALTER DATABASE [Brakel-Login] SET ANSI_PADDING OFF
GO
ALTER DATABASE [Brakel-Login] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [Brakel-Login] SET ARITHABORT OFF
GO
ALTER DATABASE [Brakel-Login] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [Brakel-Login] SET AUTO_CREATE_STATISTICS ON
GO
ALTER DATABASE [Brakel-Login] SET AUTO_SHRINK ON
GO
ALTER DATABASE [Brakel-Login] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [Brakel-Login] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [Brakel-Login] SET CURSOR_DEFAULT  GLOBAL
GO
ALTER DATABASE [Brakel-Login] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [Brakel-Login] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [Brakel-Login] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [Brakel-Login] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [Brakel-Login] SET  DISABLE_BROKER
GO
ALTER DATABASE [Brakel-Login] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [Brakel-Login] SET DATE_CORRELATION_OPTIMIZATION ON
GO
ALTER DATABASE [Brakel-Login] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [Brakel-Login] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [Brakel-Login] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [Brakel-Login] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [Brakel-Login] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [Brakel-Login] SET  READ_WRITE
GO
ALTER DATABASE [Brakel-Login] SET RECOVERY FULL
GO
ALTER DATABASE [Brakel-Login] SET  MULTI_USER
GO
ALTER DATABASE [Brakel-Login] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [Brakel-Login] SET DB_CHAINING OFF
GO
EXEC sys.sp_db_vardecimal_storage_format N'Brakel-Login', N'ON'
GO
USE [Brakel-Login]
GO
/****** Object:  User [BrakelApplication]    Script Date: 11/22/2012 15:13:34 ******/
CREATE USER [BrakelApplication] FOR LOGIN [BrakelApplication] WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  Table [dbo].[building]    Script Date: 11/22/2012 15:13:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[building](
	[buildingId] [int] IDENTITY(1,1) NOT NULL,
	[name] [varchar](255) NOT NULL,
	[parentId] [int] NULL,
 CONSTRAINT [PK_building] PRIMARY KEY CLUSTERED 
(
	[buildingId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[userBuildingCouple]    Script Date: 11/22/2012 15:13:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[userBuildingCouple](
	[userId] [int] NOT NULL,
	[buildingId] [int] NOT NULL,
	[accessRights] [varchar](50) NOT NULL,
	[screenLayout] [text] NOT NULL,
 CONSTRAINT [PK_userBuildingRights] PRIMARY KEY CLUSTERED 
(
	[userId] ASC,
	[buildingId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[user]    Script Date: 11/22/2012 15:13:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[user](
	[userId] [int] IDENTITY(1,1) NOT NULL,
	[username] [varchar](255) NOT NULL,
	[hash] [varchar](255) NOT NULL,
	[friendlyName] [text] NOT NULL,
 CONSTRAINT [PK_user] PRIMARY KEY CLUSTERED 
(
	[userId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[token]    Script Date: 11/22/2012 15:13:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[token](
	[username] [varchar](255) NOT NULL,
	[token] [uniqueidentifier] NOT NULL,
	[createDateTime] [datetime] NOT NULL,
	[deviceId] [varchar](255) NOT NULL,
 CONSTRAINT [PK_token] PRIMARY KEY CLUSTERED 
(
	[token] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[room]    Script Date: 11/22/2012 15:13:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[room](
	[roomId] [int] IDENTITY(1,1) NOT NULL,
	[roomName] [varchar](255) NOT NULL,
	[buildingId] [int] NOT NULL,
	[xCoordinate] [int] NOT NULL,
	[yCoordinate] [int] NOT NULL,
	[width] [int] NOT NULL,
	[height] [int] NOT NULL,
	[enabled] [bit] NOT NULL,
	[hasAlarm] [bit] NOT NULL,
 CONSTRAINT [PK_room] PRIMARY KEY CLUSTERED 
(
	[roomId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  StoredProcedure [dbo].[invalidateOlderTokens]    Script Date: 11/22/2012 15:13:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Christiaan Rakowski>
-- Create date: <2012-10-12>
-- Description:	<Stored procedure to drop old login tokens>
-- =============================================
CREATE PROCEDURE [dbo].[invalidateOlderTokens]
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
    DELETE FROM [token] WHERE DATEDIFF(minute, [createDateTime], cast(getDate() as datetime)) >= 20
END
GO
/****** Object:  Default [DF__userBuild__scree__108B795B]    Script Date: 11/22/2012 15:13:35 ******/
ALTER TABLE [dbo].[userBuildingCouple] ADD  DEFAULT ('') FOR [screenLayout]
GO
/****** Object:  Default [DF_user_friendlyName]    Script Date: 11/22/2012 15:13:35 ******/
ALTER TABLE [dbo].[user] ADD  CONSTRAINT [DF_user_friendlyName]  DEFAULT ('') FOR [friendlyName]
GO
