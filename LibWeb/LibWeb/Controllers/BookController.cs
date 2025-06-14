﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using LibWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization.Infrastructure;

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
        var genres = await _context.Genres
                                   .ToListAsync();
        ViewBag.Genres = genres;
        ViewBag.CurrentTitle = Title;
        ViewBag.SelectedGenres = genre?.ToList();

        var books = _context.Books
                    .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                    .AsQueryable();

        if (!string.IsNullOrEmpty(Title))
        {
            string searchPattern = $"%{Title}%";
            books = books.Where(b => EF.Functions.Like(b.Title, searchPattern) || EF.Functions.Like(b.Author, searchPattern));
        }

        if (genre != null && genre.Any())
        {
            books = books.Where(b => b.BookGenres.Any(bg => genre.Any(g => g == bg.Genre.Name)));
        }

        int pageSize = 10;
        var totalBooks = await books.CountAsync();
        var totalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);
        var pagedBooks = await books
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedBooks);
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
    [Authorize(Roles = "Admin, Thuthu")]
    public async Task<IActionResult> Create(Book book)
    {
        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        {
            Console.WriteLine(error.ErrorMessage);
        }

        if (ModelState.IsValid)
        {
            if (string.IsNullOrEmpty(book.BookID))
            {
                book.BookID = GenerateBookID();
            }

            _context.Add(book);
            await _context.SaveChangesAsync();

            if (book.SelectedGenreIDs != null && book.SelectedGenreIDs.Any())
            {
                foreach (var genreId in book.SelectedGenreIDs)
                {
                    _context.BookGenres.Add(new BookGenre { BookID = book.BookID, GenreID = genreId });
                }
                await _context.SaveChangesAsync();
            }

            if ((book.ImgPath != null && book.ImgPath.Length > 0) || (book.ImgFile != null))
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(book.ImgPath.FileName);
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Img");

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

            _context.Update(book);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Books");
        }

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
    [Authorize(Roles = "Admin, Thuthu")]
    public async Task<IActionResult> Edit(Book book)
    {
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage); 
            }
        }
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(book);
                await _context.SaveChangesAsync();

                var existingGenres = await _context.BookGenres.Where(bg => bg.BookID == book.BookID).ToListAsync();
                _context.BookGenres.RemoveRange(existingGenres);

                foreach (var genreId in book.SelectedGenreIDs)
                {
                    _context.BookGenres.Add(new BookGenre { BookID = book.BookID, GenreID = genreId });
                }

                if ((book.ImgPath != null && book.ImgPath.Length > 0) || (book.ImgFile != null))
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(book.ImgPath.FileName);
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

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

            _context.Update(book);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Books");
        }

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
    [Authorize(Roles = "Admin, Thuthu")]
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