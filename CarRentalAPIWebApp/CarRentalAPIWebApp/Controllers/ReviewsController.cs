using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRentalAPIWebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRentalAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly CarRentalAPIContext _context;

        public ReviewsController(CarRentalAPIContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
        }

        public class ReviewDto
        {
            public int Id { get; set; }
            public string UserName { get; set; }
            public string Comment { get; set; }
            public System.DateTime Date { get; set; }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReviewDto>> GetReview(int id)
        {
            var review = await _context.Reviews
                .Where(r => r.Id == id)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.UserName,
                    Comment = r.Comment,
                    Date = r.Date
                })
                .FirstOrDefaultAsync();

            if (review == null)
            {
                return NotFound(new { message = "Відгук не знайдено." });
            }

            return review;
        }

        [HttpPost]
        public async Task<ActionResult<Review>> PostReview(Review review)
        {
            if (review == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            review.Date = System.DateTime.Now;
            _context.Reviews.Add(review);
            try
            {
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "Помилка створення відгуку." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, Review review)
        {
            if (id != review.Id)
            {
                return BadRequest(new { message = "Review ID mismatch." });
            }

            if (review == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingReview = await _context.Reviews.FindAsync(id);
            if (existingReview == null)
            {
                return NotFound(new { message = "Відгук не знайдено." });
            }

            existingReview.UserName = review.UserName;
            existingReview.Comment = review.Comment;
            existingReview.Date = System.DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ReviewExists(id))
                {
                    return NotFound(new { message = "Відгук не знайдено." });
                }
                throw;
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "Помилка оновлення відгуку." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound(new { message = "Відгук не знайдено." });
            }

            _context.Reviews.Remove(review);
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Відгук успішно видалено." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "Помилка видалення відгуку." });
            }
        }

        private async Task<bool> ReviewExists(int id)
        {
            return await _context.Reviews.AnyAsync(r => r.Id == id);
        }
    }
}