namespace WorldBank_CRUD.API.DTOs
{
    public class LoginDTO
    {
        public int AccountNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}