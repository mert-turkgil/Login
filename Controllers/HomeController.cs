using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Login.Identity;
using Login.EmailServices;
using Login.Models;
using Microsoft.AspNetCore.Authorization;
using Login.Controllers;
using System.Collections.Concurrent;
using Login.Services;


public class HomeController : Controller
{
    private readonly IEmailSender _emailSender;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private static readonly ConcurrentDictionary<int, bool> RoomLockStatus = new ConcurrentDictionary<int, bool>();
    private readonly RoleManager<IdentityRole> _roleManager;

    public HomeController(IEmailSender emailSender,UserManager<User> userManager, SignInManager<User> signInManager,RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
        _emailSender = emailSender;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Account");
        }
        return View();
    }
    [HttpGet]
    public IActionResult Privacy()
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
            if (string.IsNullOrEmpty(user?.UserName))
            {
                // Handle the null case appropriately, e.g., return an error or log it
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
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

            // Build the view model (UserViewModel) with 12 room cards
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedDate = user.CreatedDate,
                UserName = user.UserName,
                User = user,
                RoomCards = new List<polRoomCardModel>()
            };

            return View(userViewModel);
        }

        [HttpGet]
        public IActionResult RefreshPolRoomCards()
        {
            var polRooms = new List<polRoomCardModel>();
            for (int i = 1; i <= 12; i++)
            {
                if (LiveDataStore.LivePolRoomData.TryGetValue(i, out var roomCard))
                {
                    polRooms.Add(new polRoomCardModel
                    {
                        id = roomCard.id,
                        RoomName = roomCard.RoomName,
                        Status = roomCard.Status,
                        Temperature = roomCard.Temperature,
                        IsLocked = roomCard.IsLocked
                    });
                }
                else
                {
                    for (int a = 1; a <= 12; a++){
                    // If no data yet, show "Loading..."
                        polRooms.Add(new polRoomCardModel
                        {
                            id = a,
                            RoomName = $"Room {a}",
                            Status = "Loading...",
                            Temperature = 0,
                            IsLocked = false
                        });
                    }

                }
            }

            return PartialView("_polRoomCards", polRooms);
        }







    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index");
    }
        #region User
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UserDelete(string id)
        {
            // Find the user by ID
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Account");
            }

            // Check if the target user is the Root user.
            bool isTargetRoot = user.FirstName == "Root" && user.LastName == "Türkgil";
            if (isTargetRoot)
            {
                TempData["ErrorMessage"] = "You cannot delete the Root user.";
                return RedirectToAction("Account");
            }

            // Get the roles for the target user.
            var roles = await _userManager.GetRolesAsync(user);
            // If the target user is an admin, block deletion.
            if (roles.Contains("Admin"))
            {
                TempData["ErrorMessage"] = "You cannot delete an Admin user.";
                return RedirectToAction("Account");
            }

            // Proceed to delete the user.
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error occurred while deleting the user.";
            }

            return RedirectToAction("Account");
        }


        // Edit User Method
        [HttpGet("Home/UserEdit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserEdit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Account");
            }

            var currentUser = await _userManager.GetUserAsync(User!);
            var currentRoles = await _userManager.GetRolesAsync(currentUser!);

            // Only Admins can edit Admins
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var currentUserIsAdmin = currentRoles.Contains("Admin");

            if (isAdmin && !currentUserIsAdmin)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this user.";
                return RedirectToAction("Account");
            }

            var model = new UserEditModel
            {
                UserId = user.Id,
                CreatedDate = user.CreatedDate,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList(),
                AllRoles = _roleManager.Roles.Select(r => r.Name).ToList()
            };

            return View(model);
        }

        [HttpPost("Home/UserEdit/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(UserEditModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Account");
            }

            var currentUser = await _userManager.GetUserAsync(User!);
            var currentRoles = await _userManager.GetRolesAsync(currentUser!);

            // Only Root can edit Root
            var isTargetRoot = user.FirstName == "Root" && user.LastName == "Türkgil";
            var isCurrentRoot = currentUser!.FirstName == "Root" && currentUser.LastName == "Türkgil";

            if (isTargetRoot && !isCurrentRoot)
            {
                TempData["ErrorMessage"] = "Only the Root user can edit the Root user.";
                return RedirectToAction("Account");
            }

            // Only Admins can edit Admins
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var currentUserIsAdmin = currentRoles.Contains("Admin");

            if (isAdmin && !currentUserIsAdmin)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this user.";
                return RedirectToAction("Account");
            }

            // Update User Information
            user.CreatedDate = model.CreatedDate;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.EmailConfirmed = model.EmailConfirmed;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Update Roles
                var currentRolesForUser = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRolesForUser);
                await _userManager.AddToRolesAsync(user, model.SelectedRoles ?? new List<string>());

                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction("Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }
        #endregion
        
}
