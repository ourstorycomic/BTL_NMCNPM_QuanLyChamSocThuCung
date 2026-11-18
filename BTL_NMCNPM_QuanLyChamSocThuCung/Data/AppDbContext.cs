using Microsoft.EntityFrameworkCore;
using BTL_NMCNPM_QuanLyChamSocThuCung.Models;

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình quan hệ 1-1 giữa Account và Customer
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Customer)
                .WithOne(c => c.Account)
                .HasForeignKey<Customer>(c => c.AccountId);
        }
    }
}