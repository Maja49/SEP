using bank.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace bank.Api.DbContext
{
    public class BankDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

        public DbSet<Card> Cards { get; set; } = null!;
    }
}
