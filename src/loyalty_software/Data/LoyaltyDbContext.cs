using Microsoft.EntityFrameworkCore;
using gussmann_loyalty_program.Models;

namespace gussmann_loyalty_program.Data
{
    public class LoyaltyDbContext : DbContext
    {
        public LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.DateJoined).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure LoyaltyAccount entity
            modelBuilder.Entity<LoyaltyAccount>(entity =>
            {
                entity.HasIndex(e => e.AccountNumber).IsUnique();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.TierLevel).HasDefaultValue("Bronze");
                
                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.LoyaltyAccounts)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PointTransaction entity
            modelBuilder.Entity<PointTransaction>(entity =>
            {
                entity.Property(e => e.TransactionDate).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.LoyaltyAccount)
                    .WithMany(p => p.PointTransactions)
                    .HasForeignKey(d => d.LoyaltyAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.TransactionNumber).IsUnique();
                entity.Property(e => e.TransactionDate).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Customers
            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    PhoneNumber = "555-0123",
                    Address = "123 Main St",
                    City = "Anytown",
                    State = "CA",
                    ZipCode = "12345",
                    DateJoined = new DateTime(2024, 1, 15),
                    IsActive = true
                },
                new Customer
                {
                    Id = 2,
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com",
                    PhoneNumber = "555-0456",
                    Address = "456 Oak Ave",
                    City = "Somewhere",
                    State = "NY",
                    ZipCode = "67890",
                    DateJoined = new DateTime(2024, 2, 20),
                    IsActive = true
                }
            );

            // Seed LoyaltyAccounts
            modelBuilder.Entity<LoyaltyAccount>().HasData(
                new LoyaltyAccount
                {
                    Id = 1,
                    CustomerId = 1,
                    AccountNumber = "LOY001001",
                    PointsBalance = 2500,
                    LifetimePointsEarned = 3500,
                    LifetimePointsRedeemed = 1000,
                    TierLevel = "Silver",
                    CreatedDate = new DateTime(2024, 1, 15),
                    LastActivityDate = new DateTime(2024, 3, 1),
                    IsActive = true
                },
                new LoyaltyAccount
                {
                    Id = 2,
                    CustomerId = 2,
                    AccountNumber = "LOY001002",
                    PointsBalance = 1200,
                    LifetimePointsEarned = 1200,
                    LifetimePointsRedeemed = 0,
                    TierLevel = "Bronze",
                    CreatedDate = new DateTime(2024, 2, 20),
                    LastActivityDate = new DateTime(2024, 2, 25),
                    IsActive = true
                }
            );
        }
    }
}