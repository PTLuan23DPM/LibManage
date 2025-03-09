using System.ComponentModel.DataAnnotations.Schema;
using LibWeb.Models;
public class BookGenre
{
    public string BookID { get; set; }
    [ForeignKey("BookID")]
    public Book Book { get; set; }

    public string GenreID { get; set; }
    [ForeignKey("GenreID")]
    public Genre Genre { get; set; }
}
