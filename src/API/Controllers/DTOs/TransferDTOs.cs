namespace WorldBank_CRUD.API.DTOs;

public class TransferDTO
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public decimal Amount { get; set; }
}