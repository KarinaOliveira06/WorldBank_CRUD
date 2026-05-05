using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldBank_CRUD.Domain.Entities;
using WorldBank_CRUD.Infrastructure.Data;
using WorldBank_CRUD.API.DTOs;

namespace WorldBank_CRUD.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
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
        public async Task<IActionResult> UpdateAccount(int id, Account account)
        {
            if (id != account.Id) return BadRequest("Incompatible ID");

            _context.Entry(account).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Accounts.Any(e => e.Id == id)) return NotFound();
                throw;
            }

            return NoContent();
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

            await _context.SaveChangesAsync();

            return Ok("Successful transfer!");
        }
    }
}