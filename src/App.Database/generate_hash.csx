using BCrypt.Net;

var password = "princialtest";
var hash = BCrypt.HashPassword(password);
Console.WriteLine($"BCrypt hash for '{password}': {hash}");