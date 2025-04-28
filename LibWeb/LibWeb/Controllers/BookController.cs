using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using LibWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

public class BooksController : Controller
{
    private readonly AppDbContext _context;
    private const int PageSize = 5;
    public BooksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string Title, string[] genre, int page = 1)
    {
        var genres = await _context.Books
                                   .SelectMany(g => g.BookGenres)
                                   .Select(gg => gg.Genre)
                                   .Distinct()
                                   .ToListAsync();

        ViewBag.Genres = genres;

        var books = _context.Books
                    .Include(g => g.BookGenres)
                    .ThenInclude(gg => gg.Genre)
        .AsQueryable();

        if (!string.IsNullOrEmpty(Title))
        {
            books = books.Where(g => EF.Functions.Like(g.Title, $"%{Title}%"));
        }

        if (genre != null && genre.Any())
        {
            books = books.Where(g => g.BookGenres.Any(gg => genre.Contains(gg.Genre.Name)));
        }

        var totalGames = await books.CountAsync();
        var totalPages = (int)Math.Ceiling(totalGames / (double)PageSize);

        var pagedGames = await books
                            .Skip((page - 1) * PageSize)
                            .Take(PageSize)
                            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedGames);
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
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Genres = await _context.Genres.ToListAsync();
        ViewBag.BookID = GenerateBookID();
        return View(new Book());
    }
    
    public static string GenerateBookID()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 20)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Book book)
    {
        // Debug ModelState errors before proceeding
        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        {
            Console.WriteLine(error.ErrorMessage);
        }

        // Ensure the BookID is generated before saving the book to the database
        if (ModelState.IsValid)
        {
            // Generate BookID if it's not set already
            if (string.IsNullOrEmpty(book.BookID))
            {
                book.BookID = GenerateBookID();
            }

            // Add the book to the database
            _context.Add(book);
            await _context.SaveChangesAsync();

            // Add genres to BookGenres table if SelectedGenreIDs are provided
            if (book.SelectedGenreIDs != null && book.SelectedGenreIDs.Any())
            {
                foreach (var genreId in book.SelectedGenreIDs)
                {
                    _context.BookGenres.Add(new BookGenre { BookID = book.BookID, GenreID = genreId });
                }
                await _context.SaveChangesAsync();
            }

            // Handle image upload if present
            if ((book.ImgPath != null && book.ImgPath.Length > 0) || (book.ImgFile != null))
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(book.ImgPath.FileName);
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Img");

                // Ensure the directory exists
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var filePath = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await book.ImgPath.CopyToAsync(stream);
                }
                book.ImgFile = fileName;
            }

            // Update the book record with the image file name if necessary
            _context.Update(book);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Books");
        }

        // If ModelState is invalid, load genres again and return to view
        ViewBag.Genres = await _context.Genres.ToListAsync() ?? new List<Genre>();
        return View(new Book { SelectedGenreIDs = new List<string>() });
    }


    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.BookGenres)
            .ThenInclude(bg => bg.Genre)
            .FirstOrDefaultAsync(b => b.BookID == id);

        if (book == null)
        {
            return NotFound();
        }

        // Populate genres for dropdown in the view
        ViewBag.Genres = new SelectList(await _context.Genres.ToListAsync(), "GenreID", "Name");
        ViewBag.SelectedGenres = book.BookGenres.Select(bg => bg.GenreID).ToList();

        return View(book);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Book book)
    {
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage); // Or log the error
            }
        }
        if (ModelState.IsValid)
        {
            try
            {
                // Update the book's basic details in the database
                _context.Update(book);
                await _context.SaveChangesAsync();

                // Remove the existing genres associated with the book
                var existingGenres = await _context.BookGenres.Where(bg => bg.BookID == book.BookID).ToListAsync();
                _context.BookGenres.RemoveRange(existingGenres);

                // Add the selected genres for the book
                foreach (var genreId in book.SelectedGenreIDs)
                {
                    _context.BookGenres.Add(new BookGenre { BookID = book.BookID, GenreID = genreId });
                }

                // Handle the image upload if there's a new file
                if ((book.ImgPath != null && book.ImgPath.Length > 0) || (book.ImgFile != null))
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(book.ImgPath.FileName);
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

                    // Ensure the directory exists
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await book.ImgPath.CopyToAsync(stream);
                    }

                    book.ImgFile = fileName;
                }
                else
                {
                    // If no image is uploaded, set a default image
                    if (string.IsNullOrEmpty(book.ImgFile))
                    {
                        book.ImgFile = "default.png";
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(book.BookID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Save changes to the book entity after updating genres and image
            _context.Update(book);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Books");
        }

        // If model validation fails, return the form with existing genres and selected genres
        ViewBag.Genres = new SelectList(await _context.Genres.ToListAsync(), "GenreID", "Name");
        ViewBag.SelectedGenres = book.SelectedGenreIDs;
        return View(book);
    }
    private bool BookExists(string id)
    {
        return _context.Books.Any(e => e.BookID == id);
    }

    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(m => m.BookID == id);
        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Books");
    }
}