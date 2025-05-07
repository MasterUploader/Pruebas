using CAUAdministracion.Models;
using Connections.Interfaces;
using System.Data.Common;

namespace CAUAdministracion.Services.Agencias
{
    /// <summary>
    /// Servicio que gestiona operaciones sobre agencias en AS400.
    /// </summary>
    public class AgenciaService : IAgenciaService
    {
        private readonly IDatabaseConnection _as400;

        public AgenciaService(IDatabaseConnection as400)
        {
            _as400 = as400;
        }

        /// <summary>
        /// Lista todas las agencias registradas.
        /// </summary>
        public List<AgenciaModel> ObtenerAgencias()
        {
            var agencias = new List<AgenciaModel>();
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = @"
                    SELECT CODCCO, NOMAGE, ZONA, MARQUESINA, RSTBRANCH, NOMBD, NOMSER, IPSER
                    FROM BCAH96DTA.RSAGE01
                    ORDER BY CODCCO";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    agencias.Add(new AgenciaModel
                    {
                        Codcco = Convert.ToInt32(reader["CODCCO"]),
                        Nombre = reader["NOMAGE"]?.ToString(),
                        Zona = Convert.ToInt32(reader["ZONA"]),
                        Marquesina = reader["MARQUESINA"]?.ToString(),
                        RstBranch = reader["RSTBRANCH"]?.ToString(),
                        NombreBD = reader["NOMBD"]?.ToString(),
                        NombreServer = reader["NOMSER"]?.ToString(),
                        IpServer = reader["IPSER"]?.ToString()
                    });
                }
            }
            finally
            {
                _as400.Close();
            }

            return agencias;
        }

        /// <summary>
        /// Inserta una nueva agencia en la base de datos.
        /// </summary>
        public bool InsertarAgencia(AgenciaModel agencia)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    INSERT INTO BCAH96DTA.RSAGE01 
                    (CODCCO, NOMAGE, ZONA, MARQUESINA, RSTBRANCH, NOMBD, NOMSER, IPSER)
                    VALUES 
                    ({agencia.Codcco}, '{agencia.Nombre}', {agencia.Zona}, '{agencia.Marquesina}', 
                     '{agencia.RstBranch}', '{agencia.NombreBD}', '{agencia.NombreServer}', '{agencia.IpServer}')";

                return command.ExecuteNonQuery() > 0;
            }
            finally
            {
                _as400.Close();
            }
        }

        /// <summary>
        /// Actualiza los datos de una agencia existente.
        /// </summary>
        public bool ActualizarAgencia(AgenciaModel agencia)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    UPDATE BCAH96DTA.RSAGE01
                    SET NOMAGE = '{agencia.Nombre}', ZONA = {agencia.Zona},
                        MARQUESINA = '{agencia.Marquesina}', RSTBRANCH = '{agencia.RstBranch}',
                        NOMBD = '{agencia.NombreBD}', NOMSER = '{agencia.NombreServer}', IPSER = '{agencia.IpServer}'
                    WHERE CODCCO = {agencia.Codcco}";

                return command.ExecuteNonQuery() > 0;
            }
            finally
            {
                _as400.Close();
            }
        }

        /// <summary>
        /// Elimina una agencia de la base de datos por su código.
        /// </summary>
        public bool EliminarAgencia(int codcco)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    DELETE FROM BCAH96DTA.RSAGE01 WHERE CODCCO = {codcco}";

                return command.ExecuteNonQuery() > 0;
            }
            finally
            {
                _as400.Close();
            }
        }
    }
}



using CAUAdministracion.Models;
using CAUAdministracion.Services.Agencias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAUAdministracion.Controllers
{
    [Authorize]
    public class AgenciasController : Controller
    {
        private readonly IAgenciaService _agenciaService;

        public AgenciasController(IAgenciaService agenciaService)
        {
            _agenciaService = agenciaService;
        }

        /// <summary>
        /// Muestra la vista de mantenimiento de agencias.
        /// </summary>
        public IActionResult Index()
        {
            var agencias = _agenciaService.ObtenerAgencias();
            return View(agencias);
        }

        /// <summary>
        /// Muestra la vista para agregar una nueva agencia.
        /// </summary>
        [HttpGet]
        public IActionResult Agregar()
        {
            return View();
        }

        /// <summary>
        /// Procesa la creación de una nueva agencia.
        /// </summary>
        [HttpPost]
        public IActionResult Agregar(AgenciaModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool ok = _agenciaService.InsertarAgencia(model);
            ViewBag.Mensaje = ok ? "Agencia agregada exitosamente." : "Error al agregar agencia.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edita una agencia específica.
        /// </summary>
        [HttpPost]
        public IActionResult Editar(AgenciaModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            _agenciaService.ActualizarAgencia(model);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Elimina una agencia por su código.
        /// </summary>
        public IActionResult Eliminar(int id)
        {
            _agenciaService.EliminarAgencia(id);
            return RedirectToAction("Index");
        }
    }
}
