using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.ExamModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;

namespace TawtheefTest.Controllers
{
  public class CandidatesController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CandidatesController(ApplicationDbContext context, IMapper mapper)
    {
      _context = context;
      _mapper = mapper;
    }

    // GET: Candidates
    public async Task<IActionResult> Index()
    {
      var candidates = await _context.Candidates
          .Include(c => c.Job)
          .ToListAsync();

      var candidateDTOs = _mapper.Map<List<CandidateDTO>>(candidates);

      var viewModel = new CandidateListViewModel
      {
        Candidates = candidateDTOs
      };

      return View(viewModel);
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

      var candidateDto = _mapper.Map<CandidateDTO>(candidate);

      return View(candidateDto);
    }

    // GET: Candidates/Create
    public IActionResult Create()
    {
      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title");
      return View();
    }

    // POST: Candidates/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCandidatesVM model)
    {
      if (ModelState.IsValid)
      {
        var candidate = new Candidate
        {
          Name = model.Name,
          Phone = model.Phone,
          Email = model.Email,
          JobId = model.JobId,
          IsActive = true,
          CreatedAt = DateTime.UtcNow
        };

        _context.Add(candidate);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم إضافة المرشح بنجاح";
        return RedirectToAction(nameof(Index));
      }

      ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
      return View(model);
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

      var viewModel = new EditCandidatesVM
      {
        Id = candidate.Id,
        Name = candidate.Name,
        PhoneNumber = candidate.Phone.ToString(),
        Email = candidate.Email ?? string.Empty,
        JobId = candidate.JobId
      };

      ViewBag.JobId = new SelectList(_context.Jobs, "Id", "Title", candidate.JobId);
      return View(viewModel);
    }

    // POST: Candidates/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditCandidatesVM model)
    {
      if (id != model.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          var candidate = await _context.Candidates.FindAsync(id);
          if (candidate == null)
          {
            return NotFound();
          }

          candidate.Name = model.Name;
          candidate.Phone = int.Parse(model.PhoneNumber);
          candidate.Email = model.Email;
          candidate.JobId = model.JobId;
          candidate.UpdatedAt = DateTime.UtcNow;

          _context.Update(candidate);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "تم تحديث المرشح بنجاح";
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!CandidateExists(model.Id))
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

      ViewBag.JobId = new SelectList(_context.Jobs, "Id", "Title", model.JobId);
      return View(model);
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

      var candidateDto = _mapper.Map<CandidateDTO>(candidate);

      return View(candidateDto);
    }

    // POST: Candidates/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var candidate = await _context.Candidates
          .Include(c => c.CandidateExams)
          .FirstOrDefaultAsync(c => c.Id == id);

      if (candidate != null)
      {
        if (candidate.CandidateExams.Any())
        {
          TempData["ErrorMessage"] = "لا يمكن حذف المرشح لوجود اختبارات مرتبطة به";
          return RedirectToAction(nameof(Index));
        }

        _context.Candidates.Remove(candidate);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "تم حذف المرشح بنجاح";
      }

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

      var candidateDTOs = _mapper.Map<List<CandidateDTO>>(candidates);

      var viewModel = new CandidateListViewModel
      {
        Candidates = candidateDTOs,
        JobName = job.Title,
        JobId = job.Id
      };

      return View(viewModel);
    }

    private bool CandidateExists(int id)
    {
      return _context.Candidates.Any(e => e.Id == id);
    }
  }
}
