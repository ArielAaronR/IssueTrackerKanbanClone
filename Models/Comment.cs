using System;
using System.ComponentModel.DataAnnotations;

namespace IssueTracker.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }
        [Required]
        [MinLength(8, ErrorMessage = "Password must be 8 characters or longer!")]
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int UserId { get; set; }
        public int TicketId { get; set; }
        public User CommentedFromUser { get; set; }
        public Ticket CommentedTicket { get; set; }




    }
}