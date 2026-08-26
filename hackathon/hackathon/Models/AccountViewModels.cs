using System.ComponentModel.DataAnnotations;

namespace hackathon.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o email.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        public string Senha { get; set; } = string.Empty;
    }

    public class CadastroViewModel
    {
        [Required(ErrorMessage = "Informe o nome.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o email.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [MinLength(4, ErrorMessage = "A senha deve ter ao menos 4 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione um perfil.")]
        public string Perfil { get; set; } = string.Empty; // ALUNO ou CRIADOR
    }
}