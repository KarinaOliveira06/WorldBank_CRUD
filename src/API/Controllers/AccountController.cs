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

            return Ok(account);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
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
    }
}