using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WEBAPI.DBContext;
using WEBAPI.DBContext.Models;

namespace WEBAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostController(MyDBContext context, ILogger<PostController> logger) : ControllerBase
    {
        private readonly MyDBContext _context = context;
        private readonly ILogger<PostController> _logger = logger;

        [HttpGet("posts")]
        public IActionResult GetAllPosts()
        {
            var posts = _context.Posts.ToList();
            return Ok(posts);
        }

        [HttpPost("create")]
        public IActionResult CreatePost(CreatePostModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the user exists
            var userExists = _context.Users.Any(u => u.UserId == model.UserId);
            if (!userExists)
            {
                return BadRequest(new { message = "User does not exist" });
            }

            var newPost = new Post
            {
                Title = model.Title,
                Description = model.Description,
                Tags = model.Tags,
                Image = model.Image,
                UserId = model.UserId,
                CreatedDate = DateTime.Now
            };

            _context.Posts.Add(newPost);
            _context.SaveChanges();

            return Ok(new { message = "Post created successfully", newPost });
        }

        [HttpPut("update")]
        public IActionResult UpdatePost([FromBody] UpdatePostModel model)
        {
            try
            {
                var postToUpdate = _context.Posts.FirstOrDefault(p => p.PostId == model.PostId);

                if (postToUpdate == null)
                {
                    return NotFound(); // Return 404 if post is not found
                }

                // Update post properties
                postToUpdate.Title = model.Title;
                postToUpdate.Description = model.Description;

                _context.SaveChanges(); // Save changes to the database

                return Ok(new { message = "Post updated successfully", postToUpdate }); // Return the updated post
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("delete")]
        public IActionResult DeletePosts([FromBody] List<Guid> postIds)
        {
            if (postIds == null || postIds.Count == 0)
            {
                return BadRequest(new { message = "Post IDs cannot be empty" });
            }

            var postsToDelete = _context.Posts.Where(p => postIds.Contains(p.PostId)).ToList();

            if (postsToDelete.Count == 0)
            {
                return NotFound(new { message = "No posts found with the provided IDs" });
            }

            _context.Posts.RemoveRange(postsToDelete);
            _context.SaveChanges();

            return Ok(new { message = $"{postsToDelete.Count} post(s) deleted successfully" });
        }

        public class CreatePostModel
        {
            public required string Title { get; set; }
            public string? Description { get; set; }
            public string? Tags { get; set; }
            public byte[]? Image { get; set; }
            public Guid UserId { get; set; }

        }

        public class UpdatePostModel
        {
            public Guid PostId { get; set; }
            public required string Title { get; set; }
            public string? Description { get; set; }
        }
    }
}
