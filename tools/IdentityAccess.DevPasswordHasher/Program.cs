using Microsoft.AspNetCore.Identity;

if (args.Length != 2 || !Guid.TryParse(args[0], out var identityScopeId) || !Guid.TryParse(args[1], out var userId))
{
    Console.Error.WriteLine("Usage: IdentityAccess.DevPasswordHasher <identity-scope-id> <user-id>");
    return 2;
}

var password = Console.ReadLine();
if (string.IsNullOrEmpty(password) || password.Length is < 12 or > 256)
{
    Console.Error.WriteLine("Password input must contain 12 to 256 characters.");
    return 3;
}

var subjectKey = $"{identityScopeId:D}:{userId:D}";
var hasher = new PasswordHasher<string>();
Console.WriteLine(hasher.HashPassword(subjectKey, password));
return 0;
