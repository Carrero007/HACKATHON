using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using hackathon.Data;
using System.Security.Claims;

namespace hackathon.Controllers
{
    [Authorize(Roles = "ALUNO")]
    public class PresencaController : Controller
    {
        private readonly Database _db;

        public PresencaController(Database db)
        {
            _db = db;
        }

        private int UsuarioId => int.Parse(User.FindFirstValue("UsuarioId")!);

        // GET /Presenca/Confirmar?eventoId=10
        [HttpGet]
        public IActionResult Confirmar(int eventoId)
        {
            using var conn = _db.GetConnection();

            var cmd = new SqlCommand("SELECT Titulo FROM Eventos WHERE Id = @EventoId", conn);
            cmd.Parameters.AddWithValue("@EventoId", eventoId);
            var tituloObj = cmd.ExecuteScalar();

            if (tituloObj == null) return NotFound();

            ViewBag.EventoId = eventoId;
            ViewBag.TituloEvento = (string)tituloObj;

            var checkCmd = new SqlCommand(
                "SELECT Presente FROM Inscricoes WHERE EventoId = @EventoId AND AlunoId = @AlunoId", conn);
            checkCmd.Parameters.AddWithValue("@EventoId", eventoId);
            checkCmd.Parameters.AddWithValue("@AlunoId", UsuarioId);
            var presenteObj = checkCmd.ExecuteScalar();

            ViewBag.Inscrito = presenteObj != null;
            ViewBag.JaConfirmado = presenteObj != null && (bool)presenteObj;

            return View();
        }

        // POST /Presenca/Confirmar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Confirmar")]
        public IActionResult ConfirmarPost(int eventoId)
        {
            using var conn = _db.GetConnection();

            var checkCmd = new SqlCommand(
                "SELECT COUNT(1) FROM Inscricoes WHERE EventoId = @EventoId AND AlunoId = @AlunoId", conn);
            checkCmd.Parameters.AddWithValue("@EventoId", eventoId);
            checkCmd.Parameters.AddWithValue("@AlunoId", UsuarioId);
            var inscrito = (int)checkCmd.ExecuteScalar() > 0;

            if (!inscrito)
            {
                TempData["Erro"] = "Aluno não está inscrito.";
                return RedirectToAction("Confirmar", new { eventoId });
            }

            var updateCmd = new SqlCommand(@"
                UPDATE Inscricoes SET Presente = 1
                WHERE EventoId = @EventoId AND AlunoId = @AlunoId", conn);
            updateCmd.Parameters.AddWithValue("@EventoId", eventoId);
            updateCmd.Parameters.AddWithValue("@AlunoId", UsuarioId);
            updateCmd.ExecuteNonQuery();

            TempData["Sucesso"] = "Presença confirmada!";
            return RedirectToAction("Confirmar", new { eventoId });
        }
    }
}