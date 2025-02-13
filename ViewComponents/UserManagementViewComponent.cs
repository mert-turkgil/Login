using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Login.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Login.ViewComponents
{
    public class UserManagementViewComponent : ViewComponent
    {
        private readonly UserManager<User> _userManager;

        public UserManagementViewComponent(UserManager<User> userManager)
        {
            _userManager = userManager;
        } 

        // This method renders the view component's UI
        public IViewComponentResult Invoke()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // API-like methods for user management:

        public async Task<IActionResult> CreateUser(string email, string password)
        {
            var user = new User { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, password);
            return result.Succeeded 
                ? new JsonResult(new { message = "User created" }) 
                : new JsonResult(result.Errors);
        }

        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) 
                return new JsonResult(new { message = "User not found" });

            user.LockoutEnd = DateTime.UtcNow.AddYears(100); // Blocking user indefinitely
            await _userManager.UpdateAsync(user);
            return new JsonResult(new { message = "User blocked" });
        }

        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) 
                return new JsonResult(new { message = "User not found" });

            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);
            return new JsonResult(new { message = "User unblocked" });
        }

        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) 
                return new JsonResult(new { message = "User not found" });

            await _userManager.DeleteAsync(user);
            return new JsonResult(new { message = "User deleted" });
        }

        public async Task<IActionResult> UpdateUser(string userId, string newEmail)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) 
                return new JsonResult(new { message = "User not found" });

            user.Email = newEmail;
            user.UserName = newEmail;
            await _userManager.UpdateAsync(user);
            return new JsonResult(new { message = "User updated" });
        }
    }
}
