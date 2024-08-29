namespace ConsolidadoDiario.Models
{
  
        public class Lancamento
        {
            public int Id { get; set; }
            public int ClientId { get; set; }
            public decimal Valor { get; set; }
            public DateTime Data { get; set; }
            public int Tipo { get; set; }
        }
    
}
