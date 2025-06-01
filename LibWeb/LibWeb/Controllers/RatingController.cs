using LibWeb.Models;
using LibWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
public class RatingController : Controller
{
    private readonly AppDbContext _context;

    public RatingController(AppDbContext context)
    {
        _context = context;
    }
    // GET: Rating/Create/{bookID}
    public ActionResult Create(string bookID)
    {
        ViewBag.BookID = bookID;
        return View();
    }

    // POST: Rating/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(Ratings rating)
    {
        if (!ModelState.IsValid) return View(rating);

        var existing = _context.Ratings.FirstOrDefault(r => r.UserID == rating.UserID && r.BookID == rating.BookID);
        if (existing != null)
        {
            ModelState.AddModelError("", "Bạn đã đánh giá sách này rồi.");
            return View(rating);
        }

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        await UpdateBookAverageRating(rating.BookID);

        return RedirectToAction("Details", "Book", new { id = rating.BookID });
    }

    // GET: Rating/Edit/{userID}/{bookID}
    public ActionResult Edit(string userID, string bookID)
    {
        var rating = _context.Ratings.FirstOrDefault(r => r.UserID == userID && r.BookID == bookID);
        if (rating == null) return NotFound();

        return View(rating);
    }

    // POST: Rating/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(Ratings updatedRating)
    {
        if (!ModelState.IsValid) return View(updatedRating);

        var rating = _context.Ratings.FirstOrDefault(r => r.UserID == updatedRating.UserID && r.BookID == updatedRating.BookID);
        if (rating == null) return NotFound();

        rating.Rating = updatedRating.Rating;
        await _context.SaveChangesAsync();
        await UpdateBookAverageRating(updatedRating.BookID);

        return RedirectToAction("Details", "Book", new { id = updatedRating.BookID });
    }

    private async Task UpdateBookAverageRating(string bookID)
    {
        var ratings = _context.Ratings.Where(r => r.BookID == bookID).ToList();
        float avg = ratings.Any() ? (float)System.Math.Round(ratings.Average(r => r.Rating), 2) : 0;

        var book = _context.Books.FirstOrDefault(b => b.BookID == bookID);
        if (book != null)
        {
            book.AverageRating = avg;
            await _context.SaveChangesAsync();
        }
    }
    private static Random _random = new Random();
    public static string GenerateRandomId(int length = 20)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    [HttpPost]
    [Authorize]
    public IActionResult Rate(string bookID, int ratingValue)
    {
        string username = User.Identity.Name;
        var user = _context.Users.FirstOrDefault(u => u.Username == username);
        if (user == null)
        {
            return Unauthorized();
        }
        string userId = user.UserID;
        var existingRating = _context.Ratings.FirstOrDefault(r => r.BookID == bookID && r.UserID == userId);

        if (existingRating != null)
        {
            existingRating.Rating = ratingValue;
        }
        else
        {
            string newId;
            do
            {
                newId = GenerateRandomId(20);
            } while (_context.Ratings.Any(r => r.RatingID == newId));

            _context.Ratings.Add(new Ratings
            {
                RatingID = newId,
                BookID = bookID,
                UserID = userId,
                Rating = ratingValue
            });
        }

        _context.SaveChanges();

        // Update average rating
        var book = _context.Books.FirstOrDefault(b => b.BookID == bookID);
        if (book != null)
        {
            var ratings = _context.Ratings.Where(r => r.BookID == bookID).Select(r => r.Rating).ToList();
            book.AverageRating = ratings.Count > 0 ? (float)ratings.Average() : 0f;
            _context.SaveChanges();
        }

        return RedirectToAction("Details", "Books", new { id = bookID });
    }
}
