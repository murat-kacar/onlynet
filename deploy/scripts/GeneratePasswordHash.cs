using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<IdentityUser>();
var password = "Admin123!";
var hash = hasher.HashPassword(null, password);
Console.WriteLine(hash);
