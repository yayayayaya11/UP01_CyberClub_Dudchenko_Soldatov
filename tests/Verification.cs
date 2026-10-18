using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CyberClub {
 public static class VerificationEntry {
  static StringBuilder log=new StringBuilder();static int passed,failed;static string root=AppDomain.CurrentDomain.BaseDirectory;
  static void Check(string name,Action test){try{test();passed++;log.AppendLine("PASS | "+name);}catch(Exception ex){failed++;log.AppendLine("FAIL | "+name+" | "+ex);}}
  static void Assert(bool test,string error){if(!test)throw new Exception(error);}
  static void Reject(Action a){bool rejected=false;try{a();}catch{rejected=true;}Assert(rejected,"Недопустимая операция была принята");}
  static Dictionary<string,object> V(params object[] pairs){var d=new Dictionary<string,object>();for(int i=0;i<pairs.Length;i+=2)d.Add((string)pairs[i],pairs[i+1]);return d;}
  static int Save(ClubService s,string table,params object[] pairs){return s.Save(Catalog.Get(table),null,V(pairs));}
  [STAThread] public static void Main(){
   Directory.SetCurrentDirectory(root);Directory.CreateDirectory(Path.Combine(root,"tests","results"));
   string original=DataContext.ConnectionString,db=DataContext.Database+"_Verify_"+DateTime.Now.ToString("yyyyMMddHHmmss");
   try{
    var master=new SqlConnectionStringBuilder(original);master.InitialCatalog="master";
    using(var c=new SqlConnection(master.ToString())){c.Open();string script=File.ReadAllText(Path.Combine(root,"db","install.sql")).Replace(DataContext.Database,db);foreach(string batch in Regex.Split(script,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))if(!String.IsNullOrWhiteSpace(batch))using(var cmd=new SqlCommand(batch,c)){cmd.CommandTimeout=60;cmd.ExecuteNonQuery();}}
    var cs=new SqlConnectionStringBuilder(original);cs.InitialCatalog=db;DataContext.ConnectionString=cs.ToString();
    var a=new AuthService();var service=new ClubService(a);
    Check("EDMX: восемь сущностей, таблиц и шесть каскадных связей",()=>{using(var context=DataContext.GetContext()){var metadata=((System.Data.Entity.Infrastructure.IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;Assert(metadata.GetItems<System.Data.Entity.Core.Metadata.Edm.EntityType>(System.Data.Entity.Core.Metadata.Edm.DataSpace.CSpace).Count==8,"Сущности EDMX");Assert(context.Clubs.AsNoTracking().Count()==3,"LINQ через EDMX");}Assert(Convert.ToInt32(DataContext.Scalar("SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0"))==8,"Число таблиц");Assert(Convert.ToInt32(DataContext.Scalar("SELECT COUNT(*) FROM sys.foreign_keys WHERE delete_referential_action=1"))==6,"Каскадные связи");});
    Check("PBKDF2: соль и проверка пароля",()=>{string x=Passwords.Hash("Test123");Assert(Passwords.Verify("Test123",x),"Правильный пароль");Assert(!Passwords.Verify("wrong",x),"Неверный пароль");Assert(x!=Passwords.Hash("Test123"),"Соль должна различаться");});
    Check("Капча после трёх ошибок и обязательная проверка",()=>{for(int i=0;i<3;i++)Reject(()=>a.Login("admin","wrong",""));Assert(a.NeedsCaptcha,"Капча не появилась");Reject(()=>a.Login("admin","CyberAdmin1","wrong"));a.Login("admin","CyberAdmin1",a.Captcha);Assert(a.Current.IsAdmin&&!a.NeedsCaptcha,"Вход с капчей");});
    Check("Регистрация, дубликаты, пользователь не получает роль администратора",()=>{a.Register("newuser","Новый Пользователь","Test123","Test123");Reject(()=>a.Register("newuser","Новый Пользователь","Test123","Test123"));a.Logout();a.Login("newuser","Test123","");Assert(!a.Current.IsAdmin,"Роль");Reject(()=>service.Delete(Catalog.Get("Clubs"),1));Reject(()=>service.Revenue(null,new DateTime(2026,1,1),new DateTime(2027,1,1)));});
    Check("Смена пароля и отклонение старого пароля",()=>{a.ChangePassword("newuser","Test123","New1234","New1234");a.Logout();Reject(()=>a.Login("newuser","Test123",""));a.Login("newuser","New1234","");});
    Check("Формат ФИО, телефона, email, сумм и дат",()=>{Reject(()=>ClubService.Parse(new Field("Phone","Телефон","phone"),"123"));Reject(()=>ClubService.Parse(new Field("FullName","ФИО","name"),"12345"));Reject(()=>ClubService.Parse(new Field("Email","Email","email"),"wrong"));Reject(()=>ClubService.Parse(new Field("Amount","Сумма","amount"),"-1"));Reject(()=>ClubService.Parse(new Field("HourlyRate","Цена","money"),"10001"));Reject(()=>ClubService.Parse(new Field("StartsAt","Дата","date"),"31.02.2026 10:00"));Reject(()=>Passwords.Validate("абв123"));});
    a.Logout();a.Login("admin","CyberAdmin1",a.Captcha);int club=0,zone=0,pc=0,tariff=0,client=0,booking=0,payment=0,user=0;
    Check("CRUD клубов",()=>{club=Save(service,"Clubs","Name","Тестовый клуб","Address","Москва, Тестовая, 1","Phone","+79009999991");service.Save(Catalog.Get("Clubs"),club,V("Name","Тестовый клуб 2","Address","Москва, Тестовая, 2","Phone","+79009999991"));Assert(service.List(Catalog.Get("Clubs")).Rows.Count==4,"Добавление");});
    Check("CRUD зон",()=>{zone=Save(service,"Zones","ClubId",club,"Name","Тестовая зона");service.Save(Catalog.Get("Zones"),zone,V("ClubId",club,"Name","Зона команды"));Assert((string)service.Read(Catalog.Get("Zones"),zone)["Name"]=="Зона команды","Изменение");});
    Check("CRUD компьютеров",()=>{pc=Save(service,"Computers","ZoneId",zone,"Number","TEST-1","Specifications","RTX 4060","Status","Доступен");service.Save(Catalog.Get("Computers"),pc,V("ZoneId",zone,"Number","TEST-2","Specifications","RTX 4070","Status","Доступен"));Assert((string)service.Read(Catalog.Get("Computers"),pc)["Number"]=="TEST-2","Изменение");});
    Check("CRUD тарифов",()=>{tariff=Save(service,"Tariffs","Name","Тестовый","HourlyRate",150m);service.Save(Catalog.Get("Tariffs"),tariff,V("Name","Тестовый 2","HourlyRate",180m));Assert((decimal)service.Read(Catalog.Get("Tariffs"),tariff)["HourlyRate"]==180m,"Изменение");});
    Check("CRUD клиентов",()=>{client=Save(service,"Clients","FullName","Тестовый Клиент","Phone","+79009999992","Email","test@example.test");service.Save(Catalog.Get("Clients"),client,V("FullName","Тестовый Клиент Второй","Phone","+79009999992","Email",DBNull.Value));Assert((string)service.Read(Catalog.Get("Clients"),client)["FullName"]=="Тестовый Клиент Второй","Изменение");});
    DateTime start=new DateTime(2026,11,1,12,0,0);
    Check("CRUD бронирований и расчёт по минутам",()=>{booking=Save(service,"Bookings","ClientId",client,"ComputerId",pc,"TariffId",tariff,"StartsAt",start,"EndsAt",start.AddHours(2),"Status","Забронировано");Assert((decimal)service.Read(Catalog.Get("Bookings"),booking)["Total"]==360m,"Стоимость");service.Save(Catalog.Get("Bookings"),booking,V("ClientId",client,"ComputerId",pc,"TariffId",tariff,"StartsAt",start,"EndsAt",start.AddMinutes(90),"Status","Забронировано"));Assert((decimal)service.Read(Catalog.Get("Bookings"),booking)["Total"]==270m,"Перерасчёт");});
    Check("Нельзя пересекать бронирования или бронировать ремонт",()=>{Reject(()=>Save(service,"Bookings","ClientId",client,"ComputerId",pc,"TariffId",tariff,"StartsAt",start.AddMinutes(10),"EndsAt",start.AddHours(1),"Status","Забронировано"));Reject(()=>Save(service,"Bookings","ClientId",client,"ComputerId",18,"TariffId",tariff,"StartsAt",start,"EndsAt",start.AddHours(1),"Status","Забронировано"));});
    Check("CRUD платежей, защита от переплаты и отмены оплаченной брони",()=>{payment=Save(service,"Payments","BookingId",booking,"PaidAt",start,"Amount",100m,"Method","Карта");service.Save(Catalog.Get("Payments"),payment,V("BookingId",booking,"PaidAt",start,"Amount",150m,"Method","Наличные"));Reject(()=>Save(service,"Payments","BookingId",booking,"PaidAt",start,"Amount",200m,"Method","Карта"));Reject(()=>service.Save(Catalog.Get("Bookings"),booking,V("ClientId",client,"ComputerId",pc,"TariffId",tariff,"StartsAt",start,"EndsAt",start.AddMinutes(90),"Status","Отменено")));});
    Check("CRUD пользователей и защита собственной роли",()=>{user=Save(service,"Users","Login","manager","FullName","Тестовый Администратор","Role","Администратор","Photo",DBNull.Value,"PasswordHash","Test123");service.Save(Catalog.Get("Users"),user,V("Login","manager","FullName","Изменённый Администратор","Role","Пользователь","Photo",DBNull.Value,"PasswordHash",""));Reject(()=>service.Delete(Catalog.Get("Users"),a.Current.Id));Reject(()=>service.Save(Catalog.Get("Users"),a.Current.Id,V("Role","Пользователь","PasswordHash","")));});
    Check("Диаграмма считает фактические оплаты",()=>{var t=service.Revenue(club,new DateTime(2026,1,1),new DateTime(2027,1,1));Assert(t.Rows.Count==1&&Convert.ToDecimal(t.Rows[0][1])==150m,"Выручка");});
    Check("Каскад клуб → зоны → компьютеры → бронирования → платежи",()=>{service.Delete(Catalog.Get("Clubs"),club);foreach(var pair in new[]{new{T="Zones",Id=zone},new{T="Computers",Id=pc},new{T="Bookings",Id=booking},new{T="Payments",Id=payment}})Assert(Convert.ToInt32(DataContext.Scalar("SELECT COUNT(*) FROM dbo."+pair.T+" WHERE Id=@p0",pair.Id))==0,"Не удалена "+pair.T);service.Delete(Catalog.Get("Tariffs"),tariff);service.Delete(Catalog.Get("Clients"),client);service.Delete(Catalog.Get("Users"),user);});
    Check("Просмотр и поиск таблиц для обеих ролей",()=>{foreach(var e in Catalog.All)Assert(service.List(e).Rows.Count>0,"Пустая таблица "+e.Table);a.Logout();a.Login("user","CyberUser1","");foreach(var e in Catalog.All)Assert(service.List(e).Rows.Count>0,"Просмотр "+e.Table);Assert(service.List(Catalog.Get("Users")).Rows.Count==1,"Ограничение пользователей");});
    DataContext.ConnectionString=original;
    var app=new Application();var window=new MainWindow{EvidenceMode=true};app.MainWindow=window;
    window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>{
     try{
      window.Snapshot("01_login");window.ShowRegistration();window.Snapshot("02_registration");window.ShowPassword();window.Snapshot("03_password");
      for(int i=0;i<3;i++)try{window.Auth.Login("admin","wrong","");}catch{}window.ShowLogin();window.Snapshot("04_captcha");
      window.Auth.Login("admin","CyberAdmin1",window.Auth.Captcha);
      foreach(var entity in Catalog.All){window.ShowData(entity);window.Snapshot("table_"+entity.Table);window.ShowEditor(entity,null);window.Snapshot("form_"+entity.Table);}
      window.ShowData(Catalog.Get("Bookings"));window.Snapshot("05_admin");
      Check("Поиск, фильтрация и сортировка в WPF",()=>{
       window.ShowData(Catalog.Get("Computers"));window.Snapshot("search_before");var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
       var search=(TextBox)typeof(MainWindow).GetField("search",flags).GetValue(window);var grid=(DataGrid)typeof(MainWindow).GetField("table",flags).GetValue(window);search.Text="VIP";Assert(((DataView)grid.ItemsSource).Count==6,"Поиск VIP");window.Snapshot("09_search");search.Text="";
       var col=(ComboBox)typeof(MainWindow).GetField("filterColumn",flags).GetValue(window);var val=(ComboBox)typeof(MainWindow).GetField("filterValue",flags).GetValue(window);col.SelectedItem="Статус";val.SelectedItem="Ремонт";Assert(((DataView)grid.ItemsSource).Count==1,"Фильтр ремонта");window.Snapshot("10_filter");
       col.SelectedIndex=0;var sort=(ComboBox)typeof(MainWindow).GetField("sortColumn",flags).GetValue(window);var dir=(ComboBox)typeof(MainWindow).GetField("sortDirection",flags).GetValue(window);sort.SelectedItem="Номер";dir.SelectedIndex=1;Assert(Convert.ToString(((DataView)grid.ItemsSource)[0]["Номер"])=="PC-03","Сортировка");window.Snapshot("11_sort");
      });
      window.ShowAnalytics();window.Snapshot("06_chart");window.SetTheme(true);window.ShowData(Catalog.Get("Clubs"));window.Snapshot("07_dark");window.SetTheme(false);
      window.Auth.Logout();window.Auth.Login("user","CyberUser1","");window.ShowData(Catalog.Get("Computers"));window.Snapshot("08_user");
      window.Run(()=>ClubService.Parse(new Field("Phone","Телефон","phone"),"123"));window.Run(()=>ClubService.Parse(new Field("Amount","Сумма","amount"),"-1"));window.Confirm("Удалить выбранный клуб? Связанные зоны, компьютеры, бронирования и платежи будут удалены каскадно.");window.Confirm("Вы действительно хотите выйти?");
      var t=DataContext.Query("SELECT c.Name AS [Клуб],ISNULL(SUM(p.Amount),0) AS [Выручка] FROM dbo.Clubs c LEFT JOIN dbo.Zones z ON z.ClubId=c.Id LEFT JOIN dbo.Computers pc ON pc.ZoneId=z.Id LEFT JOIN dbo.Bookings b ON b.ComputerId=pc.Id LEFT JOIN dbo.Payments p ON p.BookingId=b.Id GROUP BY c.Id,c.Name");
      foreach(string ext in new[]{"docx","pdf","xlsx"}){string path=Path.Combine(root,"docs","Пример_отчёта."+ext);Exporter.Save(path,"Выручка сети клубов",t);Assert(new FileInfo(path).Length>100,"Пустой экспорт "+ext);}
      passed++;log.AppendLine("PASS | Рендер WPF экранов, обе темы и экспорт docx/pdf/xlsx");
     }catch(Exception ex){failed++;log.AppendLine("FAIL | WPF и экспорт | "+ex);}finally{File.WriteAllText(Path.Combine(root,"tests","results","verification.txt"),"Дата: "+DateTime.Now+"\nПройдено: "+passed+"\nОшибок: "+failed+"\n"+log,Encoding.UTF8);window.Close();app.Shutdown();}
    }),DispatcherPriority.ApplicationIdle);app.Run(window);
   }catch(Exception ex){File.WriteAllText(Path.Combine(root,"tests","results","verification.txt"),"FATAL\n"+ex+"\n"+log,Encoding.UTF8);}
   finally{DataContext.ConnectionString=original;try{SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(original);master.InitialCatalog="master";using(var c=new SqlConnection(master.ToString())){c.Open();using(var cmd=new SqlCommand("IF DB_ID(@db) IS NOT NULL BEGIN ALTER DATABASE ["+db+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+db+"]; END",c)){cmd.Parameters.AddWithValue("@db",db);cmd.ExecuteNonQuery();}}}catch(Exception ex){File.AppendAllText(Path.Combine(root,"tests","results","verification.txt"),"\nОчистка тестовой БД: "+ex.Message);}}
  }
 }
}
