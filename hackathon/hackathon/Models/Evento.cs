using System.ComponentModel.DataAnnotations;

namespace hackathon.Models
{
    public class Evento
    {
        public int Id { get; set; }
        public int CriadorId { get; set; }

        [Required(ErrorMessage = "Informe o título.")]
        [StringLength(200, ErrorMessage = "Título deve ter no máximo 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "Descrição deve ter no máximo 4000 caracteres.")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Informe a data e hora.")]
        public DateTime DataHora { get; set; }

        [StringLength(200, ErrorMessage = "Local deve ter no máximo 200 caracteres.")]
        public string? Local { get; set; }

        public bool Publicado { get; set; } = true;

        // Auxiliares (não persistidos)
        public string? CriadorNome { get; set; }
        public bool JaInscrito { get; set; }
    }
}