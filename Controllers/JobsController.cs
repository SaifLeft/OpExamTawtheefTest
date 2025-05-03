using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System;
using System.Linq;
using System.Threading.Tasks;
using TawtheefTest.Models;

namespace TawtheefTest.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Jobs
        public async Task<IActionResult> Index()
        {
            return View(await _context.Jobs.ToListAsync());
        }

        // GET: Jobs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Jobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Job job)
        {
            if (ModelState.IsValid)
            {
                // Generate a random 8-digit code
                Random random = new Random();
                job.Code = random.Next(10000000, 99999999).ToString();
                job.CreatedDate = DateTime.UtcNow;

                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        // GET: Jobs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }
            return View(job);
        }

        // POST: Jobs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,CreatedDate")] Job job)
        {
            if (id != job.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing job from database to preserve relationships
                    var existingJob = await _context.Jobs
                        .Include(j => j.Candidates)
                        .Include(j => j.Exams)
                        .FirstOrDefaultAsync(j => j.Id == id);

                    if (existingJob == null)
                    {
                        return NotFound();
                    }

                    // Update only the fields that should be updated
                    existingJob.Name = job.Name;
                    // Code remains the same (no need to update)
                    // CreatedDate remains the same (no need to update)

                    // Mark as modified and save
                    _context.Entry(existingJob).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم تحديث الوظيفة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        // GET: Jobs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // POST: Jobs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.Id == id);
        }
    }
}