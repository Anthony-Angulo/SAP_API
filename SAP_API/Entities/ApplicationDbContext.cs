using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SAP_API.Models;

namespace SAP_API.Entities {
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole, string> {
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryProduct> InventoryProducts { get; set; }
        public DbSet<InventoryProductDetail> InventoryProductDetails { get; set; }
        public DbSet<InventoryProductBatch> InventoryProductBatches { get; set; }
        public DbSet<InventoryType> InventoryTypes { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<OrderAuth> OrderAuths { get; set; }
        public DbSet<OrderAuthRow> OrderAuthRows { get; set; }
        public DbSet<CodeBarDetail> CodeBarDetails { get; set; }
        public DbSet<ClientsProducts> Clientes_Productos { get; set; }
        public DbSet<LogFacturacion> LogFacturacion { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseMySql(GetConnectionString());
        }

        private static string GetConnectionString() {
            const string databaseName = "CCFNPROD";
            const string databaseUser = "remote";
            const string databasePass = "Ch1v@s.2019@CCFN.2X5B8M";

            return $"Server=54.177.203.25;" +
                   $"database={databaseName};" +
                   $"uid={databaseUser};" +
                   $"pwd={databasePass};" +
                   $"pooling=true;";
        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            //builder.Entity<IdentityUser>(entity =>
            //{
            //    entity.ToTable(name: "User");
            //});

            //builder.Entity<IdentityUser>()
            //    .ToTable("Users", "dbo");//.Property(p => p.Id).HasColumnName("User_Id");
            //builder.Entity<User>()
            //    .ToTable("Users", "dbo");//.Property(p => p.Id).HasColumnName("User_Id");

            //builder.Entity<IdentityUser>(entity =>
            //{
            //    entity.ToTable(name: "Users");
            //});

            builder.Entity<User>(entity =>
            {
                entity.ToTable(name: "Users");
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable(name: "Roles");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
                //in case you chagned the TKey type
                //  entity.HasKey(key => new { key.UserId, key.RoleId });
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
                //in case you chagned the TKey type
                //  entity.HasKey(key => new { key.ProviderKey, key.LoginProvider });       
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");

            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
                //in case you chagned the TKey type
                // entity.HasKey(key => new { key.UserId, key.LoginProvider, key.Name });

            });
        }

    }
}
