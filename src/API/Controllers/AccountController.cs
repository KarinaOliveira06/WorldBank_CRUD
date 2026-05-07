using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldBank_CRUD.Domain.Entities;
using WorldBank_CRUD.Infrastructure.Data;
using WorldBank_CRUD.API.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace WorldBank_CRUD.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<Account>> CreateAccount(Account account)
        {
            account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password);

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var accountDTO = new AccountResponseDTO
            {
                Id = account.Id,
                Name = account.Name,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                SavingsBalance = account.SavingsBalance
            };

            return Ok(accountDTO);
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDto)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == loginDto.AccountNumber && a.Name == loginDto.Name);

            if (account == null)
                return Unauthorized("Invalid credentials.");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, account.Password);

            if (!isPasswordValid)
                return Unauthorized("Invalid credentials.");

            return Ok(new
            {
                user = account.Name,
                message = "Successfully Logged-in!"
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountResponseDTO>>> GetAccounts()
        {
            var accounts = await _context.Accounts.ToListAsync();

            var accountsDTO = accounts.Select(a => new AccountResponseDTO
            {
                Id = a.Id,
                Name = a.Name,
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                SavingsBalance = a.SavingsBalance
            }).ToList();

            return Ok(accountsDTO);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, AccountUpdateDTO updateDto)
        {
            var account = await _context.Accounts.FindAsync(id);

            if (account == null)
                return NotFound("Account not found.");

            account.Name = updateDto.Name;

            await _context.SaveChangesAsync();

            return Ok("Update Account successfully.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("Transfer")]
        public async Task<IActionResult> Transfer(TransferDTO transferDto)
        {
            if (transferDto.Amount <= 0)
                return BadRequest("The transfer amount must be greater than 0.");

            if (transferDto.SenderId == transferDto.ReceiverId)
                return BadRequest("The transfer must be between different accounts");

            var sender = await _context.Accounts.FindAsync(transferDto.SenderId);
            var receiver = await _context.Accounts.FindAsync(transferDto.ReceiverId);

            if (sender == null || receiver == null)
                return NotFound("User not found.");

            if (sender.Balance < transferDto.Amount)
                return BadRequest("Transfer denied: insufficient funds.");

            sender.Balance -= transferDto.Amount;
            receiver.Balance += transferDto.Amount;

            var senderTransaction = new Transaction
            {
                Type = "TransferOut",
                Amount = transferDto.Amount,
                Description = $"Transfer sent to account ID {receiver.Id}",
                AccountId = sender.Id
            };

            var receiverTransaction = new Transaction
            {
                Type = "TransferIn",
                Amount = transferDto.Amount,
                Description = $"Transfer received from account ID {sender.Id}",
                AccountId = receiver.Id
            };

            _context.Transactions.Add(senderTransaction);
            _context.Transactions.Add(receiverTransaction);

            await _context.SaveChangesAsync();

            return Ok("Successful transfer!");
        }

        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> Deposit(int id, TransactionDTO transactionDto)
        {
            if (transactionDto.Amount <= 0)
                return BadRequest("The deposit amount must be greater than 0.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound("Account not found.");

            account.Balance += transactionDto.Amount;

            var transactionHistory = new Transaction
            {
                Type = "Deposit",
                Amount = transactionDto.Amount,
                Description = "Deposit realized via API",
                AccountId = account.Id
            };

            _context.Transactions.Add(transactionHistory);

            await _context.SaveChangesAsync();

            return Ok($"Deposit successful. New balance: {account.Balance}");
        }

        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> Withdraw(int id, TransactionDTO transactionDto)
        {
            if (transactionDto.Amount <= 0)
                return BadRequest("The withdrawal amount must be greater than 0.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound("Account not found.");

            if (account.Balance < transactionDto.Amount)
                return BadRequest("Withdrawal denied: insufficient funds.");

            account.Balance -= transactionDto.Amount;

            var transactionHistory = new Transaction
            {
                Type = "Withdrawal",
                Amount = transactionDto.Amount,
                Description = "Withdrawal realized via API",
                AccountId = account.Id
            };

            _context.Transactions.Add(transactionHistory);

            await _context.SaveChangesAsync();

            return Ok($"Withdrawal successful. New balance: {account.Balance}");
        }
    }
}