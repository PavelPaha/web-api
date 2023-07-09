using System;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class UserDto
    { 
        public Guid Id { get; set; }
        
        [Required]
        public string Login { get; set; }
        public string FullName { get; set; }
        public int GamesPlayed { get; set; }
        public Guid? CurrentGameId { get; set; }
    }
}