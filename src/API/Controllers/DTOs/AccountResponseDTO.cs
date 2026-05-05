namespace WorldBank_CRUD.API.DTOs
{
    public class AccountResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public decimal SavingsBalance { get; set; }
    }
}