// Classe que representa um lançamento financeiro
public class Lancamento
{
    // Identificador único do lançamento
    public int Id { get; set; } = 0;

    // Valor do lançamento, pode ser positivo (crédito) ou negativo (débito)
    public decimal Valor { get; set; }

    // Tipo do lançamento: Crédito ou Débito
    public TipoLancamento Tipo { get; set; } = TipoLancamento.Credito;

    // Identificador único do cliente
    public int ClientId { get; set; }

    // Data em que o lançamento foi realizado
    public DateTime Data { get; set; }
}

// Enumeração que define os tipos de lançamento
public enum TipoLancamento
{
    Credito,
    Debito
}
