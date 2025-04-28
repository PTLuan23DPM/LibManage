using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using AspNetCoreGeneratedDocument;

namespace LibWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        public CartController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult AddToCart(string bookID, int quantity)
        {
            var book = _context.Books.FirstOrDefault(g => g.BookID == bookID);

            if (book != null && book.AvailableQuantity >= quantity)
            {
                var cart = GetCart();

                // Check if the book is already in the cart
                var cartItem = cart.FirstOrDefault(c => c.Book.BookID == bookID);

                if (cartItem == null)
                {
                    // Book not in the cart, add it
                    cart.Add(new CartItem { Book = book, Quantity = quantity });
                }
                else
                {
                    // Book already in cart, deny adding more
                    TempData["ErrorMessage"] = $"You have already added {book.Title} to your cart.";
                    return View("Index", cart);  // Return the same view with the current cart
                }

                // Update the cart in the session
                SetCart(cart);
            }

            return View("Index", GetCart());  // Return the current cart
        }

        [HttpPost]
        public IActionResult RemoveFromCart(string bookID)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(c => c.Book.BookID == bookID);

            if (cartItem != null)
            {
                var book = _context.Books.FirstOrDefault(g => g.BookID == bookID);
                if (book != null)
                {
                    book.AvailableQuantity += cartItem.Quantity;
                    _context.SaveChanges();
                }

                cart.Remove(cartItem);
                SetCart(cart);
            }

            return RedirectToAction("Index", "Cart");
        }

        [HttpGet]
        public IActionResult BorrowSuccess()
        {
            return View();
        }
        public static string GenerateBorrowID()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 20)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not found or not logged in.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found!";
                return RedirectToAction("Index", "Cart");
            }

            var borrow = new Borrow
            {
                BorrowID = GenerateBorrowID(),
                UserID = userId
            };

            _context.Borrows.Add(borrow);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                var book = await _context.Books.FindAsync(item.Book.BookID);
                if (book == null || book.AvailableQuantity < 1)
                {
                    TempData["ErrorMessage"] = $"Not enough stock for game: {book?.Title ?? "Unknown"}.";
                    return RedirectToAction("Index", "Cart");
                }

                var borrowDetail = new BorrowDetail
                {
                    BorrowID = borrow.BorrowID,
                    BookID = item.Book.BookID,
                    OrderDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(7) // Set due date to 7 days from now
                };

                _context.BorrowDetails.Add(borrowDetail);

                book.AvailableQuantity -= 1;
            }


            await _context.SaveChangesAsync();

            SetCart(new List<CartItem>());

            return RedirectToAction("OrderSuccess", "Cart", new { borrowID = borrow.BorrowID });
        }


        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cart))
            {
                return new List<CartItem>();
            }

            return JsonConvert.DeserializeObject<List<CartItem>>(cart);
        }

        private void SetCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        }
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }
    }
}
