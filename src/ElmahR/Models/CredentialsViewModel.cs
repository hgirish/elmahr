using System.ComponentModel.DataAnnotations;

namespace ElmahR.Models
{
    public class CredentialsViewModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}