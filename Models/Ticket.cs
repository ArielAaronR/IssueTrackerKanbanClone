using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace IssueTracker.Models
{
    public class DateInTheFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if ((DateTime)value <= DateTime.Now)
                return new ValidationResult("Date must be in the future");
            return ValidationResult.Success;
        }
    }
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }
        [Required]
        [MinLength(2)]
        public string Title { get; set; }
        [Required]
        [MinLength(5)]
        public string Description { get; set; }
        [Required]
        public string Priority { get; set; }
        [Required]
        [DateInTheFuture(ErrorMessage = " Must be a future date")]
        public DateTime Deadline { get; set; }
        [Required]
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;


        // One to Many must set who is creating the Ticket
        public int UserId { get; set; }
        // Navigation property for the One to Many of the one who making the event
        public Admin Creator { get; set; }
        public User AssignedUser { get; set; }
        public List<Comment> Comments { get; set; }

    }

}