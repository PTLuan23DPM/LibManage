using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibWeb.Models;
public class BorrowDetail
{
    [Key]
    [MaxLength(20)]
    public string BorrowDetailID { get; set; }

    [Required]
    public string BorrowID { get; set; }
    public Borrow Borrow { get; set; }

    [Required]
    public string BookID { get; set; }
    public Book Book { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
}
