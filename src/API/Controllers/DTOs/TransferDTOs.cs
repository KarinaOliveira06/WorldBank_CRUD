namespace WorldBank_CRUD.API.DTOs;

public class TransferDTO
{
    public int SenderId { get; set; }    // ID de quem envia o dinheiro
    public int ReceiverId { get; set; }  // ID de quem recebe o dinheiro
    public decimal Amount { get; set; }  // Valor a transferir
}