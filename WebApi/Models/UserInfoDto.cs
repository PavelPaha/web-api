using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class UserInfoDto
    {
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }
        
        [DefaultValue("Oleg")]
        public string FirstName { get; set; }
        
        [DefaultValue("Olegov")]
        public string LastName { get; set; }
    }
}