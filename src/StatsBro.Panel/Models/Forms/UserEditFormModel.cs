using System.ComponentModel.DataAnnotations;

namespace StatsBro.Panel.Models.Forms
{
    public class UserEditFormModel : FormModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Podaj adres email")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Twój adres email ma nieprawidłowy format")]
        public string Email { get; set; } = null!;

        public string? Password { get; set; }

        public Guid Id { get; set; }
    }
}
