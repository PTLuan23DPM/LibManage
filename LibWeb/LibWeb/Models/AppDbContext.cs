using Microsoft.EntityFrameworkCore;
using LibWeb.Models;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<BookGenre> BookGenres { get; set; }
    public DbSet<Ratings> Ratings { get; set; }
    public DbSet<Borrow> Borrow { get; set; }
    public DbSet<BorrowDetail> BorrowDetails { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        modelBuilder.Entity<Genre>()
            .HasIndex(g => g.Name)
            .IsUnique();
        modelBuilder.Entity<Ratings>()
            .HasIndex(r => new { r.UserID, r.BookID })
            .IsUnique();
        modelBuilder.Entity<Book>()
            .HasKey(b => b.BookID);
        // Đảm bảo SQL lưu đúng kiểu double
        modelBuilder.Entity<Book>()
            .Property(b => b.AverageRating)
            .HasColumnType("float");
        //Thiết lập quan hệ n-n giữa Genre và Book qua BookGenre 
        modelBuilder.Entity<BookGenre>()
            .HasKey(bg => new { bg.BookID, bg.GenreID });
        modelBuilder.Entity<BookGenre>()
            .HasOne(bg => bg.Book)
            .WithMany(b => b.BookGenres)
            .HasForeignKey(bg => bg.BookID);
        modelBuilder.Entity<BookGenre>()
            .HasOne(bg => bg.Genre)
            .WithMany(g => g.BookGenres)
            .HasForeignKey(bg => bg.GenreID);

        modelBuilder.Entity<Borrow>()
            .HasKey(b => b.BorrowID);
        modelBuilder.Entity<Borrow>()
            .HasOne(b => b.User)
            .WithMany(u => u.Borrows)
            .HasForeignKey(b => b.UserID)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BorrowDetail>()
            .HasKey(bd => bd.BorrowDetailID);
        modelBuilder.Entity<BorrowDetail>()
            .HasOne(bd => bd.Borrow)
            .WithMany(b => b.BorrowDetails)
            .HasForeignKey(bd => bd.BorrowID)
            .OnDelete(DeleteBehavior.Cascade);  
        modelBuilder.Entity<BorrowDetail>()
            .HasOne(bd => bd.Book)
            .WithMany(b => b.BorrowDetails)
            .HasForeignKey(bd => bd.BookID)
            .OnDelete(DeleteBehavior.Restrict); 
    }
}
