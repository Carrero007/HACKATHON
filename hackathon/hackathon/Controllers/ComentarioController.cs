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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(int eventoId, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                TempData["Erro"] = "Comentário não pode ser vazio.";
                return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
            }

            using var conn = _db.GetConnection();

            var eventoCmd = new SqlCommand("SELECT COUNT(1) FROM Eventos WHERE Id = @EventoId", conn);
            eventoCmd.Parameters.AddWithValue("@EventoId", eventoId);
            if ((int)eventoCmd.ExecuteScalar() == 0)
                return NotFound();

            var insertCmd = new SqlCommand(@"
                INSERT INTO Comentarios (EventoId, UsuarioId, Texto, DataComentario)
                VALUES (@EventoId, @UsuarioId, @Texto, GETDATE())", conn);
            insertCmd.Parameters.AddWithValue("@EventoId", eventoId);
            insertCmd.Parameters.AddWithValue("@UsuarioId", UsuarioId);
            insertCmd.Parameters.AddWithValue("@Texto", texto);
            insertCmd.ExecuteNonQuery();

            return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
        }
    }
}