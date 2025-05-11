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
        public static string GenerateID()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 20)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            // 1. Get the User's ID:
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Use this!
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thanh toán."; // You need to login to checkout.
                return RedirectToAction("Login", "Account"); // Redirect to the login page
            }

            // 2. Get the Cart:
            var cart = GetCart();
            if (cart == null || cart.Count == 0) // Check for null cart
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!"; // Your cart is empty!
                return RedirectToAction("Index", "Cart");
            }

            // 3. Get the User from the Database:
            var user = await _context.Users.FindAsync(userId); // Find by ID
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng!"; // User not found!
                return RedirectToAction("Index", "Cart"); // Or handle this more gracefully
            }

            // 4. Create the Borrow:
            var borrow = new Borrow
            {
                BorrowID = GenerateID(),
                UserID = userId, // Use the user ID from the claim
                User = user, // Set the User navigation property
            };

            _context.Borrow.Add(borrow);


            // 5. Iterate through the cart and create BorrowDetails:
            foreach (var item in cart)
            {
                var book = await _context.Books.FindAsync(item.Book.BookID); // Find book

                if (book == null || book.AvailableQuantity < 1) // Check quantity!
                {
                    TempData["ErrorMessage"] = $"Không đủ số lượng cho sách: {book?.Title ?? "Unknown"}. Chỉ còn lại {book?.AvailableQuantity ?? 0} quyển"; // Not enough stock
                    return RedirectToAction("Index", "Cart");
                }

                var borrowDetail = new BorrowDetail
                {
                    BorrowDetailID = GenerateID(), // Generate unique ID for BorrowDetail
                    BorrowID = borrow.BorrowID,
                    BookID = item.Book.BookID,
                    OrderDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(7)
                };
                Console.WriteLine($"[DEBUG] Adding BorrowDetail: {borrowDetail.BorrowDetailID}, BookID: {borrowDetail.BookID}");

                _context.BorrowDetails.Add(borrowDetail);

                // Deduct the quantity before saving
                book.AvailableQuantity -= 1;
            }

            // 6. Save Changes:
            await _context.SaveChangesAsync();

            // 7. Clear the Cart:
            SetCart(new List<CartItem>());

            // 8. Redirect:
            return RedirectToAction("OrderSuccess", "Cart");
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
