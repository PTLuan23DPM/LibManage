using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibWeb.Models
{
    public class Book
    {
        [Key]
        [MaxLength(20)]
        public string BookID { get; set; }

        [MaxLength(255)]
        public string? ImgFile { get; set; }
        [NotMapped]
        public IFormFile? ImgPath { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        [MaxLength(255)]
        public string Author { get; set; }

        [Required]
        public int AvailableQuantity { get; set; }

        public float AverageRating { get; set; } = 0;

        public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public virtual ICollection<BorrowDetail> BorrowDetails { get; set; } = new List<BorrowDetail>();
        public List<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
        [NotMapped]
        public List<string> SelectedGenreIDs { get; set; } = new List<string>();
    }
}