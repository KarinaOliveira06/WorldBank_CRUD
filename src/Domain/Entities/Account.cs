namespace WorldBank_CRUD.Domain.Entities
{
    public class Account
    {
        public int Id {get; set;}
        public string Name {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
        public int AccountNumber {get; set;}
        public decimal Balance {get; set;}
        public decimal SavingsBalance {get; set;}

        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}