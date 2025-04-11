using Microsoft.EntityFrameworkCore;

namespace RestUtilities.Helpers
{
    /// <summary>
    /// Helper especializado para operaciones CRUD sobre AS400 usando Entity Framework Core.
    /// Incluye validación previa antes de guardar cambios.
    /// </summary>
    public static class As400EntityHelper
    {
        /// <summary>
        /// Inserta una entidad en AS400 y guarda si hay cambios.
        /// </summary>
        public static async Task<bool> InsertAsync<T>(DbContext context, T entity) where T : class
        {
            context.Set<T>().Add(entity);

            if (!HasPendingChanges(context))
                return false;

            return await context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Actualiza una entidad existente si hay cambios.
        /// </summary>
        public static async Task<bool> UpdateAsync<T>(DbContext context, T entity) where T : class
        {
            context.Set<T>().Update(entity);

            if (!HasPendingChanges(context))
                return false;

            return await context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Elimina una entidad de AS400 si es válida.
        /// </summary>
        public static async Task<bool> DeleteAsync<T>(DbContext context, T entity) where T : class
        {
            context.Set<T>().Remove(entity);

            if (!HasPendingChanges(context))
                return false;

            return await context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Verifica si hay cambios pendientes en el contexto antes de guardar.
        /// </summary>
        private static bool HasPendingChanges(DbContext context)
        {
            return context.ChangeTracker.Entries()
                .Any(e => e.State == EntityState.Added
                       || e.State == EntityState.Modified
                       || e.State == EntityState.Deleted);
        }

        /// <summary>
        /// Obtiene todos los registros de una tabla.
        /// </summary>
        public static async Task<List<T>> GetAllAsync<T>(DbContext context) where T : class
        {
            return await context.Set<T>().AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Obtiene una entidad por clave primaria.
        /// </summary>
        public static async Task<T?> GetByIdAsync<T>(DbContext context, object id) where T : class
        {
            return await context.Set<T>().FindAsync(id);
        }
    }
}
