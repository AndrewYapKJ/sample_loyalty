using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gussmann_loyalty_program.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUserData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert admin user with role "principal", email "principaltest", password "princialtest"
            migrationBuilder.Sql(@"
                INSERT INTO [Admins] ([Id], [Email], [PasswordHash], [Username], [FullName], [Role], [IsActive], [LoginAttempts], [CreatedAt])
                VALUES (
                    NEWID(),
                    'principaltest',
                    '$2a$11$rKjqGVwF9jQ8hP2mN5lK1.F1tS3vB7wC9xE2yD6zA8bH4nM5kL0oG',
                    'principaltest',
                    'Principal Test User',
                    'principal',
                    1,
                    0,
                    GETUTCDATE()
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the seeded admin user
            migrationBuilder.Sql(@"
                DELETE FROM [Admins] 
                WHERE [Email] = 'principaltest' AND [Role] = 'principal'
            ");
        }
    }
}
