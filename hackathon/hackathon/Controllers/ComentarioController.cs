using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using hackathon.Data;
using System.Security.Claims;

namespace hackathon.Controllers
{
    [Authorize]
    public class ComentarioController : Controller
    {
        private readonly Database _db;

        public ComentarioController(Database db)
        {
            _db = db;
        }

        private int UsuarioId => int.Parse(User.FindFirstValue("UsuarioId")!);

        // POST /Comentario/Criar
        // POST /Comentario/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(int eventoId, string texto)
        {
            texto = texto?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(texto))
            {
                TempData["Erro"] = "Comentário não pode ser vazio.";
                return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
            }

            if (texto.Length > 1000)
            {
                TempData["Erro"] = "Comentário muito longo (máximo 1000 caracteres).";
                return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
            }

            using var conn = _db.GetConnection();

            var eventoCmd = new SqlCommand("SELECT Publicado FROM Eventos WHERE Id = @EventoId", conn);
            eventoCmd.Parameters.AddWithValue("@EventoId", eventoId);
            var publicadoObj = eventoCmd.ExecuteScalar();

            if (publicadoObj == null)
                return NotFound();

            if (!(bool)publicadoObj)
            {
                TempData["Erro"] = "Não é possível comentar em um evento não publicado.";
                return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
            }

            try
            {
                var insertCmd = new SqlCommand(@"
            INSERT INTO Comentarios (EventoId, UsuarioId, Texto, DataComentario)
            VALUES (@EventoId, @UsuarioId, @Texto, GETDATE())", conn);
                insertCmd.Parameters.AddWithValue("@EventoId", eventoId);
                insertCmd.Parameters.AddWithValue("@UsuarioId", UsuarioId);
                insertCmd.Parameters.AddWithValue("@Texto", texto);
                insertCmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                TempData["Erro"] = "Erro ao salvar comentário. Tente novamente.";
            }

            return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
        }
    }
}