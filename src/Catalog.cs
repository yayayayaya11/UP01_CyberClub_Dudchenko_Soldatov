using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberClub
{
    public static class Catalog
    {
        public static readonly Entity[] All =
        {
            new Entity("Clubs", "Клубы", "SELECT Id,Name AS [Название],Address AS [Адрес],Phone AS [Телефон] FROM dbo.Clubs", new Field("Name", "Название", "text", "", 80), new Field("Address", "Адрес", "text", "", 180), new Field("Phone", "Телефон +7XXXXXXXXXX", "phone", "", 12)),
            new Entity("Zones", "Зоны", "SELECT z.Id,c.Name AS [Клуб],z.Name AS [Зона] FROM dbo.Zones z JOIN dbo.Clubs c ON c.Id=z.ClubId", new Field("ClubId", "Клуб", "ref", "Clubs"), new Field("Name", "Название зоны", "text", "", 60)),
            new Entity("Computers", "Компьютеры", "SELECT c.Id,cl.Name AS [Клуб],z.Name AS [Зона],c.Number AS [Номер],c.Specifications AS [Характеристики],c.Status AS [Статус] FROM dbo.Computers c JOIN dbo.Zones z ON z.Id=c.ZoneId JOIN dbo.Clubs cl ON cl.Id=z.ClubId", new Field("ZoneId", "Зона", "ref", "Zones"), new Field("Number", "Номер", "text", "", 20), new Field("Specifications", "Характеристики", "text", "", 200), new Field("Status", "Статус", "enum", "Доступен|Ремонт")),
            new Entity("Tariffs", "Тарифы", "SELECT Id,Name AS [Тариф],HourlyRate AS [Цена за час] FROM dbo.Tariffs", new Field("Name", "Название", "text", "", 60), new Field("HourlyRate", "Цена за час от 1 до 10000 руб.", "money")),
            new Entity("Clients", "Клиенты", "SELECT Id,FullName AS [ФИО],Phone AS [Телефон],Email AS [Email] FROM dbo.Clients", new Field("FullName", "ФИО", "name"), new Field("Phone", "Телефон +7XXXXXXXXXX", "phone", "", 12), new Field("Email", "Email необязательно", "email")),
            new Entity("Bookings", "Бронирования", "SELECT b.Id,c.FullName AS [Клиент],cl.Name AS [Клуб],z.Name+' / '+pc.Number AS [Компьютер],t.Name AS [Тариф],b.StartsAt AS [Начало],b.EndsAt AS [Окончание],b.HourlyRate AS [Цена за час],b.Total AS [Стоимость],b.Status AS [Статус],ISNULL((SELECT SUM(p.Amount) FROM dbo.Payments p WHERE p.BookingId=b.Id),0) AS [Оплачено] FROM dbo.Bookings b JOIN dbo.Clients c ON c.Id=b.ClientId JOIN dbo.Computers pc ON pc.Id=b.ComputerId JOIN dbo.Zones z ON z.Id=pc.ZoneId JOIN dbo.Clubs cl ON cl.Id=z.ClubId JOIN dbo.Tariffs t ON t.Id=b.TariffId", new Field("ClientId", "Клиент", "ref", "Clients"), new Field("ComputerId", "Компьютер", "ref", "Computers"), new Field("TariffId", "Тариф", "ref", "Tariffs"), new Field("StartsAt", "Начало ДД.ММ.ГГГГ ЧЧ:ММ", "date"), new Field("EndsAt", "Окончание ДД.ММ.ГГГГ ЧЧ:ММ", "date"), new Field("Status", "Статус", "enum", "Забронировано|Завершено|Отменено")),
            new Entity("Payments", "Платежи", "SELECT p.Id,p.BookingId AS [Бронь],c.FullName AS [Клиент],cl.Name AS [Клуб],p.PaidAt AS [Дата оплаты],p.Amount AS [Сумма],p.Method AS [Способ] FROM dbo.Payments p JOIN dbo.Bookings b ON b.Id=p.BookingId JOIN dbo.Clients c ON c.Id=b.ClientId JOIN dbo.Computers pc ON pc.Id=b.ComputerId JOIN dbo.Zones z ON z.Id=pc.ZoneId JOIN dbo.Clubs cl ON cl.Id=z.ClubId", new Field("BookingId", "Бронирование", "ref", "Bookings"), new Field("PaidAt", "Дата ДД.ММ.ГГГГ ЧЧ:ММ", "date"), new Field("Amount", "Сумма до остатка бронирования", "amount"), new Field("Method", "Способ оплаты", "enum", "Карта|Наличные")),
            new Entity("Users", "Пользователи", "SELECT Id,Login AS [Логин],FullName AS [ФИО],Role AS [Роль],Photo AS [Фото] FROM dbo.Users", new Field("Login", "Логин", "login", "", 40), new Field("FullName", "ФИО", "name"), new Field("Role", "Роль", "enum", "Пользователь|Администратор"), new Field("Photo", "Путь к фото необязательно", "optional", "", 260), new Field("PasswordHash", "Пароль при создании или сбросе", "password", "", 64))
        };
        public static Entity Get(string table)
        {
            return All.Single(x => x.Table == table);
        }

        public static Type ModelType(string table)
        {
            switch (Get(table).Table)
            {
                case "Users":
                    return typeof(Models.User);
                case "Clients":
                    return typeof(Models.Client);
                case "Clubs":
                    return typeof(Models.Club);
                case "Zones":
                    return typeof(Models.Zone);
                case "Computers":
                    return typeof(Models.Computer);
                case "Tariffs":
                    return typeof(Models.Tariff);
                case "Bookings":
                    return typeof(Models.Booking);
                case "Payments":
                    return typeof(Models.Payment);
                default:
                    throw new Exception("Неизвестная сущность.");
            }
        }

        public static DataTable Lookup(string table)
        {
            switch (table)
            {
                case "Zones":
                    return Core.Query("SELECT z.Id,c.Name+' / '+z.Name AS Label FROM dbo.Zones z JOIN dbo.Clubs c ON c.Id=z.ClubId");
                case "Computers":
                    return Core.Query("SELECT p.Id,c.Name+' / '+z.Name+' / '+p.Number+' / '+p.Status AS Label FROM dbo.Computers p JOIN dbo.Zones z ON z.Id=p.ZoneId JOIN dbo.Clubs c ON c.Id=z.ClubId");
                case "Clients":
                    return Core.Query("SELECT Id,FullName+' / '+Phone AS Label FROM dbo.Clients");
                case "Bookings":
                    return Core.Query("SELECT b.Id,CONVERT(nvarchar,b.Id)+' / '+c.FullName+' / '+CONVERT(nvarchar,b.Total-ISNULL((SELECT SUM(Amount) FROM dbo.Payments p WHERE p.BookingId=b.Id),0))+' руб. остаток / '+b.Status AS Label FROM dbo.Bookings b JOIN dbo.Clients c ON c.Id=b.ClientId");
                default:
                    Get(table);
                    return Core.Query("SELECT Id,Name AS Label FROM dbo." + table);
            }
        }
    }
}
