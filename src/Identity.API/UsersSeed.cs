
namespace eShop.Identity.API;

public class UsersSeed(ILogger<UsersSeed> logger, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : IDbSeeder<ApplicationDbContext>
{
    public const string AdministratorRole = "Administrator";

    public async Task SeedAsync(ApplicationDbContext context)
    {
        var alice = await userManager.FindByNameAsync("alice");

        if (alice == null)
        {
            alice = new ApplicationUser
            {
                UserName = "alice",
                Email = "AliceSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Alice Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Alice",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "123"
            };

            var result = await userManager.CreateAsync(alice, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice already exists");
            }
        }

        var bob = await userManager.FindByNameAsync("bob");

        if (bob == null)
        {
            bob = new ApplicationUser
            {
                UserName = "bob",
                Email = "BobSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Bob Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Bob",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "456"
            };

            var result = await userManager.CreateAsync(bob, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob already exists");
            }
        }

        if (!await roleManager.RoleExistsAsync(AdministratorRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdministratorRole));
        }

        var admin = await userManager.FindByNameAsync("admin");

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@eshop.com",
                EmailConfirmed = true,
                CardHolderName = "Priya Admin",
                CardNumber = "XXXXXXXXXXXX0000",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/30",
                Id = Guid.NewGuid().ToString(),
                LastName = "Admin",
                Name = "Priya",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "1 Admin Way",
                SecurityNumber = "000"
            };

            var result = await userManager.CreateAsync(admin, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            await userManager.AddToRoleAsync(admin, AdministratorRole);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("admin created");
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(admin, AdministratorRole))
            {
                await userManager.AddToRoleAsync(admin, AdministratorRole);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("admin already exists");
            }
        }
    }
}
