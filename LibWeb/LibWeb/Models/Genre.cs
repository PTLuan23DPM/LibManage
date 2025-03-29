using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class Genre
{
    [Key]
    [MaxLength(20)]
    public string GenreID { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    public ICollection<BookGenre> BookGenres { get; set; }
}
