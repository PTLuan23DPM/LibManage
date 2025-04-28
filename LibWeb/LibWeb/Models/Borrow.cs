using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class Borrow
{
    [Key]
    [MaxLength(20)]
    public string BorrowID { get; set; }

    [Required]
    public string UserID { get; set; }
    [ForeignKey("UserID")]
    public User User { get; set; }

    public ICollection<BorrowDetail> BorrowDetails { get; set; }
}