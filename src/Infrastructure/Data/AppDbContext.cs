using Microsoft.EntityFrameworkCore;
using WorldBank_CRUD.Domain.Entities;

namespace WorldBank_CRUD.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions <AppDbContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts {get; set;}
        public DbSet<Transaction> Transactions { get; set; }
    }
    
    
}