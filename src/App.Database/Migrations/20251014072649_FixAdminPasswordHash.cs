using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gussmann_loyalty_program.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update admin password hash to correct BCrypt format for password "admin123"
            migrationBuilder.Sql(@"
                UPDATE [Admins] 
                SET [PasswordHash] = '$2a$11$RARrE2iVt29Ds6M6lr8lwObKybVhtmWXUUHhctxZzvEjzgOEbefly'
                WHERE [Email] = 'principaltest' AND [Role] = 'principal'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to previous hash format
            migrationBuilder.Sql(@"
                UPDATE [Admins] 
                SET [PasswordHash] = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqfuihiIGJ8VJPfz8Y.kLAe'
                WHERE [Email] = 'principaltest' AND [Role] = 'principal'
            ");
        }
    }
}
