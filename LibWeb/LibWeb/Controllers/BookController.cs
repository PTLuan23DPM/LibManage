using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using LibWeb.Models;

public class BooksController : Controller
{
    private readonly AppDbContext _context;

    public BooksController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var books = await _context.Books.Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
                  .ToListAsync();
        return View(books);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.BookGenres)
            .ThenInclude(bg => bg.Genre)
            .FirstOrDefaultAsync(m => m.BookID == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }
    [HttpPost]
    public async Task<IActionResult> Buy(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books.FirstOrDefaultAsync(b => b.BookID == id);

        if (book == null)
        {
            return NotFound();
        }

        // Kiểm tra nếu sách còn trong kho
        if (book.AvailableQuantity > 0)
        {
            book.AvailableQuantity -= 1; 
            await _context.SaveChangesAsync();
        }
        else
        {
            TempData["ErrorMessage"] = "This book is out of stock!";
        }

        return RedirectToAction("Details", new { id });
    }

}