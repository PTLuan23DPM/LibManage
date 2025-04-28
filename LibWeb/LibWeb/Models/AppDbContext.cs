using Microsoft.EntityFrameworkCore;
using LibWeb.Models;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<BookGenre> BookGenres { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Borrow> Borrows { get; set; }
    public DbSet<BorrowDetail> BorrowDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique constraint Username
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Unique constraint Genre Name
        modelBuilder.Entity<Genre>()
            .HasIndex(g => g.Name)
            .IsUnique();

        // Unique constraint UserID & BookID trong Ratings
        modelBuilder.Entity<Rating>()
            .HasIndex(r => new { r.UserID, r.BookID })
            .IsUnique();

        // Đảm bảo SQL lưu đúng kiểu double
        modelBuilder.Entity<Book>()
        .Property(b => b.AverageRating)
        .HasColumnType("float");

        //Thiết lập quan hệ n-n giữa Genre và Book qua BookGenre (do SQL k hỗ trợ quan hệ n-n trực tiếp
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
    }
}
