using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using hackathon.Data;
using hackathon.Models;
using System.Security.Claims;

namespace hackathon.Controllers
{
    public class EventoController : Controller
    {
        private readonly Database _db;

        public EventoController(Database db)
        {
            _db = db;
        }

        private int? UsuarioId =>
            User.Identity!.IsAuthenticated
                ? int.Parse(User.FindFirstValue("UsuarioId")!)
                : null;

        private string? Perfil =>
            User.Identity!.IsAuthenticated
                ? User.FindFirstValue(ClaimTypes.Role)
                : null;

        // GET /Evento
        public IActionResult Index()
        {
            var lista = new List<Evento>();
            using var conn = _db.GetConnection();
            var cmd = new SqlCommand(@"
                SELECT E.Id, E.CriadorId, E.Titulo, E.Descricao, E.DataHora, E.Local, E.Publicado, U.Nome AS CriadorNome
                FROM Eventos E
                JOIN Usuarios U ON U.Id = E.CriadorId
                WHERE E.Publicado = 1
                ORDER BY E.DataHora", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Evento
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CriadorId = reader.GetInt32(reader.GetOrdinal("CriadorId")),
                    Titulo = reader.GetString(reader.GetOrdinal("Titulo")),
                    Descricao = reader.IsDBNull(reader.GetOrdinal("Descricao")) ? null : reader.GetString(reader.GetOrdinal("Descricao")),
                    DataHora = reader.GetDateTime(reader.GetOrdinal("DataHora")),
                    Local = reader.IsDBNull(reader.GetOrdinal("Local")) ? null : reader.GetString(reader.GetOrdinal("Local")),
                    Publicado = reader.GetBoolean(reader.GetOrdinal("Publicado")),
                    CriadorNome = reader.GetString(reader.GetOrdinal("CriadorNome"))
                });
            }

