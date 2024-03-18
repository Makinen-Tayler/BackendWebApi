using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEBAPI.DBContext.Models;

public partial class Post
{
    [Key]
    public Guid PostId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Tags { get; set; }

    public byte[]? Image { get; set; }

    // Foreign key property
    public Guid UserId { get; set; }

    // Navigation property
    //[ForeignKey("UserId")]
    //public virtual required User User { get; set; }

    public DateTime CreatedDate { get; set; }
}