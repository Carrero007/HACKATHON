using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using hackathon.Data;
using hackathon.Models;
using hackathon.Services;

namespace hackathon.Controllers
{
    public class AccountController : Controller
    {
        private readonly Database _db;

        public AccountController(Database db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var conn = _db.GetConnection();
            var cmd = new SqlCommand(
                "SELECT Id, Nome, Senha, Perfil FROM Usuarios WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", model.Email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                ModelState.AddModelError("", "Email ou senha inválidos.");
                return View(model);
            }

            var senhaHash = reader.GetString(reader.GetOrdinal("Senha"));
            if (!SenhaService.Verificar(model.Senha, senhaHash))
            {
                ModelState.AddModelError("", "Email ou senha inválidos.");
                return View(model);
            }

            var usuarioId = reader.GetInt32(reader.GetOrdinal("Id"));
            var nome = reader.GetString(reader.GetOrdinal("Nome"));
            var perfil = reader.GetString(reader.GetOrdinal("Perfil"));
            reader.Close();

            var claims = new List<Claim>
            {
                new Claim("UsuarioId", usuarioId.ToString()),
                new Claim(ClaimTypes.Name, nome),
                new Claim(ClaimTypes.Role, perfil)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Evento");
        }

        [HttpGet]
        public IActionResult Cadastro()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cadastro(CadastroViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Perfil != "ALUNO" && model.Perfil != "CRIADOR")
            {
                ModelState.AddModelError("", "Perfil inválido.");
                return View(model);
            }

            using var conn = _db.GetConnection();

            var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Usuarios WHERE Email = @Email", conn);
            checkCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
            var existe = (int)checkCmd.ExecuteScalar();
            if (existe > 0)
            {
                ModelState.AddModelError("", "Este email já está cadastrado.");
                return View(model);
            }

            try
            {
                var insertCmd = new SqlCommand(
                    @"INSERT INTO Usuarios (Nome, Email, Senha, Perfil)
              VALUES (@Nome, @Email, @Senha, @Perfil)", conn);
                insertCmd.Parameters.AddWithValue("@Nome", model.Nome.Trim());
                insertCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                insertCmd.Parameters.AddWithValue("@Senha", SenhaService.Hash(model.Senha));
                insertCmd.Parameters.AddWithValue("@Perfil", model.Perfil);
                insertCmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // Violação de UNIQUE (email duplicado por concorrência)
                ModelState.AddModelError("", "Este email já está cadastrado.");
                return View(model);
            }

            TempData["Sucesso"] = "Cadastro realizado! Faça login para continuar.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}