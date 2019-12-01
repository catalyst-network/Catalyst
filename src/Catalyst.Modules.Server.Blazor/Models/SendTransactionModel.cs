using System.ComponentModel.DataAnnotations;

namespace Catalyst.Modules.Server.Blazor.Models
{
    public class SendTransactionModel
    {
        [Required]
        [Range(1, 100000, ErrorMessage = "Amount invalid (1-100000).")]
        public double Amount { get; set; }
    }
}
