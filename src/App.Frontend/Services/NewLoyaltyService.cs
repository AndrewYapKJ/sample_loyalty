using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Models;
using Microsoft.EntityFrameworkCore;

namespace gussmann_loyalty_program.Services
{
    public class NewLoyaltyService
    {
        private readonly NewLoyaltyDbContext _context;

        public NewLoyaltyService(NewLoyaltyDbContext context)
        {
            _context = context;
        }

        // User methods
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.CurrentTier)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _context.Users
                .Include(u => u.CurrentTier)
                .Include(u => u.LoyaltyPoints)
                .Include(u => u.Appointments)
                // Temporarily commented out UserPackages
                // .Include(u => u.UserPackages)
                .Include(u => u.Redemptions)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.CurrentTier)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Generate referral code
            user.ReferralCode = GenerateReferralCode();
            
            // Set default tier to Bronze
            var bronzeTier = await _context.MembershipTiers
                .FirstOrDefaultAsync(t => t.Name == "Bronze");
            if (bronzeTier != null)
            {
                user.CurrentTierId = bronzeTier.Id;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create user preferences
            var preferences = new UserPreferences
            {
                UserId = user.Id,
                MarketingOptIn = true,
                CommunicationChannel = "Email"
            };
            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return user;
        }

        // Membership Tier methods
        public async Task<List<MembershipTier>> GetAllTiersAsync()
        {
            return await _context.MembershipTiers
                .OrderBy(t => t.TierOrder)
                .ToListAsync();
        }

        // Store methods
        public async Task<List<Store>> GetAllStoresAsync()
        {
            return await _context.Stores
                .Include(s => s.StoreGroup)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // Loyalty Points methods
        public async Task<bool> AddPointsAsync(string userId, int points, string source, string? description = null)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            var loyaltyPoint = new LoyaltyPoint
            {
                UserId = userId,
                Points = points,
                Source = source,
                Description = description ?? $"Points earned from {source}"
            };

            _context.LoyaltyPoints.Add(loyaltyPoint);

            // Update user's total points
            user.TotalPoints += points;

            // Check for tier upgrade
            await CheckAndUpdateUserTier(user);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RedeemPointsAsync(string userId, int points, string rewardId, string? storeId = null)
        {
            var user = await GetUserByIdAsync(userId);
            var reward = await _context.Rewards.FindAsync(rewardId);
            
            if (user == null || reward == null || user.TotalPoints < points) 
                return false;

            // Create redemption record
            var redemption = new Redemption
            {
                UserId = userId,
                RewardId = rewardId,
                PointsRedeemed = points,
                StoreId = storeId,
                Status = "Completed"
            };

            _context.Redemptions.Add(redemption);

            // Create negative loyalty point entry
            var loyaltyPoint = new LoyaltyPoint
            {
                UserId = userId,
                Points = -points,
                Source = "Redemption",
                Description = $"Redeemed {reward.Name}",
                RelatedRedemptionId = redemption.Id
            };

            _context.LoyaltyPoints.Add(loyaltyPoint);

            // Update user's total points
            user.TotalPoints -= points;

            await _context.SaveChangesAsync();
            return true;
        }

        // Appointment methods
        public async Task<List<Appointment>> GetUserAppointmentsAsync(string userId)
        {
            return await _context.Appointments
                .Include(a => a.Store)
                .Include(a => a.Service)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Award points for booking
            await AddPointsAsync(appointment.UserId, 10, "Appointment", "Points for booking appointment");

            return appointment;
        }

        // Dashboard methods
        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync(u => u.IsActive);
        }

        public async Task<int> GetTotalPointsIssuedAsync()
        {
            return await _context.LoyaltyPoints
                .Where(lp => lp.Points > 0)
                .SumAsync(lp => lp.Points);
        }

        public async Task<int> GetTotalPointsRedeemedAsync()
        {
            return await _context.LoyaltyPoints
                .Where(lp => lp.Points < 0)
                .SumAsync(lp => Math.Abs(lp.Points));
        }

        public async Task<List<User>> GetTopUsersByPointsAsync(int count = 10)
        {
            return await _context.Users
                .Include(u => u.CurrentTier)
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.TotalPoints)
                .Take(count)
                .ToListAsync();
        }

        // Service methods
        public async Task<List<Service>> GetAllServicesAsync()
        {
            return await _context.Services
                .Include(s => s.ServiceCategory)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // Reward methods
        public async Task<List<Reward>> GetAllRewardsAsync()
        {
            return await _context.Rewards
                .Include(r => r.Category)
                .Where(r => r.IsActive)
                .OrderBy(r => r.PointsRequired)
                .ToListAsync();
        }

        // Package methods
        public async Task<List<Package>> GetAllPackagesAsync()
        {
            return await _context.Packages
                .Include(p => p.PackageType)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        // Helper methods
        private string GenerateReferralCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task CheckAndUpdateUserTier(User user)
        {
            var tiers = await _context.MembershipTiers
                .OrderByDescending(t => t.MinPoints)
                .ToListAsync();

            var newTier = tiers.FirstOrDefault(t => user.TotalPoints >= t.MinPoints);
            
            if (newTier != null && newTier.Id != user.CurrentTierId)
            {
                // Record tier change
                var tierHistory = new TierHistory
                {
                    UserId = user.Id,
                    TierId = newTier.Id
                };

                _context.TierHistories.Add(tierHistory);
                user.CurrentTierId = newTier.Id;
            }
        }

        // Referral methods
        public async Task<bool> ProcessReferralAsync(string referrerCode, string newUserId)
        {
            var referrer = await _context.Users
                .FirstOrDefaultAsync(u => u.ReferralCode == referrerCode);
            
            if (referrer == null) return false;

            var referral = new Referral
            {
                ReferrerUserId = referrer.Id,
                ReferredUserId = newUserId,
                BonusPoints = 100,
                Status = "Completed"
            };

            _context.Referrals.Add(referral);

            // Award points to referrer
            await AddPointsAsync(referrer.Id, 100, "Referral", "Referral bonus");
            referrer.TotalReferrals++;

            await _context.SaveChangesAsync();
            return true;
        }

        // Payment methods
        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Award points based on amount spent (1 point per dollar)
            var pointsEarned = (int)Math.Floor(payment.Amount);
            await AddPointsAsync(payment.UserId, pointsEarned, "Purchase", $"Points earned from payment of {payment.Amount:C}");

            return payment;
        }
    }
}