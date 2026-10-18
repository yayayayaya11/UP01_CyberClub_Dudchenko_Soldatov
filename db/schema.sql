-- SQL Server 2016+ / LocalDB. New independent academic database.
USE master;
GO
IF DB_ID(N'DB_Dudchenko_Soldatov_CyberClub') IS NULL
    CREATE DATABASE DB_Dudchenko_Soldatov_CyberClub;
GO
USE DB_Dudchenko_Soldatov_CyberClub;
GO
SET XACT_ABORT ON;
IF OBJECT_ID('dbo.Users') IS NOT NULL
    THROW 51000, N'Таблицы уже существуют. Повторное создание остановлено без изменения данных.', 1;
GO
BEGIN TRANSACTION;
CREATE TABLE dbo.Users (
 Id int IDENTITY PRIMARY KEY,
 Login nvarchar(40) NOT NULL UNIQUE,
 PasswordHash nvarchar(200) NOT NULL,
 FullName nvarchar(100) NOT NULL,
 Role nvarchar(20) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN (N'Администратор',N'Пользователь')),
 Photo nvarchar(260) NULL,
 CONSTRAINT CK_Users_Name CHECK (LEN(FullName)>=3)
);
CREATE TABLE dbo.Clients (
 Id int IDENTITY PRIMARY KEY,
 FullName nvarchar(100) NOT NULL,
 Phone nvarchar(12) NOT NULL UNIQUE,
 Email nvarchar(100) NULL,
 CONSTRAINT CK_Clients_Name CHECK(LEN(FullName)>=3),
 CONSTRAINT CK_Clients_Phone CHECK(LEN(Phone)=12 AND LEFT(Phone,2)='+7' AND SUBSTRING(Phone,3,10) NOT LIKE '%[^0-9]%')
);
CREATE TABLE dbo.Clubs (
 Id int IDENTITY PRIMARY KEY,
 Name nvarchar(80) NOT NULL UNIQUE,
 Address nvarchar(180) NOT NULL,
 Phone nvarchar(12) NOT NULL,
 CONSTRAINT CK_Clubs_Name CHECK(LEN(Name)>=2),
 CONSTRAINT CK_Clubs_Phone CHECK(LEN(Phone)=12 AND LEFT(Phone,2)='+7' AND SUBSTRING(Phone,3,10) NOT LIKE '%[^0-9]%')
);
CREATE TABLE dbo.Zones (
 Id int IDENTITY PRIMARY KEY,
 ClubId int NOT NULL REFERENCES dbo.Clubs(Id) ON DELETE CASCADE,
 Name nvarchar(60) NOT NULL,
 CONSTRAINT UQ_Zones UNIQUE(ClubId,Name)
);
CREATE TABLE dbo.Computers (
 Id int IDENTITY PRIMARY KEY,
 ZoneId int NOT NULL REFERENCES dbo.Zones(Id) ON DELETE CASCADE,
 Number nvarchar(20) NOT NULL,
 Specifications nvarchar(200) NOT NULL,
 Status nvarchar(20) NOT NULL CHECK(Status IN(N'Доступен',N'Ремонт')),
 CONSTRAINT UQ_Computers UNIQUE(ZoneId,Number)
);
CREATE TABLE dbo.Tariffs (
 Id int IDENTITY PRIMARY KEY,
 Name nvarchar(60) NOT NULL UNIQUE,
 HourlyRate decimal(10,2) NOT NULL CHECK(HourlyRate BETWEEN 1 AND 10000)
);
CREATE TABLE dbo.Bookings (
 Id int IDENTITY PRIMARY KEY,
 ClientId int NOT NULL REFERENCES dbo.Clients(Id) ON DELETE CASCADE,
 ComputerId int NOT NULL REFERENCES dbo.Computers(Id) ON DELETE CASCADE,
 TariffId int NOT NULL REFERENCES dbo.Tariffs(Id) ON DELETE CASCADE,
 StartsAt datetime2(0) NOT NULL,
 EndsAt datetime2(0) NOT NULL,
 HourlyRate decimal(10,2) NOT NULL CHECK(HourlyRate BETWEEN 1 AND 10000),
 Total AS CONVERT(decimal(12,2),ROUND(DATEDIFF(MINUTE,StartsAt,EndsAt)/60.0*HourlyRate,2)) PERSISTED,
 Status nvarchar(20) NOT NULL CHECK(Status IN(N'Забронировано',N'Завершено',N'Отменено')),
 CONSTRAINT CK_Bookings_Time CHECK(EndsAt>StartsAt AND DATEDIFF(MINUTE,StartsAt,EndsAt) BETWEEN 1 AND 1440 AND DATEPART(SECOND,StartsAt)=0 AND DATEPART(SECOND,EndsAt)=0)
);
CREATE INDEX IX_Bookings_ComputerTime ON dbo.Bookings(ComputerId,StartsAt,EndsAt) INCLUDE(Status);
CREATE INDEX IX_Bookings_Client ON dbo.Bookings(ClientId);
CREATE TABLE dbo.Payments (
 Id int IDENTITY PRIMARY KEY,
 BookingId int NOT NULL REFERENCES dbo.Bookings(Id) ON DELETE CASCADE,
 PaidAt datetime2(0) NOT NULL,
 Amount decimal(12,2) NOT NULL CHECK(Amount>0 AND Amount<=240000),
 Method nvarchar(20) NOT NULL CHECK(Method IN(N'Карта',N'Наличные'))
);
CREATE INDEX IX_Payments_Booking ON dbo.Payments(BookingId);
COMMIT;
GO
CREATE TRIGGER dbo.TR_Bookings_Validate ON dbo.Bookings AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 -- Serializable range locks protect against simultaneous overlapping inserts.
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.Bookings b WITH(UPDLOCK,HOLDLOCK)
 ON b.ComputerId=i.ComputerId AND b.Id<>i.Id AND i.StartsAt<b.EndsAt AND b.StartsAt<i.EndsAt
 WHERE b.Status<>N'Отменено' AND i.Status<>N'Отменено')
 THROW 51001,N'Компьютер уже забронирован на это время.',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.Computers c ON c.Id=i.ComputerId WHERE c.Status=N'Ремонт' AND i.Status=N'Забронировано')
 THROW 51002,N'Нельзя бронировать компьютер в ремонте.',1;
 IF EXISTS(SELECT 1 FROM inserted i WHERE (SELECT ISNULL(SUM(Amount),0) FROM dbo.Payments p WHERE p.BookingId=i.Id)>i.Total
 OR (i.Status=N'Отменено' AND EXISTS(SELECT 1 FROM dbo.Payments p WHERE p.BookingId=i.Id)))
 THROW 51003,N'Бронирование оплачено: нельзя отменить его или уменьшить сумму ниже оплаты.',1;
END;
GO
CREATE TRIGGER dbo.TR_Payments_Validate ON dbo.Payments AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.Bookings b WITH(UPDLOCK,HOLDLOCK)
 WHERE b.Id IN(SELECT BookingId FROM inserted) AND
 (b.Status=N'Отменено' OR (SELECT SUM(p.Amount) FROM dbo.Payments p WHERE p.BookingId=b.Id)>b.Total))
 THROW 51004,N'Платёж превышает остаток либо бронирование отменено.',1;
END;
GO
CREATE TRIGGER dbo.TR_Users_LastAdmin ON dbo.Users AFTER DELETE, UPDATE AS
BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Role=N'Администратор')
 THROW 51005,N'Необходимо оставить хотя бы одного администратора.',1;
END;
GO
