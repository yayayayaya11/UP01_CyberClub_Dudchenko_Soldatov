using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberClub
{
    public class ClubService
    {
        readonly AuthService auth;
        public ClubService(AuthService a)
        {
            auth = a;
        }

        public void RequireAdmin()
        {
            if (auth.Current == null || !auth.Current.IsAdmin)
                throw new Exception("Действие доступно только администратору.");
        }

        public DataTable List(Entity e)
        {
            if (auth.Current == null)
                throw new Exception("Выполните вход.");
            return Core.Query(e.Query + (e.Table == "Users" && !auth.Current.IsAdmin ? " WHERE Id=@p0" : ""), auth.Current.Id);
        }

        public Dictionary<string, object> Read(Entity e, int id)
        {
            RequireAdmin();
            {
                var context = Core.Context;
                var entity = context.Set(Catalog.ModelType(e.Table)).Find(id);
                if (entity == null)
                    throw new Exception("Запись уже удалена.");
                var values = context.Entry(entity).CurrentValues;
                return values.PropertyNames.ToDictionary(name => name, name => values[name] ?? DBNull.Value);
            }
        }

        public static object Parse(Field f, string text)
        {
            text = (text ?? "").Trim();
            if ((f.Kind == "optional" || f.Kind == "email") && text == "")
                return DBNull.Value;
            if (text == "")
                throw new Exception("Заполните поле «" + f.Title + "».");
            if (f.Kind == "date")
            {
                DateTime d;
                if (!DateTime.TryParseExact(text, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out d) || d.Year < 2000 || d.Year > 2100)
                    throw new Exception("Дата: ДД.ММ.ГГГГ ЧЧ:ММ, год 2000–2100.");
                return d;
            }

            if (f.Kind == "money" || f.Kind == "amount")
            {
                decimal n;
                if (!decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out n) || n <= 0 || n > (f.Kind == "money" ? 10000 : 240000) || decimal.Round(n, 2) != n || (f.Kind == "money" && n < 1))
                    throw new Exception("Некорректная сумма. Не более двух знаков после запятой; цена 1–10000, платёж до 240000 руб.");
                return n;
            }

            if (text.Length > f.Max)
                throw new Exception("Слишком длинное значение: " + f.Title);
            if (f.Kind == "phone" && !Regex.IsMatch(text, @"\A\+7\d{10}\z"))
                throw new Exception("Телефон должен иметь вид +79001234567.");
            if (f.Kind == "email" && !Regex.IsMatch(text, @"\A[^\s@]+@[^\s@]+\.[^\s@]+\z"))
                throw new Exception("Некорректный email.");
            if (f.Kind == "name" && !Regex.IsMatch(text, @"\A[\p{L} .'-]{3,100}\z"))
                throw new Exception("ФИО должно содержать буквы и иметь длину 3–100 символов.");
            if (f.Kind == "login" && !Regex.IsMatch(text, @"\A[A-Za-z][A-Za-z0-9_.]{2,39}\z"))
                throw new Exception("Некорректный логин. Допустимы латиница, цифры, точка и подчёркивание.");
            if (f.Kind == "enum" && !f.Source.Split('|').Contains(text))
                throw new Exception("Выберите значение из списка.");
            if (f.Kind == "ref")
            {
                int n;
                if (!Int32.TryParse(text, out n) || n <= 0)
                    throw new Exception("Выберите «" + f.Title + "».");
                return n;
            }

            if ((f.Key == "Name" || f.Key == "Address") && text.Length < 2)
                throw new Exception("Значение должно содержать минимум два символа.");
            return text;
        }

        public int Save(Entity e, int? id, Dictionary<string, object> v)
        {
            RequireAdmin();
            Catalog.Get(e.Table);
            if (e.Table == "Users")
            {
                string pw = Convert.ToString(v["PasswordHash"]);
                if (pw != "")
                {
                    Passwords.Validate(pw);
                    v["PasswordHash"] = Passwords.Hash(pw);
                }
                else if (id.HasValue)
                    v.Remove("PasswordHash");
                else
                    throw new Exception("Укажите пароль.");
                if (id == auth.Current.Id && Convert.ToString(v["Role"]) != "Администратор")
                    throw new Exception("Нельзя изменить собственную роль.");
            }

            if (e.Table == "Bookings")
            {
                var start = (DateTime)v["StartsAt"];
                var end = (DateTime)v["EndsAt"];
                if (end <= start || (end - start).TotalHours > 24)
                    throw new Exception("Продолжительность бронирования: от 1 минуты до 24 часов.");
                if (!id.HasValue || Convert.ToInt32(Core.Scalar("SELECT TariffId FROM dbo.Bookings WHERE Id=@p0", id.Value)) != Convert.ToInt32(v["TariffId"]))
                    v["HourlyRate"] = Core.Scalar("SELECT HourlyRate FROM dbo.Tariffs WHERE Id=@p0", v["TariffId"]);
            }

            {
                var context = Core.Context;
                var type = Catalog.ModelType(e.Table);
                var set = context.Set(type);
                object entity = id.HasValue ? set.Find(id.Value) : Activator.CreateInstance(type);
                if (entity == null)
                    throw new Exception("Запись уже удалена.");
                foreach (var pair in v)
                {
                    var property = type.GetProperty(pair.Key);
                    if (property == null)
                        throw new Exception("Неизвестное поле.");
                    property.SetValue(entity, pair.Value == DBNull.Value ? null : pair.Value, null);
                }

                if (!id.HasValue)
                    set.Add(entity);
                Core.SaveChanges();
                return (int)type.GetProperty("Id").GetValue(entity, null);
            }
        }

        public void Delete(Entity e, int id)
        {
            RequireAdmin();
            Catalog.Get(e.Table);
            if (e.Table == "Users" && id == auth.Current.Id)
                throw new Exception("Нельзя удалить собственную учётную запись.");
            {
                var context = Core.Context;
                var set = context.Set(Catalog.ModelType(e.Table));
                var entity = set.Find(id);
                if (entity == null)
                    throw new Exception("Запись уже удалена.");
                set.Remove(entity);
                Core.SaveChanges();
            }
        }

        // выручка рассчитывается по фактическим платежам за выбранный период
        public DataTable Revenue(int? club, DateTime from, DateTime to)
        {
            RequireAdmin();
            return Core.Query("SELECT c.Name AS [Клуб],ISNULL(SUM(CASE WHEN p.PaidAt>=@p0 AND p.PaidAt<@p1 THEN p.Amount ELSE 0 END),0) AS [Выручка] FROM dbo.Clubs c LEFT JOIN dbo.Zones z ON z.ClubId=c.Id LEFT JOIN dbo.Computers pc ON pc.ZoneId=z.Id LEFT JOIN dbo.Bookings b ON b.ComputerId=pc.Id LEFT JOIN dbo.Payments p ON p.BookingId=b.Id WHERE (@p2 IS NULL OR c.Id=@p2) GROUP BY c.Id,c.Name ORDER BY c.Name", from, to, (object)club ?? DBNull.Value);
        }
    }
}
