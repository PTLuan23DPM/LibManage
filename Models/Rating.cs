using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibWeb.Models;
public class Rating
{
    [Key]
    [MaxLength(20)]
    public string RatingID { get; set; }

    [Required]
    public string UserID { get; set; }
    [ForeignKey("UserID")]
    public User User { get; set; }

    [Required]
    public string BookID { get; set; }
    [ForeignKey("BookID")]
    public Book Book { get; set; }

    [Required]
    [Range(1, 10)]
    public int RatingValue { get; set; }
}