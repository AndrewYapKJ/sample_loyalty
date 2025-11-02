-- SQL Script to add admin user to the Admins table
-- Run this using SQL Server Management Studio or similar SQL client

-- Parameters for the admin user
DECLARE @AdminId NVARCHAR(450) = NEWID();
DECLARE @Username NVARCHAR(100) = 'principaltest';
DECLARE @Email NVARCHAR(255) = 'principaltest';
DECLARE @PasswordHash NVARCHAR(255) = 'temp_hash_replace_this'; -- This will be replaced with proper hash
DECLARE @FullName NVARCHAR(200) = 'Principal Administrator';
DECLARE @Role NVARCHAR(50) = 'principal';

-- Check if admin already exists
IF NOT EXISTS (SELECT 1 FROM Admins WHERE Username = @Username OR Email = @Email)
BEGIN
    INSERT INTO Admins (Id, Username, Email, PasswordHash, FullName, Role, IsActive, CreatedAt, LoginAttempts)
    VALUES (@AdminId, @Username, @Email, @PasswordHash, @FullName, @Role, 1, GETUTCDATE(), 0);
    
    PRINT 'Admin user created successfully!';
    PRINT 'Username: ' + @Username;
    PRINT 'Email: ' + @Email;
    PRINT 'Role: ' + @Role;
    PRINT 'ID: ' + @AdminId;
END
ELSE
BEGIN
    PRINT 'Admin user with username ''' + @Username + ''' or email ''' + @Email + ''' already exists.';
END

GO