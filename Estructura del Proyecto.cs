using Microsoft.EntityFrameworkCore;
using SitiosIntranet.Web.Models;

namespace SitiosIntranet.Web.Data
{
    /// <summary>
    /// DbContext que representa la conexión al sistema AS400.
    /// Contiene las entidades necesarias para consultar/modificar datos.
    /// </summary>
    public class As400DbContext : DbContext
    {
        public As400DbContext(DbContextOptions<As400DbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Representa la tabla USUADMIN en AS400.
        /// Este modelo se usa para autenticación de usuarios.
        /// </summary>
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapea manualmente la tabla y columnas si es necesario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasNoKey(); // Como estás usando FromSqlRaw, puedes usar sin clave
                entity.ToTable("USUADMIN", "BCAH96DTA"); // esquema y tabla reales en AS400

                entity.Property(e => e.USUARIO).HasColumnName("USUARIO");
                entity.Property(e => e.TIPUSU).HasColumnName("TIPUSU");
                entity.Property(e => e.ESTADO).HasColumnName("ESTADO");
                entity.Property(e => e.PASS).HasColumnName("PASS");
            });
        }
    }
}




namespace SitiosIntranet.Web.Models
{
    /// <summary>
    /// Representa un registro de la tabla USUADMIN en AS400.
    /// Se usa para validar credenciales y roles del usuario.
    /// </summary>
    public class Usuario
    {
        public string USUARIO { get; set; }
        public string TIPUSU { get; set; }
        public string ESTADO { get; set; }
        public string PASS { get; set; }
    }
}

