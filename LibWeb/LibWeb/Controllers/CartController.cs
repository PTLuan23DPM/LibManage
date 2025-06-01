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
                    return View("Index", cart);  
                }

                SetCart(cart);
            }

            return View("Index", GetCart()); 
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
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thanh toán."; 
                return RedirectToAction("Login", "Account"); 
            }

            var cart = GetCart();
            if (cart == null || cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!"; 
                return RedirectToAction("Index", "Cart");
            }

            var user = await _context.Users.FindAsync(userId); 
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng!"; 
                return RedirectToAction("Index", "Cart"); 
            }

            var borrow = new Borrow
            {
                BorrowID = GenerateID(),
                UserID = userId, 
                User = user, 
            };

            _context.Borrow.Add(borrow);

            foreach (var item in cart)
            {
                var book = await _context.Books.FindAsync(item.Book.BookID); 

                if (book == null || book.AvailableQuantity < 1) 
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

            await _context.SaveChangesAsync();

            SetCart(new List<CartItem>());

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
