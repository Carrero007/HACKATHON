using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using hackathon.Data;
using System.Security.Claims;

namespace hackathon.Controllers
{
    [Authorize(Roles = "ALUNO")]
    public class InscricaoController : Controller
    {
        private readonly Database _db;

        public InscricaoController(Database db)
        {
            _db = db;
        }

        private int UsuarioId => int.Parse(User.FindFirstValue("UsuarioId")!);

        // POST /Inscricao/Participar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Participar(int eventoId)
        {
            using var conn = _db.GetConnection();

            // Evento existe e está publicado?
            var eventoCmd = new SqlCommand(
                "SELECT COUNT(1) FROM Eventos WHERE Id = @EventoId AND Publicado = 1", conn);
            eventoCmd.Parameters.AddWithValue("@EventoId", eventoId);
            var eventoExiste = (int)eventoCmd.ExecuteScalar() > 0;

            if (!eventoExiste)
            {
                TempData["Erro"] = "Evento não encontrado ou não publicado.";
                return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
            }

            // Já inscrito?
            var checkCmd = new SqlCommand(
                "SELECT COUNT(1) FROM Inscricoes WHERE EventoId = @EventoId AND AlunoId = @AlunoId", conn);
            checkCmd.Parameters.AddWithValue("@EventoId", eventoId);
            checkCmd.Parameters.AddWithValue("@AlunoId", UsuarioId);
            var jaInscrito = (int)checkCmd.ExecuteScalar() > 0;

            if (jaInscrito)
            {
                TempData["Erro"] = "Você já está inscrito.";
                return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
            }

            // Criar inscrição
            var insertCmd = new SqlCommand(@"
                INSERT INTO Inscricoes (EventoId, AlunoId, Presente, DataInscricao)
                VALUES (@EventoId, @AlunoId, 0, GETDATE())", conn);
            insertCmd.Parameters.AddWithValue("@EventoId", eventoId);
            insertCmd.Parameters.AddWithValue("@AlunoId", UsuarioId);
            insertCmd.ExecuteNonQuery();

            TempData["Sucesso"] = "Inscrição realizada!";
            return RedirectToAction("Detalhes", "Evento", new { id = eventoId });
        }

        // GET /Inscricao/MinhasInscricoes
        public IActionResult MinhasInscricoes()
        {
            var lista = new List<hackathon.Models.Evento>();
            using var conn = _db.GetConnection();
            var cmd = new SqlCommand(@"
                SELECT E.Id, E.Titulo, E.DataHora, E.Local, I.Presente
                FROM Inscricoes I
                JOIN Eventos E ON E.Id = I.EventoId
                WHERE I.AlunoId = @AlunoId
                ORDER BY E.DataHora", conn);
            cmd.Parameters.AddWithValue("@AlunoId", UsuarioId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new hackathon.Models.Evento
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Titulo = reader.GetString(reader.GetOrdinal("Titulo")),
                    DataHora = reader.GetDateTime(reader.GetOrdinal("DataHora")),
                    Local = reader.IsDBNull(reader.GetOrdinal("Local")) ? null : reader.GetString(reader.GetOrdinal("Local"))
                });
            }

            return View(lista);
        }
    }
}