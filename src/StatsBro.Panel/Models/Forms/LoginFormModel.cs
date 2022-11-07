using System.ComponentModel.DataAnnotations;

namespace StatsBro.Panel.Models.Forms
{
    public class LoginFormModel : FormModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Podaj adres email")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Twój adres email ma nieprawidłowy format")]
        public string Email { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Podaj hasło")]
        public string Password { get; set; } = null!;
    }
}
