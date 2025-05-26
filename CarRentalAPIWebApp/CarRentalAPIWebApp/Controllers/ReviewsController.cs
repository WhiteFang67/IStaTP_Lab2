using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarRentalAPIWebApp.Models;

namespace CarRentalAPIWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly CarRentalAPIContext _context;

        public ReviewsController(CarRentalAPIContext context)
        {
            _context = context;
        }

        // DTO для відгуків (залишаємо як є, але синхронізуємо з ReviewDto.cs)
        public class ReviewDto
        {
            public int Id { get; set; }
            public string UserName { get; set; }
            public string Comment { get; set; }
            public DateTime Date { get; set; }
        }

        // GET: api/Reviews
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews()
        {
            var reviews = await _context.Reviews
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.UserName,
                    Comment = r.Comment,
                    Date = r.Date
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // GET: api/Reviews/5
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

            return Ok(review);
        }

        // POST: api/Reviews
        [HttpPost]
        public async Task<ActionResult<ReviewDto>> PostReview([FromBody] ReviewDto reviewDto)
        {
            if (reviewDto == null)
            {
                return BadRequest("Дані відгуку не надано.");
            }

            if (string.IsNullOrWhiteSpace(reviewDto.UserName))
            {
                return BadRequest("Ім'я користувача є обов'язковим.");
            }

            if (string.IsNullOrWhiteSpace(reviewDto.Comment))
            {
                return BadRequest("Коментар є обов'язковим.");
            }

            if (reviewDto.Comment.Length > 500)
            {
                return BadRequest("Коментар не може перевищувати 500 символів.");
            }

            var review = new Review
            {
                UserName = reviewDto.UserName,
                Comment = reviewDto.Comment,
                Date = reviewDto.Date != default ? reviewDto.Date : DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            reviewDto.Id = review.Id;
            reviewDto.Date = review.Date;
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, reviewDto);
        }

        // PUT: api/Reviews/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, [FromBody] ReviewDto reviewDto)
        {
            if (id != reviewDto.Id)
            {
                return BadRequest("ID відгуку не співпадає.");
            }

            if (string.IsNullOrWhiteSpace(reviewDto.UserName))
            {
                return BadRequest("Ім'я користувача є обов'язковим.");
            }

            if (string.IsNullOrWhiteSpace(reviewDto.Comment))
            {
                return BadRequest("Коментар є обов'язковим.");
            }

            if (reviewDto.Comment.Length > 500)
            {
                return BadRequest("Коментар не може перевищувати 500 символів.");
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound(new { message = "Відгук не знайдено." });
            }

            review.UserName = reviewDto.UserName;
            review.Comment = reviewDto.Comment;
            review.Date = reviewDto.Date != default ? reviewDto.Date : review.Date;

            _context.Entry(review).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewExists(id))
                {
                    return NotFound(new { message = "Відгук не знайдено." });
                }
                throw;
            }

            return Ok(reviewDto);
        }

        // DELETE: api/Reviews/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound(new { message = "Відгук не знайдено." });
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Відгук успішно видалено." });
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(r => r.Id == id);
        }
    }
}