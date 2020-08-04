using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Rock_Paper_Scissors.Models
{
    public class UserModel
    {
        [Key]
        [DisplayName("Name")]
        [StringLength(maximumLength: 30, MinimumLength = 3, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.")]
        [Required]
        public string UserName { get; set; }

        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
    }
}
