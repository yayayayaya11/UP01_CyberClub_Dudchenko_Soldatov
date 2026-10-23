using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using CyberClub.Models;

namespace CyberClub
{
    public static class Core
    {
        public static CyberClubEntities Context = new CyberClubEntities();
        public const string Database = "DB_Dudchenko_Soldatov_CyberClub";
        public static string ConnectionString
        {
            get
            {
                return Context.Database.Connection.ConnectionString;
            }

            set
            {
                var connection = new EntityConnectionStringBuilder(ConfigurationManager.ConnectionStrings["CyberClubEntities"].ConnectionString);
                connection.ProviderConnectionString = value;
                Context.Dispose();
                Context = new CyberClubEntities(connection.ToString());
            }
        }


        public static int SaveChanges()
        {
            try
            {
                return Context.SaveChanges();
            }
            finally
            {
                ClearTracking();
            }
        }

        public static void ClearTracking()
        {
            // отклонённые записи не должны повторно сохраняться из следующей формы
            foreach (var entry in Context.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;
        }

        static T Command<T>(string sql, object[] args, Func<DbCommand, T> action)
        {
            var connection = Context.Database.Connection;
            bool opened = connection.State != ConnectionState.Open;
            try
            {
                if (opened)
                    connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    for (int i = 0; i < args.Length; i++)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = "@p" + i;
                        p.Value = args[i] ?? DBNull.Value;
                        command.Parameters.Add(p);
                    }

                    return action(command);
                }
            }
            finally
            {
                if (opened)
                    connection.Close();
            }
        }

        public static DataTable Query(string sql, params object[] args)
        {
            return Command(sql, args, command =>
            {
                var table = new DataTable();
                using (var reader = command.ExecuteReader())
                    table.Load(reader);
                return table;
            });
        }

        public static object Scalar(string sql, params object[] args)
        {
            return Command(sql, args, command => command.ExecuteScalar());
        }

        public static int Execute(string sql, params object[] args)
        {
            return Command(sql, args, command => command.ExecuteNonQuery());
        }
    }
}
