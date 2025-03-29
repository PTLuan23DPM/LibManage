using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Key]
    [MaxLength(20)]
    public string UserID { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; }

    [Required]
    [MaxLength(20)]
    [EnumDataType(typeof(UserRole))]
    public string Role { get; set; }

    public ICollection<Rating> Ratings { get; set; }
    public ICollection<Borrow> Borrows { get; set; }
}
public enum UserRole
{
    Admin,
    Thuthu,
    Nguoidung
}