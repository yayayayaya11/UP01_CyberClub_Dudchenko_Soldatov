using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CyberClub {
 public static class Program {
  [STAThread] public static void Main(){
   Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
   var app=new Application();app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show(MainWindow.ErrorText(e.Exception),"CyberClub");e.Handled=true;};
   try{app.Run(new MainWindow());}catch(Exception ex){File.WriteAllText("startup-error.txt",ex.ToString());MessageBox.Show(ex.Message,"CyberClub • ошибка запуска");}
  }
 }
}
