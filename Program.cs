using Login.EmailServices;
using Login.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
#region değişken
// Load configuration
var config = builder.Configuration;

// Configure EmailSender settings
var emailSettings = config.GetSection("EmailSender");
var port = emailSettings.GetValue<int>("Port");
var host = emailSettings.GetValue<string>("SMTPMail");
var enablessl = true; // Gmail typically requires SSL
var username = emailSettings.GetValue<string>("Username");
var password = emailSettings.GetValue<string>("Password");
var connectionString = builder.Configuration.GetConnectionString("MsSqlConnection") 
    ?? throw new InvalidOperationException("Connection string 'MsSqlConnection' not found.");
#endregion
#region Database
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
#endregion
#region giriş
// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;

    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;

    // Lockout policy
    options.Lockout.MaxFailedAccessAttempts = 4;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(4);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookies
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/accessdenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.Cookie = new CookieBuilder {
        HttpOnly = true,
        Name = ".Login.Security.Cookie",
        SameSite = SameSiteMode.None,
        SecurePolicy = CookieSecurePolicy.Always
    };
});

#endregion
#region Services
    if (string.IsNullOrEmpty(host))
        throw new ArgumentNullException(nameof(host), "SMTP host cannot be null or empty.");
    if (port <= 0)
        throw new ArgumentOutOfRangeException(nameof(port), "SMTP port must be a positive number.");
    if (string.IsNullOrEmpty(username))
        throw new ArgumentNullException(nameof(username), "SMTP username cannot be null or empty.");
    if (string.IsNullOrEmpty(password))
        throw new ArgumentNullException(nameof(password), "SMTP password cannot be null or empty.");
    builder.Services.AddScoped<Login.EmailServices.IEmailSender, SmtpEmailSender>(i =>
        new SmtpEmailSender(host, port, enablessl, username, password));
    builder.Services.AddScoped<UserManager<User>>();
#endregion
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Seed roles and users in development mode
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var rootUserSection = configuration.GetSection("Data:Users").GetChildren()
            .FirstOrDefault(user => user.GetValue<string>("username") == "root");

        if (rootUserSection != null)
        {
            var rootUsername = rootUserSection.GetValue<string>("username");
            var rootEmail = rootUserSection.GetValue<string>("email");
            if (string.IsNullOrEmpty(rootEmail))
            throw new ArgumentNullException(nameof(rootEmail), "Root email cannot be null or empty.");

            var rootUser = userManager.FindByEmailAsync(rootEmail).Result;

            if (rootUser == null)
            {
                Console.WriteLine("Root user does not exist. Seeding...");
                SeedIdentity.Seed(userManager, roleManager, configuration).Wait();
            }
            else
            {
                Console.WriteLine("Root user already exists. Skipping seed.");
            }

            // Check if root user already exists
            if (rootUser == null)
            {
                Console.WriteLine("Root user does not exist. Seeding roles and users...");
                SeedIdentity.Seed(userManager, roleManager, configuration).Wait();
            }
            else
            {
                Console.WriteLine("Root user already exists. Skipping seed process.");
            }
        }
        else
        {
            Console.WriteLine("Root user configuration not found.");
        }
    }
}
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
