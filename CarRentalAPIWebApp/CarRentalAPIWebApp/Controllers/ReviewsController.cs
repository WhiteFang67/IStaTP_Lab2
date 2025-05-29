using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRentalAPIWebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CarRentalAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly CarRentalAPIContext _context;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(CarRentalAPIContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO для відгуків (використовується лише для GET)
        public class ReviewDto
        {
            public int Id { get; set; }
            public string UserName { get; set; }
            public string Comment { get; set; }
            public System.DateTime Date { get; set; }
        }

        // GET: api/Reviews/{id}
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
                _logger.LogWarning("Review with ID {Id} not found.", id);
                return NotFound(new { message = "Відгук не знайдено." });
            }

            _logger.LogInformation("Fetched review with ID: {Id}", id);
            return review;
        }

        // POST: api/Reviews
        [HttpPost]
        public async Task<ActionResult<Review>> PostReview(Review review)
        {
            if (review == null || !ModelState.IsValid)
            {
                _logger.LogWarning("Review data is null or validation failed.");
                return BadRequest(ModelState);
            }

            review.Date = System.DateTime.Now; // Дата встановлюється серверно
            _context.Reviews.Add(review);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Review created with ID: {Id}", review.Id);
                return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating review.");
                return StatusCode(500, new { message = "Помилка створення відгуку." });
            }
        }

        // PUT: api/Reviews/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, Review review)
        {
            if (id != review.Id)
            {
                _logger.LogWarning("Review ID mismatch: URL ID {UrlId}, Body ID {BodyId}", id, review.Id);
                return BadRequest(new { message = "Review ID mismatch." });
            }

            if (review == null || !ModelState.IsValid)
            {
                _logger.LogWarning("Review data is null or validation failed for ID {Id}.", id);
                return BadRequest(ModelState);
            }

            var existingReview = await _context.Reviews.FindAsync(id);
            if (existingReview == null)
            {
                _logger.LogWarning("Review with ID {Id} not found.", id);
                return NotFound(new { message = "Відгук не знайдено." });
            }

            existingReview.UserName = review.UserName;
            existingReview.Comment = review.Comment;
            existingReview.Date = System.DateTime.Now; // Оновлюємо дату

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Review with ID {Id} successfully updated.", id);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ReviewExists(id))
                {
                    _logger.LogWarning("Concurrency conflict: Review with ID {Id} not found.", id);
                    return NotFound(new { message = "Відгук не знайдено." });
                }
                _logger.LogError("Concurrency conflict when updating review with ID {Id}.", id);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating review ID: {Id}", id);
                return StatusCode(500, new { message = "Помилка оновлення відгуку." });
            }
        }

        // DELETE: api/Reviews/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                _logger.LogWarning("Review with ID {Id} not found.", id);
                return NotFound(new { message = "Відгук не знайдено." });
            }

            _context.Reviews.Remove(review);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Review deleted with ID: {Id}", id);
                return Ok(new { message = "Відгук успішно видалено." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting review ID: {Id}", id);
                return StatusCode(500, new { message = "Помилка видалення відгуку." });
            }
        }

        private async Task<bool> ReviewExists(int id)
        {
            return await _context.Reviews.AnyAsync(r => r.Id == id);
        }
    }
}