using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs.ExamModels;
using System;
using System.Linq;
using System.Threading.Tasks;

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
      var jobs = await _context.Jobs
          .Include(j => j.Candidates)
          .Include(j => j.Exams)
          .Select(j => new JobDTO
          {
            Id = j.Id,
            Name = j.Title,
            Code = j.IsActive ? Guid.NewGuid().ToString("N").Substring(0, 8) : "",
            CreatedDate = j.CreatedAt,
            CandidateCount = j.Candidates.Count,
            ExamCount = j.Exams.Count
          })
          .ToListAsync();

      return View(jobs);
    }

    // GET: Jobs/Create
    public IActionResult Create()
    {
      return View();
    }

    // POST: Jobs/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateJobViewModel model)
    {
      if (ModelState.IsValid)
      {
        var job = new Job
        {
          Title = model.Name,
          Description = string.Empty,
          IsActive = true,
          CreatedAt = DateTime.UtcNow
        };

        _context.Add(job);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم إضافة الوظيفة بنجاح";
        return RedirectToAction(nameof(Index));
      }

      return View(model);
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

      var viewModel = new EditJobViewModel
      {
        Id = job.Id,
        Name = job.Title,
        Code = job.IsActive ? Guid.NewGuid().ToString("N").Substring(0, 8) : "",
        CreatedDate = job.CreatedAt
      };

      return View(viewModel);
    }

    // POST: Jobs/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditJobViewModel model)
    {
      if (id != model.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          var job = await _context.Jobs.FindAsync(id);
          if (job == null)
          {
            return NotFound();
          }

          job.Title = model.Name;
          job.UpdatedAt = DateTime.UtcNow;

          _context.Update(job);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "تم تحديث الوظيفة بنجاح";
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!JobExists(model.Id))
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
      return View(model);
    }

    // GET: Jobs/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var job = await _context.Jobs
          .Include(j => j.Candidates)
          .Include(j => j.Exams)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (job == null)
      {
        return NotFound();
      }

      var jobDto = new JobDTO
      {
        Id = job.Id,
        Name = job.Title,
        Code = job.IsActive ? Guid.NewGuid().ToString("N").Substring(0, 8) : "",
        CreatedDate = job.CreatedAt
      };

      return View(jobDto);
    }

    // POST: Jobs/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var job = await _context.Jobs
          .Include(j => j.Candidates)
          .Include(j => j.Exams)
          .FirstOrDefaultAsync(j => j.Id == id);

      if (job != null)
      {
        if (job.Candidates.Any() || job.Exams.Any())
        {
          TempData["ErrorMessage"] = "لا يمكن حذف الوظيفة لوجود مرشحين أو اختبارات مرتبطة بها";
          return RedirectToAction(nameof(Index));
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "تم حذف الوظيفة بنجاح";
      }

      return RedirectToAction(nameof(Index));
    }

    private bool JobExists(int id)
    {
      return _context.Jobs.Any(e => e.Id == id);
    }
  }
}
