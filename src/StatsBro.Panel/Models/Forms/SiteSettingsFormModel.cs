using System.ComponentModel.DataAnnotations;

namespace StatsBro.Panel.Models.Forms
{
    public class SiteSettingsFormModel : FormModel
    {
        public Guid Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Podaj adres twojej strony")]
        [DataType(DataType.Url, ErrorMessage = "Adres twojej strony ma nieprawidłowy format")]
        public string SiteUrl { get; set; } = null!;

        public string? Domain { get; set; }

        public string? PersistQueryParamsList { get; set; }
        public string? IgnoreIPsList { get; set; }

    }
}
