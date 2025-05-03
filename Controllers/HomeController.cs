using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using System.Threading.Tasks;

namespace TawtheefTest.Controllers
{
  public class HomeController : Controller
  {
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<IActionResult> Index()
    {
      ViewData["JobsCount"] = await _context.Jobs.CountAsync();
      ViewData["CandidatesCount"] = await _context.Candidates.CountAsync();
      ViewData["ExamsCount"] = await _context.Exams.CountAsync();

      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View();
    }
  }
}
