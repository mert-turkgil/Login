using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Login.ViewModels;
using Login.Identity;
using Login.EmailServices;

public class HomeController : Controller
{
    private readonly IEmailSender _emailSender;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public HomeController(IEmailSender emailSender,UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _emailSender = emailSender;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult SignUp()
    {
        return View();
    }
    [HttpGet]
    public IActionResult EmailConfirmationError()
    {
        // This action serves as the error page when email confirmation fails
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("EmailConfirmationError");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction("EmailConfirmationError");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            return RedirectToAction("EmailConfirmed");
        }

        return RedirectToAction("EmailConfirmationError");
    }

        [HttpGet]
        public IActionResult EmailConfirmed()
        {
            // This action serves as the error page when email confirmation fails
            return View();
        }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(SignUpViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Email,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Generate confirmation link
            var confirmationLink = Url.Action(
                nameof(ConfirmEmail), // Action name
                "Home", // Controller name
                new { userId = user.Id, token }, // Route values
                Request.Scheme // Use HTTPS
            );

            // Send confirmation email
            var subject = "Confirm Your Email";
            var message = $@"
                <h1>Welcome to our platform!</h1>
                <p>Please confirm your email by clicking the link below:</p>
                <a href='{confirmationLink}'>Confirm Email</a>";

            try
            {
                await _emailSender.SendEmailAsync(model.Email, subject, message); 

            }
            catch (Exception ex)
            {
                // Handle email send failure
                Console.WriteLine($"Error sending email: {ex.Message}");
                // Optionally, delete the user to avoid unconfirmed accounts
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Unable to send confirmation email. Please try again.");
                return View(model);
            }

            return RedirectToAction("Index"); // Show a confirmation message
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please provide valid inputs.");
                return View("Index", model);
            }
            // Find the user by their email
             var user = await _userManager.FindByEmailAsync(model.Email);
             if (user == null)
             {
                 ModelState.AddModelError(string.Empty, "Invalid login attempt: user not found.");
                 return View("Index", model);
             }
            // Sign in the user
            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

            // Check if the account is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Your account is locked. Please try again later.");
                return View("Index", model);
            }
            if (result.Succeeded)
            {
                // Redirect to the account page if login is successful
                return RedirectToAction("Account");
            }
            if (result.IsNotAllowed)
            {
                return RedirectToAction("Index");
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Your account is locked due to too many failed attempts.");
                return View("Index", model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
            return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Account()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Index");
        }
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index");
    }
}
