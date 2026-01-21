using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bank.Api.Models
{
    [Table("cards")]
    public class Card
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(19)]
        public string Pan {  get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Balance { get; set; }    
    }
}
