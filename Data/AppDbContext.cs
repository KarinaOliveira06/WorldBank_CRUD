using Microsoft.EntityFrameworkCore;
using WorldBank.Entities;

namespace WorldBank_CRUD.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(AppDbContextOptions <AppDbContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts {get; set;}
    }
}