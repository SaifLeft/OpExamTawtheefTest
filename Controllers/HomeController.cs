using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System.Threading.Tasks;
using AutoMapper;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs.ExamModels;
using System.Collections.Generic;
using System.Linq;
using System;

namespace TawtheefTest.Controllers
{
  public class HomeController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public HomeController(ApplicationDbContext context, IMapper mapper)
    {
      _context = context;
      _mapper = mapper;
    }

    public async Task<IActionResult> Index()
    {
      // الحصول على إحصائيات للوحة المعلومات
      ViewData["JobsCount"] = await _context.Jobs.CountAsync();
      ViewData["CandidatesCount"] = await _context.Candidates.CountAsync();
      ViewData["ExamsCount"] = await _context.Exams.CountAsync();
      ViewData["QuestionSetsCount"] = await _context.QuestionSets.CountAsync();
      ViewData["CompletedExamsCount"] = await _context.CandidateExams.Where(ce => ce.Status == "Completed").CountAsync();

      // الحصول على آخر 5 اختبارات
      var recentExams = await _context.Exams
          .Include(e => e.Job)
          .OrderByDescending(e => e.CreatedAt)
          .Take(5)
          .ToListAsync();

      // تحويل البيانات إلى DTOs
      var examDTOs = _mapper.Map<List<ExamDTO>>(recentExams);

      // تحويل DTOs إلى ViewModels للعرض
      var examViewModels = examDTOs.Select(dto => new ExamViewModel
      {
        Exam = dto
      }).ToList();

      return View(examViewModels);
    }

    public async Task<IActionResult> Dashboard()
    {
      // الحصول على إحصائيات مفصلة
      var dashboardModel = new DashboardViewModel
      {
        JobsCount = await _context.Jobs.CountAsync(),
        CandidatesCount = await _context.Candidates.CountAsync(),
        ExamsCount = await _context.Exams.CountAsync(),
        QuestionSetsCount = await _context.QuestionSets.CountAsync(),
        CompletedExamsCount = await _context.CandidateExams.Where(ce => ce.Status == "Completed").CountAsync(),

        // أحدث الوظائف
        RecentJobs = await _context.Jobs
              .OrderByDescending(j => j.CreatedAt)
              .Take(5)
              .Select(j => new JobViewModel
              {
                Id = j.Id,
                Name = j.Title,
                CreatedDate = j.CreatedAt
              })
              .ToListAsync(),
      };

      // أحدث المرشحين - تمت معالجتها بشكل منفصل
      var recentCandidates = await _context.Candidates
          .Include(c => c.Job)
          .OrderByDescending(c => c.CreatedAt)
          .Take(5)
          .ToListAsync();

      dashboardModel.RecentCandidates = recentCandidates
          .Select(c => new CandidateViewModel
          {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            JobId = c.JobId,
            JobTitle = c.Job != null ? c.Job.Title : string.Empty,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
          })
          .ToList();

      return View(dashboardModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View();
    }

    [HttpPost]
    public IActionResult SetSuccessMessage([FromBody] SuccessMessageModel model)
    {
      TempData["SuccessMessage"] = model.Message;
      return Ok();
    }

    public class SuccessMessageModel
    {
      public string Message { get; set; }
    }
  }
}
