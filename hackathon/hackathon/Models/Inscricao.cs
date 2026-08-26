namespace hackathon.Models
{
    public class Inscricao
    {
        public int Id { get; set; }
        public int EventoId { get; set; }
        public int AlunoId { get; set; }
        public bool Presente { get; set; }
        public DateTime DataInscricao { get; set; }

        // Auxiliares
        public string? AlunoNome { get; set; }
    }
}