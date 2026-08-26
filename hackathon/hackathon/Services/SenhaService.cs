using System.Security.Cryptography;
using System.Text;

namespace hackathon.Services
{
    public static class SenhaService
    {
        public static string Hash(string senha)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(senha));
            return Convert.ToBase64String(bytes);
        }

        public static bool Verificar(string senha, string hashSalvo)
        {
            return Hash(senha) == hashSalvo;
        }
    }
}