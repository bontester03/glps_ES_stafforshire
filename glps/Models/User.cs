using System.ComponentModel.DataAnnotations;

namespace glps.Models
{
    public class User
    {
        [Key]
        public int ID { get; set; }

        [Display(Name = "Email ID")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email ID is required")]
        [DataType(DataType.EmailAddress)]
        public string EmailID { get; set; }

        // Stores a PBKDF2 hash (see Infrastructure/PasswordHasher); legacy rows may still hold
        // plaintext and are upgraded on the next successful login.
        [Display(Name = "Password")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
