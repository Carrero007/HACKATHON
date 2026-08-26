using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using hackathon.Data;
using System.Security.Claims;

namespace hackathon.Controllers
{
    [Authorize(Roles = "ALUNO")]
    public class CertificadoController : Controller
    {
        private readonly Database _db;

        public CertificadoController(Database db)
        {
            _db = db;
        }

        private int UsuarioId => int.Parse(User.FindFirstValue("UsuarioId")!);

        // GET /Certificado/Emitir/{eventoId}
        [HttpGet]
        public IActionResult Emitir(int eventoId)
        {
            using var conn = _db.GetConnection();

            var cmd = new SqlCommand(@"
                SELECT E.Titulo, E.DataHora, I.Presente, U.Nome
                FROM Inscricoes I
                JOIN Eventos E ON E.Id = I.EventoId
                JOIN Usuarios U ON U.Id = I.AlunoId
                WHERE I.EventoId = @EventoId AND I.AlunoId = @AlunoId", conn);
            cmd.Parameters.AddWithValue("@EventoId", eventoId);
            cmd.Parameters.AddWithValue("@AlunoId", UsuarioId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                ViewBag.Disponivel = false;
                ViewBag.Mensagem = "Certificado indisponível: inscrição não encontrada.";
                return View();
            }

            var presente = reader.GetBoolean(reader.GetOrdinal("Presente"));
            if (!presente)
            {
                ViewBag.Disponivel = false;
                ViewBag.Mensagem = "Certificado indisponível: presença não confirmada.";
                return View();
            }

            ViewBag.Disponivel = true;
            ViewBag.NomeAluno = reader.GetString(reader.GetOrdinal("Nome"));
            ViewBag.TituloEvento = reader.GetString(reader.GetOrdinal("Titulo"));
            ViewBag.DataEvento = reader.GetDateTime(reader.GetOrdinal("DataHora"));

            return View();
        }
    }
}