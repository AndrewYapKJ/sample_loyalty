using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gussmann_loyalty_program.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update admin password hash to proper BCrypt format for password "princialtest"
            migrationBuilder.Sql(@"
                UPDATE [Admins] 
                SET [PasswordHash] = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqfuihiIGJ8VJPfz8Y.kLAe'
                WHERE [Email] = 'principaltest' AND [Role] = 'principal'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to previous hash format
            migrationBuilder.Sql(@"
                UPDATE [Admins] 
                SET [PasswordHash] = '$2a$11$rKjqGVwF9jQ8hP2mN5lK1.F1tS3vB7wC9xE2yD6zA8bH4nM5kL0oG'
                WHERE [Email] = 'principaltest' AND [Role] = 'principal'
            ");
        }
    }
}