            return View(lista);
        }

        // GET /Evento/Detalhes/{id}
        public IActionResult Detalhes(int id)
        {
            Evento? evento = null;
            using var conn = _db.GetConnection();

            var cmd = new SqlCommand(@"
                SELECT E.Id, E.CriadorId, E.Titulo, E.Descricao, E.DataHora, E.Local, E.Publicado, U.Nome AS CriadorNome
                FROM Eventos E
                JOIN Usuarios U ON U.Id = E.CriadorId
                WHERE E.Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    evento = new Evento
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        CriadorId = reader.GetInt32(reader.GetOrdinal("CriadorId")),
                        Titulo = reader.GetString(reader.GetOrdinal("Titulo")),
                        Descricao = reader.IsDBNull(reader.GetOrdinal("Descricao")) ? null : reader.GetString(reader.GetOrdinal("Descricao")),
                        DataHora = reader.GetDateTime(reader.GetOrdinal("DataHora")),
                        Local = reader.IsDBNull(reader.GetOrdinal("Local")) ? null : reader.GetString(reader.GetOrdinal("Local")),
                        Publicado = reader.GetBoolean(reader.GetOrdinal("Publicado")),
                        CriadorNome = reader.GetString(reader.GetOrdinal("CriadorNome"))
                    };
                }
            }

            if (evento == null) return NotFound();

            if (UsuarioId.HasValue && Perfil == "ALUNO")
            {
                var checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM Inscricoes WHERE EventoId = @EventoId AND AlunoId = @AlunoId", conn);
                checkCmd.Parameters.AddWithValue("@EventoId", id);
                checkCmd.Parameters.AddWithValue("@AlunoId", UsuarioId.Value);
                evento.JaInscrito = (int)checkCmd.ExecuteScalar() > 0;
            }

            var comentarios = new List<Comentario>();
            var comCmd = new SqlCommand(@"
    SELECT C.Id, C.EventoId, C.UsuarioId, C.Texto, C.DataComentario, U.Nome AS UsuarioNome
    FROM Comentarios C
    JOIN Usuarios U ON U.Id = C.UsuarioId
    WHERE C.EventoId = @EventoId
    ORDER BY C.DataComentario", conn);
            comCmd.Parameters.AddWithValue("@EventoId", id);

            using (var comReader = comCmd.ExecuteReader())
            {
                while (comReader.Read())
                {
                    comentarios.Add(new Comentario
                    {
                        Id = comReader.GetInt32(comReader.GetOrdinal("Id")),
                        EventoId = comReader.GetInt32(comReader.GetOrdinal("EventoId")),
                        UsuarioId = comReader.GetInt32(comReader.GetOrdinal("UsuarioId")),
                        Texto = comReader.GetString(comReader.GetOrdinal("Texto")),
                        DataComentario = comReader.GetDateTime(comReader.GetOrdinal("DataComentario")),
                        UsuarioNome = comReader.GetString(comReader.GetOrdinal("UsuarioNome"))
                    });
                }
            }

            ViewBag.Comentarios = comentarios;
            return View(evento);
        }

        // GET /Evento/Criar
        [Authorize(Roles = "CRIADOR")]
        public IActionResult Criar()
        {
            return View();
        }

        // POST /Evento/Criar
        [HttpPost]
        [Authorize(Roles = "CRIADOR")]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(Evento model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.DataHora < DateTime.Now)
            {
                ModelState.AddModelError("DataHora", "A data do evento não pode estar no passado.");
                return View(model);
            }

            try
            {
                using var conn = _db.GetConnection();
                var cmd = new SqlCommand(@"
            INSERT INTO Eventos (CriadorId, Titulo, Descricao, DataHora, Local, Publicado)
            VALUES (@CriadorId, @Titulo, @Descricao, @DataHora, @Local, 1)", conn);
                cmd.Parameters.AddWithValue("@CriadorId", UsuarioId!.Value);
                cmd.Parameters.AddWithValue("@Titulo", model.Titulo.Trim());
                cmd.Parameters.AddWithValue("@Descricao", (object?)model.Descricao?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataHora", model.DataHora);
                cmd.Parameters.AddWithValue("@Local", (object?)model.Local?.Trim() ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Erro ao salvar o evento. Tente novamente.");
                return View(model);
            }

            TempData["Sucesso"] = "Evento criado com sucesso!";
            return RedirectToAction("Index");
        }

        // GET /Evento/Editar/{id}
        [Authorize(Roles = "CRIADOR")]
        public IActionResult Editar(int id)
        {
            var evento = BuscarEventoSimples(id);
            if (evento == null) return NotFound();

            if (evento.CriadorId != UsuarioId!.Value)
                return Forbid();

            return View(evento);
        }

        // POST /Evento/Editar/{id}
        // POST /Evento/Editar/{id}
        [HttpPost]
        [Authorize(Roles = "CRIADOR")]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(int id, Evento model)
        {
            if (!ModelState.IsValid)
            {
                model.Id = id;
                return View(model);
            }

            if (model.DataHora < DateTime.Now)
            {
                ModelState.AddModelError("DataHora", "A data do evento não pode estar no passado.");
                model.Id = id;
                return View(model);
            }

            var eventoAtual = BuscarEventoSimples(id);
            if (eventoAtual == null) return NotFound();

            if (eventoAtual.CriadorId != UsuarioId!.Value)
                return Forbid();

            try
            {
                using var conn = _db.GetConnection();
                var cmd = new SqlCommand(@"
            UPDATE Eventos
            SET Titulo = @Titulo, Descricao = @Descricao, DataHora = @DataHora, Local = @Local
            WHERE Id = @Id AND CriadorId = @CriadorId", conn);
                cmd.Parameters.AddWithValue("@Titulo", model.Titulo.Trim());
                cmd.Parameters.AddWithValue("@Descricao", (object?)model.Descricao?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataHora", model.DataHora);
                cmd.Parameters.AddWithValue("@Local", (object?)model.Local?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@CriadorId", UsuarioId.Value);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Erro ao salvar as alterações. Tente novamente.");
                model.Id = id;
                return View(model);
            }

            TempData["Sucesso"] = "Evento atualizado com sucesso!";
            return RedirectToAction("Detalhes", new { id });
        }

        // POST /Evento/Excluir/{id}
        [HttpPost]
        [Authorize(Roles = "CRIADOR")]
        [ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            var evento = BuscarEventoSimples(id);
            if (evento == null) return NotFound();

            if (evento.CriadorId != UsuarioId!.Value)
                return Forbid();

            using var conn = _db.GetConnection();
            var cmd = new SqlCommand(
                "DELETE FROM Eventos WHERE Id = @Id AND CriadorId = @CriadorId", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@CriadorId", UsuarioId.Value);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        // GET /Evento/MeusEventos
        [Authorize(Roles = "CRIADOR")]
        public IActionResult MeusEventos()
        {
            var lista = new List<Evento>();
            using var conn = _db.GetConnection();
            var cmd = new SqlCommand(
                "SELECT Id, CriadorId, Titulo, Descricao, DataHora, Local, Publicado FROM Eventos WHERE CriadorId = @CriadorId ORDER BY DataHora", conn);
            cmd.Parameters.AddWithValue("@CriadorId", UsuarioId!.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Evento
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CriadorId = reader.GetInt32(reader.GetOrdinal("CriadorId")),
                    Titulo = reader.GetString(reader.GetOrdinal("Titulo")),
                    Descricao = reader.IsDBNull(reader.GetOrdinal("Descricao")) ? null : reader.GetString(reader.GetOrdinal("Descricao")),
                    DataHora = reader.GetDateTime(reader.GetOrdinal("DataHora")),
                    Local = reader.IsDBNull(reader.GetOrdinal("Local")) ? null : reader.GetString(reader.GetOrdinal("Local")),
                    Publicado = reader.GetBoolean(reader.GetOrdinal("Publicado"))
                });
            }

            return View(lista);
        }

        private Evento? BuscarEventoSimples(int id)
        {
            using var conn = _db.GetConnection();
            var cmd = new SqlCommand(
                "SELECT Id, CriadorId, Titulo, Descricao, DataHora, Local, Publicado FROM Eventos WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new Evento
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                CriadorId = reader.GetInt32(reader.GetOrdinal("CriadorId")),
                Titulo = reader.GetString(reader.GetOrdinal("Titulo")),
                Descricao = reader.IsDBNull(reader.GetOrdinal("Descricao")) ? null : reader.GetString(reader.GetOrdinal("Descricao")),
                DataHora = reader.GetDateTime(reader.GetOrdinal("DataHora")),
                Local = reader.IsDBNull(reader.GetOrdinal("Local")) ? null : reader.GetString(reader.GetOrdinal("Local")),
                Publicado = reader.GetBoolean(reader.GetOrdinal("Publicado"))
            };
        }
    }
}