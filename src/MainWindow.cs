using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CyberClub {
 public class MainWindow:Window {
  public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
  public readonly AuthService Auth=new AuthService();public readonly ClubService Service;
  readonly Grid shell;readonly Frame frame;readonly TextBlock title,account,clock;
  readonly DispatcherTimer timer=new DispatcherTimer();
  Entity selected=Catalog.All[0];DataGrid table;TextBox search;ComboBox filterColumn,filterValue,sortColumn,sortDirection;
  DataTable source;bool refreshing,dark;
  public bool EvidenceMode;public string LastMessage;public int EvidenceIndex;
  public MainWindow(){
   Service=new ClubService(Auth);Width=1240;Height=740;MinWidth=1040;MinHeight=640;WindowStartupLocation=WindowStartupLocation.CenterScreen;
   using(var s=File.OpenRead(System.IO.Path.Combine(Root,"MainWindow.xaml")))shell=(Grid)XamlReader.Load(s);
   Content=shell;frame=(Frame)shell.FindName("MainFrame");title=(TextBlock)shell.FindName("PageTitle");account=(TextBlock)shell.FindName("AccountLabel");clock=(TextBlock)shell.FindName("Clock");
   foreach(var label in new[]{title,account,clock})label.SetResourceReference(TextBlock.ForegroundProperty,"TextBrush");
   ((Button)shell.FindName("BackButton")).Click+=(s,e)=>Run(()=>{if(Auth.Current==null)ShowLogin();else ShowData(selected);});
   ((Button)shell.FindName("ThemeButton")).Click+=(s,e)=>Run(()=>{SetTheme(!dark);});
   ((Button)shell.FindName("LogoutButton")).Click+=(s,e)=>Run(()=>{if(Auth.Current!=null&&Confirm("Выйти из учётной записи?")){Auth.Logout();ShowLogin();}});
   timer.Interval=TimeSpan.FromSeconds(1);timer.Tick+=(s,e)=>clock.Text=DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");timer.Start();clock.Text=DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
   Closing+=(s,e)=>{if(!EvidenceMode&&!Confirm("Вы действительно хотите выйти?"))e.Cancel=true;};Closed+=(s,e)=>timer.Stop();
   dark=File.Exists(System.IO.Path.Combine(Root,"theme.txt"))&&File.ReadAllText(System.IO.Path.Combine(Root,"theme.txt")).Trim()=="dark";SetTheme(dark);ShowLogin();
  }
  public void SetTheme(bool value){dark=value;Application.Current.Resources.MergedDictionaries.Clear();
   foreach(var file in new[]{value?"OwnDictionary.xaml":"Dictionary.xaml","Controls.xaml"})using(var s=File.OpenRead(System.IO.Path.Combine(Root,"Themes",file)))Application.Current.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(s));
   FontFamily=new FontFamily(new Uri(Root),"./res/fonts/#Open Sans");File.WriteAllText(System.IO.Path.Combine(Root,"theme.txt"),value?"dark":"light");
  }
  public static TextBlock Text(string s,int size=13){return new TextBlock{Text=s,FontSize=size,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,3,0,5)};}
  public static Button Button(string text,Action action,string hint=null){var b=new Button{Content=text,ToolTip=hint??text};b.Click+=(s,e)=>{var w=Application.Current.MainWindow as MainWindow;if(w!=null)w.Run(action);else action();};return b;}
  public static TextBox Input(string placeholder,string value="",int max=100){return new TextBox{Tag=placeholder,Text=value??"",MaxLength=max,ToolTip=placeholder};}
  public static PasswordBox Password(string hint){return new PasswordBox{MaxLength=64,ToolTip=hint};}
  public static void Label(Panel p,string text,UIElement input){p.Children.Add(Text(text));p.Children.Add(input);}
  public void Navigate(string name,UIElement body){Title="CyberClub — "+name;title.Text="CyberClub • "+name;var p=new Page{Content=body,FontFamily=FontFamily};frame.Navigate(p);frame.RemoveBackEntry();account.Text=Auth.Current==null?"Сеть киберспортивных клубов":Auth.Current.FullName+" • "+Auth.Current.Role;((Button)shell.FindName("LogoutButton")).IsEnabled=Auth.Current!=null;}
  StackPanel Form(string heading,int width=460){var p=new StackPanel{Width=width,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(20,22,20,20)};var h=Text(heading,26);h.FontWeight=FontWeights.SemiBold;p.Children.Add(h);return p;}
  ScrollViewer Scroll(UIElement p){return new ScrollViewer{Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};}
  public void ShowLogin(){
   var p=Form("Вход в CyberClub");p.Children.Add(Text("Управление сетью клубов, бронированиями и оплатой."));
   var login=Input("Например, admin","",40);var password=Password("Введите пароль");Label(p,"Логин",login);Label(p,"Пароль",password);
   var captcha=new StackPanel();var image=new Canvas{Width=280,Height=64,Background=Brushes.White};var answer=Input("Символы с изображения","",5);
   Action redraw=()=>{captcha.Visibility=Auth.NeedsCaptcha?Visibility.Visible:Visibility.Collapsed;DrawCaptcha(image);answer.Text="";};
   captcha.Children.Add(Text("Проверка после трёх неверных попыток"));captcha.Children.Add(image);captcha.Children.Add(answer);captcha.Children.Add(Button("Обновить капчу",()=>{Auth.GenerateCaptcha();redraw();}));p.Children.Add(captcha);redraw();
   Action enter=()=>{try{Auth.Login(login.Text,password.Password,answer.Text);ShowData(Catalog.All[0]);}catch(Exception ex){redraw();Notify(ErrorText(ex));}};
   var buttons=new WrapPanel();buttons.Children.Add(Button("Войти",enter));buttons.Children.Add(Button("Регистрация",ShowRegistration));buttons.Children.Add(Button("Смена пароля",()=>ShowPassword()));p.Children.Add(buttons);
   password.KeyDown+=(s,e)=>{if(e.Key==Key.Enter)Run(enter);};
   p.Children.Add(Text("Учебные аккаунты: admin / CyberAdmin1 и user / CyberUser1",12));Navigate("Авторизация",Scroll(p));
  }
  void DrawCaptcha(Canvas canvas){canvas.Children.Clear();var rand=new Random();for(int i=0;i<10;i++)canvas.Children.Add(new Line{X1=rand.Next(280),Y1=rand.Next(64),X2=rand.Next(280),Y2=rand.Next(64),Stroke=Brushes.Silver,StrokeThickness=1});
   for(int i=0;i<Auth.Captcha.Length;i++){var t=Text(Auth.Captcha[i].ToString(),29);t.Foreground=Brushes.DarkSlateBlue;t.FontWeight=FontWeights.Bold;t.RenderTransform=new RotateTransform(rand.Next(-15,16));Canvas.SetLeft(t,22+i*48);Canvas.SetTop(t,10);canvas.Children.Add(t);}}
  public void ShowRegistration(){var p=Form("Регистрация пользователя");var login=Input("От 3 латинских символов","",40);var name=Input("Иванов Иван Иванович");var pw=Password("От 6 символов, латинская буква и цифра");var repeat=Password("Повторите пароль");Label(p,"Логин",login);Label(p,"ФИО",name);Label(p,"Пароль",pw);Label(p,"Повтор пароля",repeat);
   p.Children.Add(Text("Новая учётная запись получает роль «Пользователь». Роль администратора назначает администратор."));p.Children.Add(Button("Зарегистрироваться",()=>{Auth.Register(login.Text,name.Text,pw.Password,repeat.Password);Notify("Регистрация завершена. Теперь войдите в систему.");ShowLogin();}));Navigate("Регистрация",Scroll(p));}
  public void ShowPassword(){var p=Form("Смена пароля");var login=Input("Логин",Auth.Current==null?"":Auth.Current.Login,40);var old=Password("Текущий пароль");var pw=Password("Новый пароль: латиница и цифры, от 6 символов");var repeat=Password("Повтор нового пароля");Label(p,"Логин",login);Label(p,"Текущий пароль",old);Label(p,"Новый пароль",pw);Label(p,"Повтор нового пароля",repeat);p.Children.Add(Button("Сохранить пароль",()=>{Auth.ChangePassword(login.Text,old.Password,pw.Password,repeat.Password);Notify("Пароль успешно изменён.");if(Auth.Current==null)ShowLogin();else ShowData(selected);}));Navigate("Смена пароля",Scroll(p));}
  public void ShowData(Entity e){
   selected=e;refreshing=true;source=Service.List(e);
   var root=new Grid{Margin=new Thickness(14)};root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(165)});root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
   var nav=new StackPanel();nav.Children.Add(Text("РАЗДЕЛЫ",12));foreach(var entity in Catalog.All){var item=entity;nav.Children.Add(Button(item.Title,()=>ShowData(item)));}
   if(Auth.Current.IsAdmin)nav.Children.Add(Button("Диаграммы и отчёты",ShowAnalytics));nav.Children.Add(Button("Смена пароля",ShowPassword));root.Children.Add(Scroll(nav));
   var content=new Grid{Margin=new Thickness(14,0,0,0)};Grid.SetColumn(content,1);root.Children.Add(content);content.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});content.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});content.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});content.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});content.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
   content.Children.Add(Text(e.Title,24));var tools=new WrapPanel();Grid.SetRow(tools,1);content.Children.Add(tools);
   search=Input("Поиск по всем столбцам","",100);search.Width=230;search.Margin=new Thickness(4);tools.Children.Add(search);
   filterColumn=new ComboBox{Width=150,Margin=new Thickness(4),ToolTip="Столбец для точного фильтра"};filterColumn.Items.Add("Без фильтра");foreach(DataColumn col in source.Columns)filterColumn.Items.Add(col.ColumnName);filterColumn.SelectedIndex=0;tools.Children.Add(filterColumn);
   filterValue=new ComboBox{Width=190,Margin=new Thickness(4),ToolTip="Значение выбранного столбца"};tools.Children.Add(filterValue);
   sortColumn=new ComboBox{Width=145,Margin=new Thickness(4),ToolTip="Сортировать по столбцу"};foreach(DataColumn col in source.Columns)sortColumn.Items.Add(col.ColumnName);sortColumn.SelectedIndex=0;tools.Children.Add(sortColumn);
   sortDirection=new ComboBox{Width=130,Margin=new Thickness(4),ToolTip="Направление сортировки"};sortDirection.ItemsSource=new[]{"По возрастанию","По убыванию"};sortDirection.SelectedIndex=0;tools.Children.Add(sortDirection);
   var actions=new WrapPanel();Grid.SetRow(actions,2);content.Children.Add(actions);
   if(Auth.Current.IsAdmin){actions.Children.Add(Button("Добавить",()=>ShowEditor(e,null)));actions.Children.Add(Button("Редактировать",()=>ShowEditor(e,SelectedId())));actions.Children.Add(Button("Удалить",()=>{int id=SelectedId();if(Confirm("Удалить запись №"+id+" из раздела «"+e.Title+"»?\nСвязанные записи будут удалены каскадно. Отменить это действие нельзя.")){Service.Delete(e,id);ShowData(e);Notify("Запись и связанные данные удалены.");}}));actions.Children.Add(Button("Экспорт отчёта",()=>ExportData()));}
   actions.Children.Add(Button("Обновить",()=>ShowData(e)));
   table=new DataGrid{ItemsSource=source.DefaultView};Grid.SetRow(table,3);content.Children.Add(table);table.AutoGeneratingColumn+=(s,args)=>{var column=args.Column as DataGridTextColumn;if(column!=null){var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.MarginProperty,new Thickness(7,3,7,3)));style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty,VerticalAlignment.Center));column.ElementStyle=style;if(args.PropertyType==typeof(DateTime))column.Binding=new Binding(args.PropertyName){StringFormat="dd.MM.yyyy HH:mm"};}};
   var count=Text("Записей: "+source.Rows.Count+". Для сортировки также нажмите заголовок столбца.",12);Grid.SetRow(count,4);content.Children.Add(count);
   Action apply=()=>{if(refreshing)return;ApplyFilters();count.Text="Показано: "+((DataView)table.ItemsSource).Count+" из "+source.Rows.Count+". "+(Auth.Current.IsAdmin?"Добавление, редактирование и удаление доступны.":"Режим просмотра.");};
   search.TextChanged+=(s,args)=>apply();filterColumn.SelectionChanged+=(s,args)=>{refreshing=true;filterValue.Items.Clear();filterValue.Items.Add("Все значения");if(filterColumn.SelectedIndex>0){string col=(string)filterColumn.SelectedItem;foreach(string val in source.Rows.Cast<DataRow>().Select(r=>Format(r[col])).Distinct().OrderBy(x=>x))filterValue.Items.Add(val);}filterValue.SelectedIndex=0;refreshing=false;apply();};
   filterValue.SelectionChanged+=(s,args)=>apply();sortColumn.SelectionChanged+=(s,args)=>apply();sortDirection.SelectionChanged+=(s,args)=>apply();refreshing=false;
   Navigate((Auth.Current.IsAdmin?"Администратор • ":"Пользователь • ")+e.Title,root);
  }
  public static string Format(object value){return value is DateTime?((DateTime)value).ToString("dd.MM.yyyy HH:mm"):value is decimal?((decimal)value).ToString("0.00",CultureInfo.GetCultureInfo("ru-RU")):Convert.ToString(value);}
  public void ApplyFilters(){
   string q=search.Text.Trim();string fc=filterColumn.SelectedIndex>0?Convert.ToString(filterColumn.SelectedItem):null;string fv=filterValue.SelectedIndex>0?Convert.ToString(filterValue.SelectedItem):null;
   var t=source.Clone();foreach(DataRow r in source.Rows){if(q!=""&&!source.Columns.Cast<DataColumn>().Any(c=>Format(r[c]).IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0))continue;if(fc!=null&&fv!=null&&Format(r[fc])!=fv)continue;t.ImportRow(r);}
   string sort=Convert.ToString(sortColumn.SelectedItem);if(sort!="")t.DefaultView.Sort="["+sort.Replace("]","")+"] "+(sortDirection.SelectedIndex==1?"DESC":"ASC");table.ItemsSource=t.DefaultView;
  }
  int SelectedId(){var row=table.SelectedItem as DataRowView;if(row==null)throw new Exception("Выберите запись в таблице.");return Convert.ToInt32(row["Id"]);}
  public void ShowEditor(Entity e,int? id){
   Service.RequireAdmin();var data=id.HasValue?Service.Read(e,id.Value):new Dictionary<string,object>();var p=Form((id.HasValue?"Изменить • ":"Добавить • ")+e.Title,560);var controls=new Dictionary<Field,Control>();
   foreach(var f in e.Fields){Control control;object value=data.ContainsKey(f.Key)?data[f.Key]:null;
    if(f.Kind=="ref"){var c=new ComboBox{ItemsSource=Catalog.Lookup(f.Source).DefaultView,DisplayMemberPath="Label",SelectedValuePath="Id",ToolTip="Выберите: "+f.Title};if(value!=null)c.SelectedValue=value;control=c;}
    else if(f.Kind=="enum"){var c=new ComboBox{ItemsSource=f.Source.Split('|'),ToolTip=f.Title};c.SelectedItem=value??f.Source.Split('|')[0];control=c;}
    else if(f.Kind=="password")control=Password("Оставьте пустым, чтобы сохранить текущий пароль");
    else {var tb=Input(f.Kind=="date"?"18.10.2026 12:00":f.Title,value==null?(f.Kind=="date"?DateTime.Now.AddMinutes(60).ToString("dd.MM.yyyy HH:mm"):""):Format(value),f.Max);
     if(f.Kind=="money"||f.Kind=="amount")tb.PreviewTextInput+=(s,a)=>a.Handled=a.Text.Any(ch=>!Char.IsDigit(ch)&&ch!=','&&ch!='.');
     if(f.Kind=="phone")tb.PreviewTextInput+=(s,a)=>a.Handled=a.Text.Any(ch=>!Char.IsDigit(ch)&&ch!='+');control=tb;}
    Label(p,f.Title,control);controls.Add(f,control);
   }
   if(e.Table=="Bookings")p.Children.Add(Text("Стоимость рассчитывается по минутам. Цена тарифа фиксируется при создании брони; смена тарифа обновит её. Пересечения времени запрещены."));
   var buttons=new WrapPanel();buttons.Children.Add(Button("Сохранить",()=>{var values=new Dictionary<string,object>();foreach(var pair in controls){var f=pair.Key;var c=pair.Value;string s=c is PasswordBox?((PasswordBox)c).Password:c is ComboBox?(f.Kind=="ref"?Convert.ToString(((ComboBox)c).SelectedValue):Convert.ToString(((ComboBox)c).SelectedItem)):((TextBox)c).Text;
    values[f.Key]=f.Kind=="password"?s:ClubService.Parse(f,s);}
    Service.Save(e,id,values);ShowData(e);Notify("Данные сохранены.");}));buttons.Children.Add(Button("Отмена",()=>ShowData(e)));p.Children.Add(buttons);Navigate((id.HasValue?"Редактирование • ":"Добавление • ")+e.Title,Scroll(p));
  }
  public void ExportData(){Service.RequireAdmin();var dialog=new SaveFileDialog{Title="Сохранить отчёт",FileName=selected.Table+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"),Filter="Документ Word|*.docx|PDF|*.pdf|Книга Excel|*.xlsx"};if(dialog.ShowDialog(this)==true){var view=table.ItemsSource as DataView;Exporter.Save(dialog.FileName,selected.Title,view.ToTable());Notify("Отчёт сохранён: "+dialog.FileName);}}
  public void ShowAnalytics(){Service.RequireAdmin();var root=new Grid{Margin=new Thickness(28)};root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.Children.Add(Text("Выручка клубов по платежам",24));
   var filters=new WrapPanel();Grid.SetRow(filters,1);root.Children.Add(filters);var clubs=new ComboBox{Width=240,Margin=new Thickness(5),DisplayMemberPath="Label",SelectedValuePath="Id",ToolTip="Все клубы или один клуб"};var ct=Catalog.Lookup("Clubs");var all=ct.NewRow();all["Id"]=0;all["Label"]="Все клубы";ct.Rows.InsertAt(all,0);clubs.ItemsSource=ct.DefaultView;clubs.SelectedIndex=0;filters.Children.Add(clubs);
   var from=new DatePicker{SelectedDate=new DateTime(2026,10,1),Width=150,Margin=new Thickness(5),ToolTip="Начало периода включительно"};var to=new DatePicker{SelectedDate=DateTime.Today.AddDays(1),Width=150,Margin=new Thickness(5),ToolTip="Конец периода включительно"};filters.Children.Add(from);filters.Children.Add(to);
   var kind=new ComboBox{Width=160,Margin=new Thickness(5),ItemsSource=new[]{"Столбцы","Круговая"},SelectedIndex=0,ToolTip="Тип диаграммы"};filters.Children.Add(kind);
   var body=new Grid();body.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(2,GridUnitType.Star)});body.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});Grid.SetRow(body,2);root.Children.Add(body);
   var canvas=new Canvas{MinHeight=270,Margin=new Thickness(15),Background=Brushes.White,ClipToBounds=true};body.Children.Add(canvas);var grid=new DataGrid{Margin=new Thickness(10)};Grid.SetColumn(grid,1);body.Children.Add(grid);var total=Text("");var bottom=new WrapPanel();bottom.Children.Add(total);Grid.SetRow(bottom,3);root.Children.Add(bottom);DataTable result=null;
   Action update=()=>{if(!from.SelectedDate.HasValue||!to.SelectedDate.HasValue||from.SelectedDate>to.SelectedDate)throw new Exception("Укажите корректный период.");int c=Convert.ToInt32(clubs.SelectedValue);result=Service.Revenue(c==0?(int?)null:c,from.SelectedDate.Value,to.SelectedDate.Value.AddDays(1));grid.ItemsSource=result.DefaultView;total.Text="Итого: "+result.Rows.Cast<DataRow>().Sum(r=>Convert.ToDecimal(r[1])).ToString("N2")+" руб.   ";DrawChart(canvas,result,kind.SelectedIndex==1);};
   filters.Children.Add(Button("Построить",update));bottom.Children.Add(Button("Экспорт отчёта",()=>{if(result==null)update();var d=new SaveFileDialog{FileName="Выручка_клубов",Filter="Word|*.docx|PDF|*.pdf|Excel|*.xlsx"};if(d.ShowDialog(this)==true){Exporter.Save(d.FileName,"Выручка с "+from.SelectedDate.Value.ToShortDateString()+" по "+to.SelectedDate.Value.ToShortDateString(),result);Notify("Отчёт сохранён.");}}));canvas.SizeChanged+=(s,e)=>{if(result!=null)DrawChart(canvas,result,kind.SelectedIndex==1);};kind.SelectionChanged+=(s,e)=>{if(result!=null)DrawChart(canvas,result,kind.SelectedIndex==1);};Navigate("Диаграммы и отчёты",root);update();
  }
  public static void DrawChart(Canvas canvas,DataTable data,bool pie){canvas.Children.Clear();double w=Math.Max(350,canvas.ActualWidth),h=Math.Max(260,canvas.ActualHeight);var colors=new[]{Color.FromRgb(1,152,175),Color.FromRgb(35,62,118),Color.FromRgb(77,198,197)};double total=data.Rows.Cast<DataRow>().Sum(r=>Convert.ToDouble(r[1])),max=data.Rows.Cast<DataRow>().Select(r=>Convert.ToDouble(r[1])).DefaultIfEmpty(0).Max();
   if(total==0){var empty=Text("За период платежей нет",18);empty.Foreground=Brushes.Black;Canvas.SetLeft(empty,20);Canvas.SetTop(empty,20);canvas.Children.Add(empty);return;}
   double angle=-90;for(int i=0;i<data.Rows.Count;i++){double value=Convert.ToDouble(data.Rows[i][1]);var brush=new SolidColorBrush(colors[i%colors.Length]);
    if(pie){double sweep=value/total*360,cx=w*.40,cy=h*.43,r=Math.Min(w*.30,h*.32);if(sweep>=359.999){var ellipse=new Ellipse{Width=r*2,Height=r*2,Fill=brush};Canvas.SetLeft(ellipse,cx-r);Canvas.SetTop(ellipse,cy-r);canvas.Children.Add(ellipse);}else if(sweep>0){var a=new Point(cx+r*Math.Cos(angle*Math.PI/180),cy+r*Math.Sin(angle*Math.PI/180));var b=new Point(cx+r*Math.Cos((angle+sweep)*Math.PI/180),cy+r*Math.Sin((angle+sweep)*Math.PI/180));var figure=new PathFigure{StartPoint=new Point(cx,cy),IsClosed=true};figure.Segments.Add(new LineSegment(a,true));figure.Segments.Add(new ArcSegment(b,new Size(r,r),0,sweep>180,SweepDirection.Clockwise,true));canvas.Children.Add(new System.Windows.Shapes.Path{Fill=brush,Data=new PathGeometry(new[]{figure})});}angle+=sweep;
    }else{double bw=(w-60)/data.Rows.Count,bh=(h-110)*value/max;var rect=new Rectangle{Width=Math.Max(16,bw-22),Height=bh,Fill=brush};Canvas.SetLeft(rect,35+i*bw);Canvas.SetTop(rect,h-75-bh);canvas.Children.Add(rect);var amount=Text(value.ToString("N2"),12);amount.Foreground=Brushes.Black;Canvas.SetLeft(amount,35+i*bw);Canvas.SetTop(amount,h-100-bh);canvas.Children.Add(amount);}
    var label=Text(Convert.ToString(data.Rows[i][0])+": "+value.ToString("N2")+" руб.",11);label.Foreground=Brushes.Black;label.Width=pie?w-20:Math.Max(60,(w-60)/data.Rows.Count-6);Canvas.SetLeft(label,pie?12:35+i*(w-60)/data.Rows.Count);Canvas.SetTop(label,pie?h-75+i*22:h-70);canvas.Children.Add(label);
   }
  }
  public void Run(Action a){try{a();}catch(Exception ex){Notify(ErrorText(ex));}}
  public static string ErrorText(Exception ex){System.Data.SqlClient.SqlException sql=null;for(var current=ex;current!=null;current=current.InnerException){sql=current as System.Data.SqlClient.SqlException;if(sql!=null)break;}if(sql!=null){if(sql.Number==2601||sql.Number==2627)return "Такое значение уже существует. Проверьте уникальные поля.";if(sql.Number==547)return "Нарушено ограничение данных. Проверьте значения и связанные записи.";if(sql.Number<50000)return "Ошибка подключения или операции с базой. Проверьте LocalDB и наличие базы. Подробности: "+sql.Message;return sql.Message;}var validation=ex as System.Data.Entity.Validation.DbEntityValidationException;if(validation!=null)return String.Join("\n",validation.EntityValidationErrors.SelectMany(e=>e.ValidationErrors).Select(e=>e.PropertyName+": "+e.ErrorMessage));return ex.Message;}
  Window MessageWindow(string message,bool question){var p=new StackPanel{Margin=new Thickness(22)};p.Children.Add(Text(question?"Подтверждение действия":"Сообщение CyberClub",20));p.Children.Add(Text(message,15));var buttons=new WrapPanel{Margin=new Thickness(0,14,0,0),HorizontalAlignment=HorizontalAlignment.Right};var surface=new Border{Child=p};surface.SetResourceReference(Border.BackgroundProperty,"BackgroundBrush");var w=new Window{Owner=this,Title=question?"Подтверждение":"CyberClub",Width=620,SizeToContent=SizeToContent.Height,MinHeight=190,MaxHeight=550,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,FontFamily=FontFamily,Content=surface};
   if(question){buttons.Children.Add(Button("Да",()=>{w.DialogResult=true;}));buttons.Children.Add(Button("Нет",()=>{w.DialogResult=false;}));}else buttons.Children.Add(Button("ОК",()=>w.Close()));p.Children.Add(buttons);return w;}
  public void Notify(string message){LastMessage=message;var w=MessageWindow(message,false);if(EvidenceMode){w.Show();Dispatcher.Invoke(DispatcherPriority.ContextIdle,new Action(()=>{}));SnapshotElement((FrameworkElement)w.Content,"message_"+(EvidenceIndex++));w.Close();}else w.ShowDialog();}
  public bool Confirm(string message){LastMessage=message;var w=MessageWindow(message,true);if(EvidenceMode){w.Show();Dispatcher.Invoke(DispatcherPriority.ContextIdle,new Action(()=>{}));SnapshotElement((FrameworkElement)w.Content,"confirm_"+(EvidenceIndex++));w.Close();return false;}return w.ShowDialog()==true;}
  public void Snapshot(string name){Dispatcher.Invoke(DispatcherPriority.ContextIdle,new Action(()=>{}));UpdateLayout();frame.UpdateLayout();var image=new RenderTargetBitmap((int)shell.ActualWidth,(int)shell.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(shell);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(image));Directory.CreateDirectory(System.IO.Path.Combine(Root,"docs","screenshots"));using(var s=File.Create(System.IO.Path.Combine(Root,"docs","screenshots",name+".png")))enc.Save(s);}
  void SnapshotElement(FrameworkElement element,string name){element.UpdateLayout();var bmp=new RenderTargetBitmap(Math.Max(1,(int)element.ActualWidth),Math.Max(1,(int)element.ActualHeight),96,96,PixelFormats.Pbgra32);bmp.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using(var s=File.Create(System.IO.Path.Combine(Root,"docs","screenshots",name+".png")))encoder.Save(s);}
 }
}
