using Microsoft.EntityFrameworkCore;
using WorldBank_CRUD.Entities;

namespace WorldBank_CRUD.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions <AppDbContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts {get; set;}
    }
}