using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaBackend.API.Models
{
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [MaxLength(255)]
        public string Caption { get; set; }
        
        public string ImageUrl { get; set; }
        
        public Guid CreatorId { get; set; }
        
        [ForeignKey("CreatorId")]
        public Account Creator { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Comment> Comments { get; set; }
    }
}
