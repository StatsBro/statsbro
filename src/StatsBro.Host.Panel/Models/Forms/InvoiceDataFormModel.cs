using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace StatsBro.Host.Panel.Models.Forms
{
    public class InvoiceDataFormModel
    {
        [Required]
        [MinLength(3)]
        public string Name { get; set; } = null!;

        [Required]
        [MinLength(3)]
        public string AddressLine1 { get; set; } = null!;


        public string PostalCode { get; set; } = null!;

        [Required]
        [MinLength(3)]
        public string City { get; set; } = null!;

        [ValidateNever]
        public string NIP { get; set; } = null!;
    }
}
