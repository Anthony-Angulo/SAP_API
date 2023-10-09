using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SAP_API.Controllers;
using SAP_API.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace SAP_API.Entities
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole, string>
    {
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
        public DbSet<ClientsProductsPreferidos> Clientes_Productos { get; set; }
        public DbSet<LogFacturacion> LogFacturacion { get; set; }
        public DbSet<AutorizacionRequest> AutorizacionRequest { get; set; }
        public DbSet<AutorizacionAlmacenes> AutorizacionAlmacenes { get; set; }

        public DbSet<TrasladosVirtuales> TrasladosVirtuales { get; set; }
        public DbSet<VentaInfo> VentaInfo { get; set; }
        public DbSet<cotizaciones> cotizaciones { get; set; }

        public DbSet<rutas> rutas { get; set; }
        public DbSet<tiendas_ruta> tiendas_ruta { get; set; }

        public DbSet<FacBurn> FacBurn { get; set; }
        public DbSet<QR_ALMACENES> QR_ALMACENES { get; set; }
        /*public DbSet<rutinas> rutinas { get; set; }
        public DbSet<CategoriaPregunta> CategoriaPreguntas { get; set; }
        public DbSet<Preguntas> Preguntas { get; set; }
        public DbSet<Respuestas> Respuestas { get; set; }
        */
        public DbSet<VentaLibreModel> VentaLibre { get; set; }

        public DbSet<tarima> tarima { get; set; }


        public enum Series : int
        {
            S01_CEDIS = 72,
            S05_PACHUCO = 76,
            S06_CENTRAL_DE_ABASTOS = 77,
            S08_BARULIO_MALDONADO = 78,
            S10_TIJUANA_OTAY = 81,
            S12_SAN_LUIS_MAYOREO = 83,
            S13_TIJUANA_ABASTOS = 84,
            S15_MAYOREO_BELLAVISTA = 86,
            S17_BRULIO_FyV = 88,
            S24_MAYOREO_ENSENADA = 330,
            S36_TIJUANA_20_NOVIEMBRE = 342,
            S47_MAYOREO_LAZARO = 353,
            S49_CENTRAL_DE_ABARROTES = 355,
            S53_PLANTA_PROCESADORA = 359,
            S55_PARQUE_MORELOS = 361,
            S59_TIJUANA_MATAMOROS = 369,
            S62_ANAHUAC_MAYOREO = 1748,
            S70_BENITEZ = 1929,

            S63_MAYOREO_BUENA_VISTA_TJ = 1768,
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(GetConnectionString());
        }

        private static string GetConnectionString()
        {
            const string databaseName = "CCFNPROD";
            const string databaseUser = "apisap";
            const string databasePass = "34sg!MaXN**5c%tG";

            return $"Server=54.177.203.25;" +
                   $"database={databaseName};" +
                   $"uid={databaseUser};" +
                   $"pwd={databasePass};" +
                   $"pooling=true;";
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
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
