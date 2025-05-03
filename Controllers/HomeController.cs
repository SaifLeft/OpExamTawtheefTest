using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System.Threading.Tasks;
using AutoMapper;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs.ExamModels;
using System.Collections.Generic;
using System.Linq;

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
      // Get counts for dashboard
      ViewData["JobsCount"] = await _context.Jobs.CountAsync();
      ViewData["CandidatesCount"] = await _context.Candidates.CountAsync();
      ViewData["ExamsCount"] = await _context.Exams.CountAsync();

      // Example of using the layer separation

      // 1. Get data from database (Data.Structure models)
      var recentExams = await _context.Exams
          .Include(e => e.Job)
          .OrderByDescending(e => e.CreatedAt)
          .Take(5)
          .ToListAsync();

      // 2. Map to DTOs (for service layer operations if needed)
      var examDTOs = _mapper.Map<List<ExamDTO>>(recentExams);

      // 3. Map to ViewModels (for the view)
      var examViewModels = _mapper.Map<List<ExamViewModel>>(examDTOs);

      return View(examViewModels);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View();
    }
  }
}
