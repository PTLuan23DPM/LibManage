using Microsoft.AspNetCore.Mvc;
using LibWeb.Models;
using LibWeb.Services;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace LibWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly AuthenticationService _authService;
        private readonly AppDbContext _context;
        public AccountController(AccountService accountService, AuthenticationService authService, AppDbContext context)
        {
            _accountService = accountService;
            _authService = authService;
            _context = context;
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Thuthu")]
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _accountService.DeleteUser(id);
            return RedirectToAction("Index", "Account");
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _authService.AuthenticateAsync(model.Username, model.PasswordHash); 

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Books"); 
                }

                ModelState.AddModelError(string.Empty, "Đăng nhập không thành công. Vui lòng kiểm tra tên đăng nhập và mật khẩu."); // Login failed message
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Items.Remove("UserID");
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Books");
        }
        [HttpGet]
        public IActionResult GetBorrowsByUserId(string userId)
        {
            var borrows = _context.Borrow
              .Include(b => b.BorrowDetails)
                .ThenInclude(d => d.Book)
              .Where(b => b.UserID == userId)
              .ToList();

            return PartialView("_BorrowListPartial", borrows);
        }

    }
}
