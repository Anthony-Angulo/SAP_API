using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Entities
{
    public class LogsContext:DbContext
    {
        public DbSet<SAPLog> SAPLog { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(GetConnectionString());
        }
        private static string GetConnectionString()
        {
            const string databaseName = "crm";
            const string databaseUser = "apiccfn";
            const string databasePass = "zs5ABy#nw2Nv=xh&";

            return $"Server=54.177.203.25;" +
                   $"database={databaseName};" +
                   $"uid={databaseUser};" +
                   $"pwd={databasePass};" +
                   $"pooling=true;";
        }
    }
    public class SAPLog
    {
        public int id { get; set; }

        public string action { get; set; }

        public string document { get; set; }

        public DateTime created_at { get; set; }
    }
}
