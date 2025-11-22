using Microsoft.EntityFrameworkCore;
using gussmann_loyalty_program.Models;

namespace gussmann_loyalty_program.Data
{
    public class NewLoyaltyDbContext : DbContext
    {
        public NewLoyaltyDbContext(DbContextOptions<NewLoyaltyDbContext> options) : base(options)
        {
        }

        // Core entities
        public DbSet<User> Users { get; set; }
        public DbSet<MembershipTier> MembershipTiers { get; set; }
        
        // Admin authentication
        public DbSet<Admin> Admins { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<AccessToken> AccessTokens { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreGroup> StoreGroups { get; set; }
        public DbSet<StoreSettings> StoreSettings { get; set; }
        public DbSet<StoreHour> StoreHours { get; set; }

        // Services and appointments
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        // Payments
        public DbSet<Payment> Payments { get; set; }

        // Loyalty and rewards
        public DbSet<LoyaltyPoint> LoyaltyPoints { get; set; }
        public DbSet<PointExpiry> PointExpiries { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<RewardCategory> RewardCategories { get; set; }
        public DbSet<RewardInventory> RewardInventory { get; set; }
        public DbSet<RewardRedemptionMethod> RewardRedemptionMethods { get; set; }
        public DbSet<Redemption> Redemptions { get; set; }

        // Referrals
        public DbSet<Referral> Referrals { get; set; }
        public DbSet<ReferralCampaign> ReferralCampaigns { get; set; }

        // Packages
        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageType> PackageTypes { get; set; }
        public DbSet<UserPackage> UserPackages { get; set; }
        public DbSet<PackageUsage> PackageUsages { get; set; }

        // Events
        public DbSet<Event> Events { get; set; }
        public DbSet<EventReward> EventRewards { get; set; }
        public DbSet<UserEventParticipation> UserEventParticipations { get; set; }

        // Supporting entities
        public DbSet<Integration> Integrations { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<TierHistory> TierHistories { get; set; }

        // User-related entities
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<UserConsent> UserConsents { get; set; }
        public DbSet<UserFeedback> UserFeedback { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure all entities WITHOUT foreign key constraints to avoid cascade conflicts
            // All relationships are maintained via navigation properties but no database-level constraints

            // Configure basic entity properties and indices only
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.RegistrationDate).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.ReferralCode).IsUnique();
                entity.Property(e => e.DateOfBirth).HasColumnType("date");
                // No foreign key constraints
            });

            // Configure Admin authentication entities - minimal constraints
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<AccessToken>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.RefreshTokenExpiresAt).IsRequired();
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.RefreshToken).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.UserType);
                entity.HasIndex(e => e.IsActive);
                // No foreign key constraints
            });

            // Configure all other entities with basic property settings only
            ConfigureEntityBasics(modelBuilder);
        }

        private void ConfigureEntityBasics(ModelBuilder modelBuilder)
        {
            // Configure basic properties and default values for all entities
            // NO FOREIGN KEY CONSTRAINTS to avoid cascade issues

            modelBuilder.Entity<MembershipTier>(entity =>
            {
                entity.HasIndex(e => e.TierOrder).IsUnique();
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.City);
            });

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.AppointmentTime).HasColumnType("datetime2");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StoreId);
                entity.HasIndex(e => e.AppointmentTime);
                // Ignore ALL navigation properties
                entity.Ignore(e => e.User);
                entity.Ignore(e => e.Store);
                entity.Ignore(e => e.Service);
                entity.Ignore(e => e.Payment);
                entity.Ignore(e => e.PackageUsage);
                entity.Ignore(e => e.Integration);
                entity.Ignore(e => e.LoyaltyPoints);
                entity.Ignore(e => e.Feedback);
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                // Ignore navigation properties
                entity.Ignore(e => e.ServiceCategory);
                entity.Ignore(e => e.Appointments);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.TransactionDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StoreId);
                entity.HasIndex(e => e.TransactionDate);
                // Ignore ALL navigation properties
                entity.Ignore(e => e.User);
                entity.Ignore(e => e.Store);
                entity.Ignore(e => e.Appointment);
                entity.Ignore(e => e.PackagePurchase);
                entity.Ignore(e => e.AppointmentsWithPayment);
                entity.Ignore(e => e.UserPackages);
            });

            modelBuilder.Entity<LoyaltyPoint>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Source);
                entity.HasIndex(e => e.CreatedAt);
                // Ignore navigation properties
                entity.Ignore(e => e.User);
                entity.Ignore(e => e.RelatedAppointment);
                entity.Ignore(e => e.RelatedRedemption);
                entity.Ignore(e => e.PointExpiries);
            });

            modelBuilder.Entity<Reward>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.PointsRequired);
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.StartDate).HasColumnType("date");
                entity.Property(e => e.EndDate).HasColumnType("date");
                // Ignore navigation properties
                entity.Ignore(e => e.Category);
                entity.Ignore(e => e.Redemptions);
                entity.Ignore(e => e.EventRewards);
            });

            modelBuilder.Entity<Redemption>(entity =>
            {
                entity.Property(e => e.RedemptionDate).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RewardId);
                entity.HasIndex(e => e.RedemptionDate);
                // Ignore navigation properties
                entity.Ignore(e => e.User);
                entity.Ignore(e => e.Reward);
                entity.Ignore(e => e.Store);
                entity.Ignore(e => e.Method);
            });

            modelBuilder.Entity<Referral>(entity =>
            {
                entity.Property(e => e.ReferralDate).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.ReferrerUserId);
                entity.HasIndex(e => e.ReferredUserId);
                entity.HasIndex(e => e.ReferralDate);
                // Ignore navigation properties
                entity.Ignore(e => e.ReferrerUser);
                entity.Ignore(e => e.ReferredUser);
                entity.Ignore(e => e.ReferralCampaign);
            });

            modelBuilder.Entity<Package>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                // Ignore navigation properties
                entity.Ignore(e => e.PackageType);
                entity.Ignore(e => e.UserPackages);
            });

            modelBuilder.Entity<UserPackage>(entity =>
            {
                entity.Property(e => e.PurchaseDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.ExpiryDate).HasColumnType("date");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.PackageId);
                entity.HasIndex(e => e.ExpiryDate);
                // Ignore ALL navigation properties
                entity.Ignore(e => e.User);
                entity.Ignore(e => e.Package);
                entity.Ignore(e => e.Payment);
                entity.Ignore(e => e.PackageUsages);
                entity.Ignore(e => e.PaymentsForPackage);
            });

            modelBuilder.Entity<PackageUsage>(entity =>
            {
                entity.Property(e => e.UsedOn).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.UserPackageId);
                entity.HasIndex(e => e.UsedOn);
                // Ignore navigation properties
                entity.Ignore(e => e.UserPackage);
                entity.Ignore(e => e.AppointmentsWithUsage);
                entity.Ignore(e => e.Feedback);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.StartDate);
                entity.Property(e => e.StartDate).HasColumnType("date");
                entity.Property(e => e.EndDate).HasColumnType("date");
                // Ignore navigation properties
                entity.Ignore(e => e.EventRewards);
                entity.Ignore(e => e.UserParticipations);
            });

            // Ignore navigation properties for User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.Ignore(e => e.CurrentTier);
                entity.Ignore(e => e.Appointments);
                entity.Ignore(e => e.Payments);
                entity.Ignore(e => e.LoyaltyPoints);
                entity.Ignore(e => e.Redemptions);
                entity.Ignore(e => e.ReferralsGiven);
                entity.Ignore(e => e.ReferralsReceived);
                entity.Ignore(e => e.UserPackages);
                entity.Ignore(e => e.ActivityLogs);
                entity.Ignore(e => e.TierHistory);
                entity.Ignore(e => e.PointExpiries);
                entity.Ignore(e => e.EventParticipations);
                entity.Ignore(e => e.Preferences);
                entity.Ignore(e => e.Addresses);
                entity.Ignore(e => e.Devices);
                entity.Ignore(e => e.Consents);
                entity.Ignore(e => e.Feedback);
                entity.Ignore(e => e.NotificationLogs);
            });

            // Configure remaining entities with basic settings
            var simpleEntities = new[]
            {
                typeof(RefreshToken), typeof(AdminAuditLog), typeof(EventReward),
                typeof(Integration), typeof(UserActivityLog), typeof(StoreSettings),
                typeof(RewardRedemptionMethod), typeof(TierHistory), typeof(PointExpiry),
                typeof(RewardCategory), typeof(UserEventParticipation), typeof(ReferralCampaign),
                typeof(StoreGroup), typeof(UserPreferences), typeof(UserAddress),
                typeof(UserDevice), typeof(UserConsent), typeof(RewardInventory),
                typeof(PackageType), typeof(ServiceCategory), typeof(StoreHour),
                typeof(UserFeedback), typeof(NotificationLog)
            };

            foreach (var entityType in simpleEntities)
            {
                var entity = modelBuilder.Entity(entityType);
                
                // Set default created date for entities that have CreatedAt property
                var createdAtProperty = entityType.GetProperty("CreatedAt");
                if (createdAtProperty != null)
                {
                    entity.Property("CreatedAt").HasDefaultValueSql("GETUTCDATE()");
                }

                // Set datetime column types
                var dateTimeProps = entityType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));
                
                foreach (var prop in dateTimeProps)
                {
                    var columnType = prop.Name.EndsWith("Date") && !prop.Name.Contains("Time") && !prop.Name.Contains("At")
                        ? "date" : "datetime2";
                    entity.Property(prop.Name).HasColumnType(columnType);
                }

                // Ignore ALL navigation properties to prevent EF relationship inference
                var navigationProps = entityType.GetProperties()
                    .Where(p => p.PropertyType.IsClass && p.PropertyType != typeof(string) 
                        && !p.PropertyType.IsValueType && !p.PropertyType.IsEnum);
                
                foreach (var prop in navigationProps)
                {
                    entity.Ignore(prop.Name);
                }
            }
        }        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed Membership Tiers
            modelBuilder.Entity<MembershipTier>().HasData(
                new MembershipTier
                {
                    Id = "tier-bronze",
                    Name = "Bronze",
                    MinPoints = 0,
                    Benefits = "Basic membership benefits",
                    TierOrder = 1
                },
                new MembershipTier
                {
                    Id = "tier-silver",
                    Name = "Silver",
                    MinPoints = 1000,
                    Benefits = "Silver membership benefits including priority booking",
                    TierOrder = 2
                },
                new MembershipTier
                {
                    Id = "tier-gold",
                    Name = "Gold",
                    MinPoints = 5000,
                    Benefits = "Gold membership benefits including discounts and exclusive offers",
                    TierOrder = 3
                },
                new MembershipTier
                {
                    Id = "tier-platinum",
                    Name = "Platinum",
                    MinPoints = 10000,
                    Benefits = "Platinum membership benefits including VIP treatment and exclusive events",
                    TierOrder = 4
                }
            );

            // Seed Store Groups
            modelBuilder.Entity<StoreGroup>().HasData(
                new StoreGroup
                {
                    Id = "group-main",
                    Name = "Gussmann Main Group",
                    Description = "Main store group for Gussmann locations"
                }
            );

            // Seed Stores
            modelBuilder.Entity<Store>().HasData(
                new Store
                {
                    Id = "store-downtown",
                    Name = "Gussmann Downtown",
                    Address = "123 Main Street",
                    City = "Downtown",
                    State = "CA",
                    Country = "USA",
                    Phone = "555-0100",
                    Email = "downtown@gussmann.com",
                    IsActive = true,
                    StoreGroupId = "group-main"
                },
                new Store
                {
                    Id = "store-uptown",
                    Name = "Gussmann Uptown",
                    Address = "456 High Street",
                    City = "Uptown",
                    State = "CA",
                    Country = "USA",
                    Phone = "555-0200",
                    Email = "uptown@gussmann.com",
                    IsActive = true,
                    StoreGroupId = "group-main"
                }
            );

            // Seed Service Categories
            modelBuilder.Entity<ServiceCategory>().HasData(
                new ServiceCategory
                {
                    Id = "cat-wellness",
                    Name = "Wellness",
                    Description = "Wellness and health services"
                },
                new ServiceCategory
                {
                    Id = "cat-beauty",
                    Name = "Beauty",
                    Description = "Beauty and cosmetic services"
                }
            );

            // Seed Services
            modelBuilder.Entity<Service>().HasData(
                new Service
                {
                    Id = "service-massage",
                    Name = "Relaxation Massage",
                    Description = "60-minute full body relaxation massage",
                    DurationMinutes = 60,
                    Price = 120.00m,
                    IsActive = true,
                    ServiceCategoryId = "cat-wellness"
                },
                new Service
                {
                    Id = "service-facial",
                    Name = "Rejuvenating Facial",
                    Description = "90-minute rejuvenating facial treatment",
                    DurationMinutes = 90,
                    Price = 150.00m,
                    IsActive = true,
                    ServiceCategoryId = "cat-beauty"
                }
            );

            // Seed Reward Categories
            modelBuilder.Entity<RewardCategory>().HasData(
                new RewardCategory
                {
                    Id = "reward-cat-services",
                    Name = "Service Rewards",
                    Description = "Rewards related to service bookings"
                },
                new RewardCategory
                {
                    Id = "reward-cat-products",
                    Name = "Product Rewards",
                    Description = "Rewards for product purchases"
                }
            );

            // Seed Rewards
            modelBuilder.Entity<Reward>().HasData(
                new Reward
                {
                    Id = "reward-free-massage",
                    Name = "Free 30-min Massage",
                    Description = "Complimentary 30-minute relaxation massage",
                    PointsRequired = 500,
                    IsSeasonal = false,
                    IsActive = true,
                    CategoryId = "reward-cat-services"
                },
                new Reward
                {
                    Id = "reward-discount-facial",
                    Name = "50% Off Facial",
                    Description = "50% discount on any facial treatment",
                    PointsRequired = 750,
                    IsSeasonal = false,
                    IsActive = true,
                    CategoryId = "reward-cat-services"
                }
            );

            // Seed Package Types
            modelBuilder.Entity<PackageType>().HasData(
                new PackageType
                {
                    Id = "package-type-wellness",
                    Name = "Wellness Packages",
                    Description = "Packages focused on wellness and relaxation"
                },
                new PackageType
                {
                    Id = "package-type-beauty",
                    Name = "Beauty Packages",
                    Description = "Packages focused on beauty treatments"
                }
            );

            // Seed Packages
            modelBuilder.Entity<Package>().HasData(
                new Package
                {
                    Id = "package-wellness-basic",
                    Name = "Basic Wellness Package",
                    Description = "5 wellness sessions including massages and treatments",
                    TotalSessions = 5,
                    Price = 500.00m,
                    ValidDays = 90,
                    IsActive = true,
                    PackageTypeId = "package-type-wellness"
                }
            );
        }
    }
}