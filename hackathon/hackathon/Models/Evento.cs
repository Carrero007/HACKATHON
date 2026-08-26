namespace hackathon.Models
{
    public class Evento
    {
        public int Id { get; set; }
        public int CriadorId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime DataHora { get; set; }
        public string? Local { get; set; }
        public bool Publicado { get; set; } = true;

        // Auxiliares (não persistidos)
        public string? CriadorNome { get; set; }
        public bool JaInscrito { get; set; }
    }
}