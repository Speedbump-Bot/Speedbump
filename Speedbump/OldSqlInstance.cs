using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using System.Diagnostics;

namespace Speedbump
{
    public class OldSqlInstance : IDisposable
    {
        private static string ConnString;
        private static string Prefix;

        static ILogger Logger;

        public static void Init(IConfiguration config, ILogger logger)
        {
            Logger = logger;
            logger.Debug("Initializing SqlInstance class...");

            var host = config.Get<string>("sql.host");
            var database = config.Get<string>("sql.database");
            var user = config.Get<string>("sql.user");
            var password = config.Get<string>("sql.password");
            var port = config.Get<int>("sql.port");

            Prefix = config.Get<string>("sql.prefix");
            ConnString = $"server={host};user={user};database={database};port={port};password={password};pooling=true";

            using var conn = new MySqlConnection(ConnString);
            conn.Open();
            logger.Information("SQL connection successful.");
            conn.Close();
        }

        private MySqlConnection Connection { get; }

        public OldSqlInstance()
        {
            startup:;
            var attempts = 0;
            try
            {
                attempts++;
                Connection = new MySqlConnection(ConnString);
                Connection.Open();
            } 
            catch
            {
                if (attempts >= 3)
                {
                    throw;
                }
                else
                {
                    goto startup;
                }
            }
        }

        public int Execute(string sql, params object[] parameters)
        {
            if (Debugger.IsAttached)
            {
                Logger.Warning("EXECUTE " + sql + " " + JsonConvert.SerializeObject(parameters));
            }
            sql = sql.Replace("@p0", Prefix);

            var cmd = new MySqlCommand(sql, Connection);
            var i = 0;
            foreach (var p in parameters)
            {
                i++;
                cmd.Parameters.AddWithValue("@p" + i, p);
            }
            var rows = cmd.ExecuteNonQuery();
            cmd.Dispose();
            return rows;
        }

        public Dataset Read(string sql, params object[] parameters)
        {
            //Logger.Warning("READ " + sql + " " + JsonConvert.SerializeObject(parameters));
            sql = sql.Replace("@p0", Prefix);

            var start = DateTime.UtcNow;
            var toReturn = new Dataset();
            var cmd = new MySqlCommand(sql, Connection);
            var i = 0;
            foreach (var p in parameters)
            {
                i++;
                cmd.Parameters.AddWithValue("@p" + i, p);
            }
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (toReturn.Columns.Count == 0)
                {
                    for (var j = 0; j < reader.FieldCount; j++)
                    {
                        toReturn.Columns.Add(reader.GetName(j));
                        toReturn.ColumnTypes.Add(reader.GetDataTypeName(j));
                    }
                }

                var row = new List<object>();
                for (var j = 0; j < reader.FieldCount; j++)
                {
                    var value = reader[j];
                    row.Add(value is DBNull ? null : value);
                }
                toReturn.Rows.Add(row);
            }
            reader.DisposeAsync().GetAwaiter().GetResult();
            cmd.Dispose();
            toReturn.RowCount = toReturn.Rows.Count;
            toReturn.ReadTimeMS = (DateTime.UtcNow - start).TotalMilliseconds;
            return toReturn;
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
