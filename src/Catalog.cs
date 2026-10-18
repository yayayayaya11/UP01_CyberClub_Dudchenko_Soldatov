using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberClub {
 public class Field {
  public string Key,Title,Kind,Source;public int Max;
  public Field(string k,string t,string kind="text",string source="",int max=100){Key=k;Title=t;Kind=kind;Source=source;Max=max;}
 }
 public class Entity {
  public string Table,Title,Query;public Field[] Fields;
  public Entity(string table,string title,string query,params Field[] fields){Table=table;Title=title;Query=query;Fields=fields;}
  public override string ToString(){return Title;}
 }
 public static class Catalog {
  public static readonly Entity[] All={
   new Entity("Clubs","Клубы","SELECT Id,Name AS [Название],Address AS [Адрес],Phone AS [Телефон] FROM dbo.Clubs",new Field("Name","Название","text","",80),new Field("Address","Адрес","text","",180),new Field("Phone","Телефон +7XXXXXXXXXX","phone","",12)),
   new Entity("Zones","Зоны","SELECT z.Id,c.Name AS [Клуб],z.Name AS [Зона] FROM dbo.Zones z JOIN dbo.Clubs c ON c.Id=z.ClubId",new Field("ClubId","Клуб","ref","Clubs"),new Field("Name","Название зоны","text","",60)),
   new Entity("Computers","Компьютеры","SELECT c.Id,cl.Name AS [Клуб],z.Name AS [Зона],c.Number AS [Номер],c.Specifications AS [Характеристики],c.Status AS [Статус] FROM dbo.Computers c JOIN dbo.Zones z ON z.Id=c.ZoneId JOIN dbo.Clubs cl ON cl.Id=z.ClubId",new Field("ZoneId","Зона","ref","Zones"),new Field("Number","Номер","text","",20),new Field("Specifications","Характеристики","text","",200),new Field("Status","Статус","enum","Доступен|Ремонт")),
   new Entity("Tariffs","Тарифы","SELECT Id,Name AS [Тариф],HourlyRate AS [Цена за час] FROM dbo.Tariffs",new Field("Name","Название","text","",60),new Field("HourlyRate","Цена за час от 1 до 10000 руб.","money")),
   new Entity("Clients","Клиенты","SELECT Id,FullName AS [ФИО],Phone AS [Телефон],Email AS [Email] FROM dbo.Clients",new Field("FullName","ФИО","name"),new Field("Phone","Телефон +7XXXXXXXXXX","phone","",12),new Field("Email","Email необязательно","email")),
   new Entity("Bookings","Бронирования","SELECT b.Id,c.FullName AS [Клиент],cl.Name AS [Клуб],z.Name+' / '+pc.Number AS [Компьютер],t.Name AS [Тариф],b.StartsAt AS [Начало],b.EndsAt AS [Окончание],b.HourlyRate AS [Цена за час],b.Total AS [Стоимость],b.Status AS [Статус],ISNULL((SELECT SUM(p.Amount) FROM dbo.Payments p WHERE p.BookingId=b.Id),0) AS [Оплачено] FROM dbo.Bookings b JOIN dbo.Clients c ON c.Id=b.ClientId JOIN dbo.Computers pc ON pc.Id=b.ComputerId JOIN dbo.Zones z ON z.Id=pc.ZoneId JOIN dbo.Clubs cl ON cl.Id=z.ClubId JOIN dbo.Tariffs t ON t.Id=b.TariffId",new Field("ClientId","Клиент","ref","Clients"),new Field("ComputerId","Компьютер","ref","Computers"),new Field("TariffId","Тариф","ref","Tariffs"),new Field("StartsAt","Начало ДД.ММ.ГГГГ ЧЧ:ММ","date"),new Field("EndsAt","Окончание ДД.ММ.ГГГГ ЧЧ:ММ","date"),new Field("Status","Статус","enum","Забронировано|Завершено|Отменено")),
   new Entity("Payments","Платежи","SELECT p.Id,p.BookingId AS [Бронь],c.FullName AS [Клиент],cl.Name AS [Клуб],p.PaidAt AS [Дата оплаты],p.Amount AS [Сумма],p.Method AS [Способ] FROM dbo.Payments p JOIN dbo.Bookings b ON b.Id=p.BookingId JOIN dbo.Clients c ON c.Id=b.ClientId JOIN dbo.Computers pc ON pc.Id=b.ComputerId JOIN dbo.Zones z ON z.Id=pc.ZoneId JOIN dbo.Clubs cl ON cl.Id=z.ClubId",new Field("BookingId","Бронирование","ref","Bookings"),new Field("PaidAt","Дата ДД.ММ.ГГГГ ЧЧ:ММ","date"),new Field("Amount","Сумма до остатка бронирования","amount"),new Field("Method","Способ оплаты","enum","Карта|Наличные")),
   new Entity("Users","Пользователи","SELECT Id,Login AS [Логин],FullName AS [ФИО],Role AS [Роль],Photo AS [Фото] FROM dbo.Users",new Field("Login","Логин","login","",40),new Field("FullName","ФИО","name"),new Field("Role","Роль","enum","Пользователь|Администратор"),new Field("Photo","Путь к фото необязательно","optional","",260),new Field("PasswordHash","Пароль при создании или сбросе","password","",64))
  };
  public static Entity Get(string table){return All.Single(x=>x.Table==table);}
  public static Type ModelType(string table){switch(Get(table).Table){case "Users":return typeof(Models.User);case "Clients":return typeof(Models.Client);case "Clubs":return typeof(Models.Club);case "Zones":return typeof(Models.Zone);case "Computers":return typeof(Models.Computer);case "Tariffs":return typeof(Models.Tariff);case "Bookings":return typeof(Models.Booking);case "Payments":return typeof(Models.Payment);default:throw new Exception("Неизвестная сущность.");}}
  public static DataTable Lookup(string table){
   switch(table){
    case "Zones":return DataContext.Query("SELECT z.Id,c.Name+' / '+z.Name AS Label FROM dbo.Zones z JOIN dbo.Clubs c ON c.Id=z.ClubId");
    case "Computers":return DataContext.Query("SELECT p.Id,c.Name+' / '+z.Name+' / '+p.Number+' / '+p.Status AS Label FROM dbo.Computers p JOIN dbo.Zones z ON z.Id=p.ZoneId JOIN dbo.Clubs c ON c.Id=z.ClubId");
    case "Clients":return DataContext.Query("SELECT Id,FullName+' / '+Phone AS Label FROM dbo.Clients");
    case "Bookings":return DataContext.Query("SELECT b.Id,CONVERT(nvarchar,b.Id)+' / '+c.FullName+' / '+CONVERT(nvarchar,b.Total-ISNULL((SELECT SUM(Amount) FROM dbo.Payments p WHERE p.BookingId=b.Id),0))+' руб. остаток / '+b.Status AS Label FROM dbo.Bookings b JOIN dbo.Clients c ON c.Id=b.ClientId");
    default:Get(table);return DataContext.Query("SELECT Id,Name AS Label FROM dbo."+table);
   }
  }
 }
 public class ClubService {
  readonly AuthService auth;public ClubService(AuthService a){auth=a;}
  public void RequireAdmin(){if(auth.Current==null||!auth.Current.IsAdmin)throw new Exception("Действие доступно только администратору.");}
  public DataTable List(Entity e){if(auth.Current==null)throw new Exception("Выполните вход.");return DataContext.Query(e.Query+(e.Table=="Users"&&!auth.Current.IsAdmin?" WHERE Id=@p0":""),auth.Current.Id);}
  public Dictionary<string,object> Read(Entity e,int id){RequireAdmin();using(var context=DataContext.GetContext()){var entity=context.Set(Catalog.ModelType(e.Table)).Find(id);if(entity==null)throw new Exception("Запись уже удалена.");var values=context.Entry(entity).CurrentValues;return values.PropertyNames.ToDictionary(name=>name,name=>values[name]??DBNull.Value);}}
  public static object Parse(Field f,string text){
   text=(text??"").Trim();if((f.Kind=="optional"||f.Kind=="email")&&text=="")return DBNull.Value;
   if(text=="")throw new Exception("Заполните поле «"+f.Title+"».");
   if(f.Kind=="date"){DateTime d;if(!DateTime.TryParseExact(text,"dd.MM.yyyy HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out d)||d.Year<2000||d.Year>2100)throw new Exception("Дата: ДД.ММ.ГГГГ ЧЧ:ММ, год 2000–2100.");return d;}
   if(f.Kind=="money"||f.Kind=="amount"){decimal n;if(!decimal.TryParse(text.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out n)||n<=0||n>(f.Kind=="money"?10000:240000)||decimal.Round(n,2)!=n||(f.Kind=="money"&&n<1))throw new Exception("Некорректная сумма. Не более двух знаков после запятой; цена 1–10000, платёж до 240000 руб.");return n;}
   if(text.Length>f.Max)throw new Exception("Слишком длинное значение: "+f.Title);
   if(f.Kind=="phone"&&!Regex.IsMatch(text,@"\A\+7\d{10}\z"))throw new Exception("Телефон должен иметь вид +79001234567.");
   if(f.Kind=="email"&&!Regex.IsMatch(text,@"\A[^\s@]+@[^\s@]+\.[^\s@]+\z"))throw new Exception("Некорректный email.");
   if(f.Kind=="name"&&!Regex.IsMatch(text,@"\A[\p{L} .'-]{3,100}\z"))throw new Exception("ФИО должно содержать буквы и иметь длину 3–100 символов.");
   if(f.Kind=="login"&&!Regex.IsMatch(text,@"\A[A-Za-z][A-Za-z0-9_.]{2,39}\z"))throw new Exception("Некорректный логин. Допустимы латиница, цифры, точка и подчёркивание.");
   if(f.Kind=="enum"&&!f.Source.Split('|').Contains(text))throw new Exception("Выберите значение из списка.");
   if(f.Kind=="ref"){int n;if(!Int32.TryParse(text,out n)||n<=0)throw new Exception("Выберите «"+f.Title+"».");return n;}
   if((f.Key=="Name"||f.Key=="Address")&&text.Length<2)throw new Exception("Значение должно содержать минимум два символа.");
   return text;
  }
  public int Save(Entity e,int? id,Dictionary<string,object> v){
   RequireAdmin();Catalog.Get(e.Table);
   if(e.Table=="Users"){
    string pw=Convert.ToString(v["PasswordHash"]);if(pw!=""){Passwords.Validate(pw);v["PasswordHash"]=Passwords.Hash(pw);}else if(id.HasValue)v.Remove("PasswordHash");else throw new Exception("Укажите пароль.");
    if(id==auth.Current.Id&&Convert.ToString(v["Role"])!="Администратор")throw new Exception("Нельзя изменить собственную роль.");
   }
   if(e.Table=="Bookings"){
    var start=(DateTime)v["StartsAt"];var end=(DateTime)v["EndsAt"];if(end<=start||(end-start).TotalHours>24)throw new Exception("Продолжительность бронирования: от 1 минуты до 24 часов.");
    if(!id.HasValue||Convert.ToInt32(DataContext.Scalar("SELECT TariffId FROM dbo.Bookings WHERE Id=@p0",id.Value))!=Convert.ToInt32(v["TariffId"]))
     v["HourlyRate"]=DataContext.Scalar("SELECT HourlyRate FROM dbo.Tariffs WHERE Id=@p0",v["TariffId"]);
   }
   using(var context=DataContext.GetContext()){
    var type=Catalog.ModelType(e.Table);var set=context.Set(type);object entity=id.HasValue?set.Find(id.Value):Activator.CreateInstance(type);
    if(entity==null)throw new Exception("Запись уже удалена.");
    foreach(var pair in v){var property=type.GetProperty(pair.Key);if(property==null)throw new Exception("Неизвестное поле.");property.SetValue(entity,pair.Value==DBNull.Value?null:pair.Value,null);}
    if(!id.HasValue)set.Add(entity);context.SaveChanges();return (int)type.GetProperty("Id").GetValue(entity,null);
   }
  }
  public void Delete(Entity e,int id){RequireAdmin();Catalog.Get(e.Table);if(e.Table=="Users"&&id==auth.Current.Id)throw new Exception("Нельзя удалить собственную учётную запись.");using(var context=DataContext.GetContext()){var set=context.Set(Catalog.ModelType(e.Table));var entity=set.Find(id);if(entity==null)throw new Exception("Запись уже удалена.");set.Remove(entity);context.SaveChanges();}}
  public DataTable Revenue(int? club,DateTime from,DateTime to){RequireAdmin();return DataContext.Query("SELECT c.Name AS [Клуб],ISNULL(SUM(CASE WHEN p.PaidAt>=@p0 AND p.PaidAt<@p1 THEN p.Amount ELSE 0 END),0) AS [Выручка] FROM dbo.Clubs c LEFT JOIN dbo.Zones z ON z.ClubId=c.Id LEFT JOIN dbo.Computers pc ON pc.ZoneId=z.Id LEFT JOIN dbo.Bookings b ON b.ComputerId=pc.Id LEFT JOIN dbo.Payments p ON p.BookingId=b.Id WHERE (@p2 IS NULL OR c.Id=@p2) GROUP BY c.Id,c.Name ORDER BY c.Name",from,to,(object)club??DBNull.Value);}
 }
}
