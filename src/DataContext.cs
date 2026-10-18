using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using CyberClub.Models;

namespace CyberClub {
 public static class DataContext {
  public const string Database="DB_Dudchenko_Soldatov_CyberClub";
  // The override is used only by isolated tests. Normal startup uses App.config.
  static string connectionOverride;
  public static string ConnectionString {
   get {return connectionOverride??new EntityConnectionStringBuilder(ConfigurationManager.ConnectionStrings["CyberClubEntities"].ConnectionString).ProviderConnectionString;}
   set {connectionOverride=value;}
  }
  public static CyberClubEntities GetContext(){
   if(connectionOverride==null)return new CyberClubEntities();
   var cs=new EntityConnectionStringBuilder(ConfigurationManager.ConnectionStrings["CyberClubEntities"].ConnectionString);cs.ProviderConnectionString=connectionOverride;return new CyberClubEntities(cs.ToString());
  }
  public static DataTable Query(string sql, params object[] args) {
   using(var context=GetContext()) using(var cmd=context.Database.Connection.CreateCommand()) {
    cmd.CommandText=sql;Bind(cmd,args);cmd.Connection.Open();var t=new DataTable();using(var reader=cmd.ExecuteReader())t.Load(reader);return t;
   }
  }
  public static object Scalar(string sql,params object[] args) {
   using(var context=GetContext())using(var cmd=context.Database.Connection.CreateCommand()){cmd.CommandText=sql;Bind(cmd,args);cmd.Connection.Open();return cmd.ExecuteScalar();}
  }
  public static int Execute(string sql,params object[] args) {
   using(var context=GetContext())using(var cmd=context.Database.Connection.CreateCommand()){cmd.CommandText=sql;Bind(cmd,args);cmd.Connection.Open();return cmd.ExecuteNonQuery();}
  }
  public static void Bind(DbCommand cmd,object[] args){for(int i=0;i<args.Length;i++){var p=cmd.CreateParameter();p.ParameterName="@p"+i;p.Value=args[i]??DBNull.Value;cmd.Parameters.Add(p);}}
  public static void Install(string root){
   var cs=new SqlConnectionStringBuilder(ConnectionString); cs.InitialCatalog="master";
   using(var c=new SqlConnection(cs.ToString())) {c.Open();
    foreach(string batch in Regex.Split(File.ReadAllText(Path.Combine(root,"db","install.sql")),@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
     if(!String.IsNullOrWhiteSpace(batch))using(var cmd=new SqlCommand(batch,c)){cmd.CommandTimeout=60;cmd.ExecuteNonQuery();}
   }
  }
 }
}
