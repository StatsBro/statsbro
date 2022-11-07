using System.ComponentModel.DataAnnotations;

namespace StatsBro.Panel.Models.Forms
{
    public class RegisterFormModel : FormModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email empty")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Email incorrect")]
        public string Email { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password empty")]
        [MinLength(3, ErrorMessage = "Password too short")]
        public string Password { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Url empty")]
        [DataType(DataType.Url, ErrorMessage = "Url incorrect")]
        public string SiteUrl { get; set; } = null!;
    }
}
