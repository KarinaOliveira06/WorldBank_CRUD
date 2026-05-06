namespace WorldBank_CRUD.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public Account? Account { get; set; }
}