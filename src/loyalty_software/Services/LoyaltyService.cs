using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Models;
using Microsoft.EntityFrameworkCore;

namespace loyalty_sfotware.Services
{
    public class LoyaltyService
    {
        private readonly LoyaltyDbContext _context;

        public LoyaltyService(LoyaltyDbContext context)
        {
            _context = context;
        }

        // Customer methods
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .Include(c => c.LoyaltyAccounts)
                .OrderBy(c => c.LastName)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.LoyaltyAccounts)
                .ThenInclude(la => la.PointTransactions)
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            return await _context.Customers
                .Include(c => c.LoyaltyAccounts)
                .FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Create a loyalty account for the new customer
            var loyaltyAccount = new LoyaltyAccount
            {
                CustomerId = customer.Id,
                AccountNumber = GenerateAccountNumber(),
                CreatedDate = DateTime.UtcNow
            };

            _context.LoyaltyAccounts.Add(loyaltyAccount);
            await _context.SaveChangesAsync();

            return customer;
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            customer.LastModified = DateTime.UtcNow;
            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return customer;
        }

        // Loyalty Account methods
        public async Task<LoyaltyAccount?> GetLoyaltyAccountAsync(int customerId)
        {
            return await _context.LoyaltyAccounts
                .Include(la => la.PointTransactions)
                .Include(la => la.Customer)
                .FirstOrDefaultAsync(la => la.CustomerId == customerId && la.IsActive);
        }

        public async Task<LoyaltyAccount?> GetLoyaltyAccountByNumberAsync(string accountNumber)
        {
            return await _context.LoyaltyAccounts
                .Include(la => la.PointTransactions)
                .Include(la => la.Customer)
                .FirstOrDefaultAsync(la => la.AccountNumber == accountNumber && la.IsActive);
        }

        // Point Transaction methods
        public async Task<bool> AddPointsAsync(int customerId, decimal points, string description = "Points earned")
        {
            var loyaltyAccount = await GetLoyaltyAccountAsync(customerId);
            if (loyaltyAccount == null) return false;

            var pointTransaction = new PointTransaction
            {
                LoyaltyAccountId = loyaltyAccount.Id,
                Points = points,
                TransactionType = PointTransactionType.Earned,
                Description = description,
                TransactionDate = DateTime.UtcNow
            };

            _context.PointTransactions.Add(pointTransaction);

            // Update loyalty account balances
            loyaltyAccount.PointsBalance += points;
            loyaltyAccount.LifetimePointsEarned += points;
            loyaltyAccount.LastActivityDate = DateTime.UtcNow;

            // Update tier level based on lifetime points
            UpdateTierLevel(loyaltyAccount);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RedeemPointsAsync(int customerId, decimal points, string description = "Points redeemed")
        {
            var loyaltyAccount = await GetLoyaltyAccountAsync(customerId);
            if (loyaltyAccount == null || loyaltyAccount.PointsBalance < points) return false;

            var pointTransaction = new PointTransaction
            {
                LoyaltyAccountId = loyaltyAccount.Id,
                Points = -points,
                TransactionType = PointTransactionType.Redeemed,
                Description = description,
                TransactionDate = DateTime.UtcNow
            };

            _context.PointTransactions.Add(pointTransaction);

            // Update loyalty account balances
            loyaltyAccount.PointsBalance -= points;
            loyaltyAccount.LifetimePointsRedeemed += points;
            loyaltyAccount.LastActivityDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<PointTransaction>> GetPointTransactionsAsync(int customerId, int? limit = null)
        {
            var loyaltyAccount = await GetLoyaltyAccountAsync(customerId);
            if (loyaltyAccount == null) return new List<PointTransaction>();

            var query = _context.PointTransactions
                .Where(pt => pt.LoyaltyAccountId == loyaltyAccount.Id)
                .OrderByDescending(pt => pt.TransactionDate);

            if (limit.HasValue)
                query = (IOrderedQueryable<PointTransaction>)query.Take(limit.Value);

            return await query.ToListAsync();
        }

        // Transaction methods
        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            transaction.TransactionNumber = GenerateTransactionNumber();
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<List<Transaction>> GetCustomerTransactionsAsync(int customerId)
        {
            return await _context.Transactions
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // Dashboard methods
        public async Task<int> GetTotalCustomersAsync()
        {
            return await _context.Customers.CountAsync(c => c.IsActive);
        }

        public async Task<decimal> GetTotalPointsIssuedAsync()
        {
            return await _context.LoyaltyAccounts.SumAsync(la => la.LifetimePointsEarned);
        }

        public async Task<decimal> GetTotalPointsRedeemedAsync()
        {
            return await _context.LoyaltyAccounts.SumAsync(la => la.LifetimePointsRedeemed);
        }

        public async Task<List<LoyaltyAccount>> GetTopCustomersByPointsAsync(int count = 10)
        {
            return await _context.LoyaltyAccounts
                .Include(la => la.Customer)
                .Where(la => la.IsActive)
                .OrderByDescending(la => la.PointsBalance)
                .Take(count)
                .ToListAsync();
        }

        // Helper methods
        private string GenerateAccountNumber()
        {
            var random = new Random();
            var number = random.Next(100000, 999999);
            return $"LOY{DateTime.Now.Year}{number:D6}";
        }

        private string GenerateTransactionNumber()
        {
            var random = new Random();
            var number = random.Next(100000, 999999);
            return $"TXN{DateTime.Now:yyyyMMdd}{number:D6}";
        }

        private void UpdateTierLevel(LoyaltyAccount account)
        {
            if (account.LifetimePointsEarned >= 10000)
                account.TierLevel = "Platinum";
            else if (account.LifetimePointsEarned >= 5000)
                account.TierLevel = "Gold";
            else if (account.LifetimePointsEarned >= 2000)
                account.TierLevel = "Silver";
            else
                account.TierLevel = "Bronze";
        }
    }
}