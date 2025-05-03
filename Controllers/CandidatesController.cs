using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System;
using System.Linq;
using System.Threading.Tasks;

using TawtheefTest.Models;

namespace TawtheefTest.Controllers
{
    public class CandidatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CandidatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Candidates
        public async Task<IActionResult> Index()
        {
            var candidates = await _context.Candidates
                .Include(c => c.Job)
                .ToListAsync();
            return View(candidates);
        }

        // GET: Candidates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidates
                .Include(c => c.Job)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidate == null)
            {
                return NotFound();
            }

            return View(candidate);
        }

        // GET: Candidates/Create
        public IActionResult Create()
        {
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name");
            return View();
        }

        // POST: Candidates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,PhoneNumber,JobId")] Candidate candidate)
        {
            if (ModelState.IsValid)
            {
                candidate.RegisteredDate = DateTime.UtcNow;
                _context.Add(candidate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name", candidate.JobId);
            return View(candidate);
        }

        // GET: Candidates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name", candidate.JobId);
            return View(candidate);
        }

        // POST: Candidates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,PhoneNumber,JobId,RegisteredDate")] Candidate candidate)
        {
            if (id != candidate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(candidate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CandidateExists(candidate.Id))
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
            ViewData["JobId"] = new SelectList(_context.Jobs, "Id", "Name", candidate.JobId);
            return View(candidate);
        }

        // GET: Candidates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidates
                .Include(c => c.Job)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidate == null)
            {
                return NotFound();
            }

            return View(candidate);
        }

        // POST: Candidates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate != null)
            {
                _context.Candidates.Remove(candidate);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Candidates/ByJob/5
        public async Task<IActionResult> ByJob(int? id)
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

            var candidates = await _context.Candidates
                .Where(c => c.JobId == id)
                .Include(c => c.Job)
                .ToListAsync();

            ViewData["JobName"] = job.Name;
            ViewData["JobId"] = job.Id;

            return View(candidates);
        }

        private bool CandidateExists(int id)
        {
            return _context.Candidates.Any(e => e.Id == id);
        }
    }
}
