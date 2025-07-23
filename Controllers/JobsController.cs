using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.ViewModels;
using TawtheefTest.DTOs.ExamModels;
using TawtheefTest.Services.Interfaces;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TawtheefTest.Controllers
{
  public class JobsController : Controller
  {
    private readonly IJobService _jobService;
    private readonly IMapper _mapper;

    public JobsController(IJobService jobService, IMapper mapper)
    {
      _jobService = jobService;
      _mapper = mapper;
    }

    // GET: Jobs
    public async Task<IActionResult> Index()
    {
      var jobDTOs = await _jobService.GetAllJobsAsync();
      var jobViewModels = _mapper.Map<List<JobViewModel>>(jobDTOs);
      return View(jobViewModels);
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

        var result = await _jobService.CreateJobAsync(new JobDTO
        {
          Name = model.Name
        });

        if (result)
        {
          TempData["SuccessMessage"] = "تم إضافة الوظيفة بنجاح";
          return RedirectToAction(nameof(Index));
        }
        else
        {
          TempData["ErrorMessage"] = "حدث خطأ أثناء إضافة الوظيفة";
        }
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

      var jobDTO = await _jobService.GetJobByIdAsync(id.Value);
      if (jobDTO == null)
      {
        return NotFound();
      }

      var viewModel = _mapper.Map<EditJobViewModel>(jobDTO);
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
          var jobDTO = new JobDTO
          {
            Id = model.Id,
            Name = model.Name,
            Code = model.Code,
            CreatedDate = model.CreatedDate
          };

          var result = await _jobService.UpdateJobAsync(id, jobDTO);

          if (result)
          {
            TempData["SuccessMessage"] = "تم تحديث الوظيفة بنجاح";
            return RedirectToAction(nameof(Index));
          }
          else
          {
            TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث الوظيفة";
          }
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!await _jobService.JobExistsAsync(model.Id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
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

      var jobDTO = await _jobService.GetJobDetailsByIdAsync(id.Value);
      if (jobDTO == null)
      {
        return NotFound();
      }

      var viewModel = _mapper.Map<JobViewModel>(jobDTO);
      return View(viewModel);
    }

    // POST: Jobs/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var canDelete = await _jobService.CanDeleteJobAsync(id);
      if (!canDelete)
      {
        TempData["ErrorMessage"] = "لا يمكن حذف الوظيفة لوجود مرشحين أو اختبارات مرتبطة بها";
        return RedirectToAction(nameof(Index));
      }

      var result = await _jobService.DeleteJobAsync(id);
      if (result)
      {
        TempData["SuccessMessage"] = "تم حذف الوظيفة بنجاح";
      }
      else
      {
        TempData["ErrorMessage"] = "حدث خطأ أثناء حذف الوظيفة";
      }

      return RedirectToAction(nameof(Index));
    }
  }
}
