namespace hackathon.Models
{
    public class Comentario
    {
        public int Id { get; set; }
        public int EventoId { get; set; }
        public int UsuarioId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public DateTime DataComentario { get; set; }

        // Auxiliar
        public string? UsuarioNome { get; set; }
    }
}