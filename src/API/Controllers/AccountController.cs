using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldBank_CRUD.Domain.Entities;
using WorldBank_CRUD.Infrastructure.Data;
using WorldBank_CRUD.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using WorldBank_CRUD.API.Services;

namespace WorldBank_CRUD.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RabbitMqService _rabbitMqService;
        private readonly TokenService _tokenService;

        public AccountController(AppDbContext context, RabbitMqService rabbitMqService, TokenService tokenService)
        {
            _context = context;
            _rabbitMqService = rabbitMqService;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<Account>> CreateAccount(Account account)
        {
            var accountExists = await _context.Accounts.AnyAsync(a => a.AccountNumber == account.AccountNumber);

            if (accountExists)
            {
                return BadRequest(new { Message = "Account number already in use." });
            }

            account.Password = BCrypt.Net.BCrypt.HashPassword(account.Password);

            if (string.IsNullOrEmpty(account.Role))
            {
                account.Role = "User";
            }

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

            var token = _tokenService.CreateToken(account);

            return Ok(new
            {
                user = account.Name,
                token = token,
                message = "Successfully logged in!"
            });
        }

        [Authorize(Roles = "Admin")]
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            if (!HasPermission(id)) return StatusCode(403, "Access denied.");

            var account = await _context.Accounts.FindAsync(id);

            if (account == null)
                return NotFound("Account not found.");

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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, AccountUpdateDTO updateDto)
        {
            if (!HasPermission(id)) return StatusCode(403, "Access denied.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound("Account not found.");

            account.Name = updateDto.Name;
            await _context.SaveChangesAsync();

            return Ok("Account updated successfully.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            if (!HasPermission(id)) return StatusCode(403, "Access denied.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer(TransferDTO transferDto)
        {
            if (transferDto.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            if (transferDto.SenderId == transferDto.ReceiverId)
                return BadRequest("Sender and receiver must be different.");

            if (!HasPermission(transferDto.SenderId))
                return StatusCode(403, "Permission denied.");

            var sender = await _context.Accounts.FindAsync(transferDto.SenderId);
            var receiver = await _context.Accounts.FindAsync(transferDto.ReceiverId);

            if (sender == null || receiver == null)
                return NotFound("Account not found.");

            if (sender.Balance < transferDto.Amount)
                return BadRequest("Insufficient funds.");

            sender.Balance -= transferDto.Amount;
            receiver.Balance += transferDto.Amount;

            var senderTransaction = new Transaction
            {
                Type = "TransferOut",
                Amount = transferDto.Amount,
                Description = $"Processing transfer to ID {receiver.Id}...",
                AccountId = sender.Id
            };

            var receiverTransaction = new Transaction
            {
                Type = "TransferIn",
                Amount = transferDto.Amount,
                Description = $"Processing transfer from ID {sender.Id}...",
                AccountId = receiver.Id
            };

            _context.Transactions.Add(senderTransaction);
            _context.Transactions.Add(receiverTransaction);
            await _context.SaveChangesAsync();

            var senderMsg = new
            {
                TransactionId = senderTransaction.Id,
                AccountId = sender.Id,
                Type = "TransferOut",
                Amount = transferDto.Amount,
                Message = $"Sent ${transferDto.Amount} to {receiver.Name}."
            };

            var receiverMsg = new
            {
                TransactionId = receiverTransaction.Id,
                AccountId = receiver.Id,
                Type = "TransferIn",
                Amount = transferDto.Amount,
                Message = $"Received ${transferDto.Amount} from {sender.Name}."
            };

            try
            {
                await _rabbitMqService.PublishMessageAsync(senderMsg, "transaction_notifications");
                await _rabbitMqService.PublishMessageAsync(receiverMsg, "transaction_notifications");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ Error: {ex.Message}");
            }

            return Ok("Transfer completed successfully.");
        }

        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> Deposit(int id, TransactionDTO transactionDto)
        {
            if (transactionDto.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            if (!HasPermission(id)) return StatusCode(403, "Permission denied.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound("Account not found.");

            account.Balance += transactionDto.Amount;

            var transactionHistory = new Transaction
            {
                Type = "Deposit",
                Amount = transactionDto.Amount,
                Description = "Processing deposit...",
                AccountId = account.Id
            };

            _context.Transactions.Add(transactionHistory);
            await _context.SaveChangesAsync();

            var depositMsg = new
            {
                TransactionId = transactionHistory.Id,
                AccountId = account.Id,
                Type = "Deposit",
                Amount = transactionDto.Amount,
                Message = $"Successfully deposited ${transactionDto.Amount}."
            };

            try
            {
                await _rabbitMqService.PublishMessageAsync(depositMsg, "transaction_notifications");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ Error: {ex.Message}");
            }

            return Ok($"Deposit successful. New balance: {account.Balance}");
        }

        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> Withdraw(int id, TransactionDTO transactionDto)
        {
            if (transactionDto.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            if (!HasPermission(id)) return StatusCode(403, "Permission denied.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound("Account not found.");

            if (account.Balance < transactionDto.Amount)
                return BadRequest("Insufficient funds.");

            account.Balance -= transactionDto.Amount;

            var transactionHistory = new Transaction
            {
                Type = "Withdrawal",
                Amount = transactionDto.Amount,
                Description = "Processing withdrawal...",
                AccountId = account.Id
            };

            _context.Transactions.Add(transactionHistory);
            await _context.SaveChangesAsync();

            var withdrawMsg = new
            {
                TransactionId = transactionHistory.Id,
                AccountId = account.Id,
                Type = "Withdrawal",
                Amount = transactionDto.Amount,
                Message = $"Successfully withdrew ${transactionDto.Amount}."
            };

            try
            {
                await _rabbitMqService.PublishMessageAsync(withdrawMsg, "transaction_notifications");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ Error: {ex.Message}");
            }

            return Ok($"Withdrawal successful. New balance: {account.Balance}");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/promote")]
        public async Task<IActionResult> PromoteToAdmin(int id)
        {
            var account = await _context.Accounts.FindAsync(id);

            if (account == null)
                return NotFound("Account not found.");

            if (account.Role == "Admin")
                return BadRequest("Account is already an Admin.");

            account.Role = "Admin";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account promoted to Admin." });
        }

        [AllowAnonymous]
        [HttpPost("transactions/{id}/humanize")]
        public async Task<IActionResult> HumanizeTransaction(int id, [FromBody] string aiMessage)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();

            transaction.Description = aiMessage;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id}/transactions")]
        public async Task<ActionResult<IEnumerable<object>>> GetTransactionHistory(int id)
        {
            if (!HasPermission(id)) return StatusCode(403, "Access denied.");

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == id)
                .OrderByDescending(t => t.Id)
                .Take(10)
                .Select(t => new 
                {
                    id = t.Id,
                    type = t.Type,
                    amount = t.Amount,
                    description = t.Description
                })
                .ToListAsync();

            return Ok(transactions);
        }

        private bool HasPermission(int targetAccountId)
        {
            var loggedInUserId = User.FindFirst("AccountId")?.Value;
            if (User.IsInRole("Admin")) return true;
            return loggedInUserId == targetAccountId.ToString();
        }
    }
}