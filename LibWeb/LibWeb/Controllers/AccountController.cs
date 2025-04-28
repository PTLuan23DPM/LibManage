using Microsoft.AspNetCore.Mvc;
using LibWeb.Models;
using LibWeb.Services;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LibWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;

        private readonly AppDbContext _context;
        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }
        public async Task<IActionResult> Index()
        {
            var users = await _accountService.GetAllUsers();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(new User());
        }
        private static string GenerateUserID(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[new Random().Next(s.Length)]).ToArray());
        }
        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            Console.WriteLine($"Received User: {user.Username}, {user.PasswordHash}, {user.Role}");
            user.UserID = GenerateUserID(20);

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                return View(user);
            }

            var result = await _accountService.CreateUser(user);
            if (result)
            {
                Console.WriteLine("User created successfully!");
                return RedirectToAction("Index");
            }

            Console.WriteLine("Failed to create user.");

            await _context.SaveChangesAsync();
            return View(user);
        }

        public async Task<IActionResult> Edit(string? id)
        {
            var user = await _accountService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var result = await _accountService.UpdateUser(user);
            if (result)
            {
                return RedirectToAction("Index");
            }
            return View(user);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var result = await _accountService.DeleteUser(id);
            return RedirectToAction("Index");
        }
    }
}
