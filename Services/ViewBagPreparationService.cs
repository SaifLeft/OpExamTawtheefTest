using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;

namespace TawtheefTest.Services
{
  // IViewBagPreparationService.cs
  public interface IViewBagPreparationService
  {
    List<SelectListItem> GetQuestionTypes();
    List<SelectListItem> GetDifficultyLevels();
    List<SelectListItem> GetContentSourceTypes();
    void PrepareCreateQuestionSetViewBags(Controller controller);

    void SetJobsSelectList(Controller controller, int? selectedJobId = null);
    void SetPhoneNumberInViewData(Controller controller, string phoneNumber);
    void KeepTempData(Controller controller, params string[] keys);
  }

  // ViewBagPreparationService.cs
  public class ViewBagPreparationService : IViewBagPreparationService
  {
    private readonly ApplicationDbContext _context;
    public ViewBagPreparationService(ApplicationDbContext context)
    {
      _context = context;
    }

    public void SetJobsSelectList(Controller controller, int? selectedJobId = null)
    {
      controller.ViewBag.Jobs = new SelectList(_context.Jobs, "Id", "Title", selectedJobId);
    }

    public void SetPhoneNumberInViewData(Controller controller, string phoneNumber)
    {
      controller.ViewData["PhoneNumber"] = phoneNumber;
    }

    public void KeepTempData(Controller controller, params string[] keys)
    {
      foreach (var key in keys)
      {
        controller.TempData.Keep(key);
      }
    }
    public List<SelectListItem> GetQuestionTypes()
    {
      return new List<SelectListItem>
        {
            new SelectListItem { Value = "MCQ", Text = "اختيار من متعدد" },
            new SelectListItem { Value = "TF", Text = "صح / خطأ" },
            new SelectListItem { Value = "open", Text = "إجابة مفتوحة" },
            new SelectListItem { Value = "fillInTheBlank", Text = "ملء الفراغات" },
            new SelectListItem { Value = "ordering", Text = "ترتيب" },
            new SelectListItem { Value = "matching", Text = "مطابقة" },
            new SelectListItem { Value = "multiSelect", Text = "اختيار متعدد (أكثر من إجابة)" },
            new SelectListItem { Value = "shortAnswer", Text = "إجابة قصيرة" }
        };
    }

    public List<SelectListItem> GetDifficultyLevels()
    {
      return new List<SelectListItem>
        {
            new SelectListItem { Value = "auto", Text = "تلقائي" },
            new SelectListItem { Value = "easy", Text = "سهل" },
            new SelectListItem { Value = "medium", Text = "متوسط" },
            new SelectListItem { Value = "hard", Text = "صعب" }
        };
    }

    public List<SelectListItem> GetContentSourceTypes()
    {
      return new List<SelectListItem>
        {
            new SelectListItem { Value = "text", Text = "نص" },
            new SelectListItem { Value = "topic", Text = "موضوع" },
            new SelectListItem { Value = "link", Text = "رابط" },
            new SelectListItem { Value = "youtube", Text = "فيديو يوتيوب" },
            new SelectListItem { Value = "document", Text = "مستند (PDF/Word)" },
            new SelectListItem { Value = "image", Text = "صورة" },
            new SelectListItem { Value = "audio", Text = "ملف صوتي" },
            new SelectListItem { Value = "video", Text = "فيديو" }
        };
    }

    public void PrepareCreateQuestionSetViewBags(Controller controller)
    {
      controller.ViewBag.QuestionTypes = GetQuestionTypes();
      controller.ViewBag.DifficultyLevels = GetDifficultyLevels();
      controller.ViewBag.ContentSourceTypes = GetContentSourceTypes();
    }
  }
}
