using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs.ExamModels;
using TawtheefTest.Services.Interfaces;

namespace TawtheefTest.Services
{
  public class JobService : IJobService
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public JobService(ApplicationDbContext context, IMapper mapper)
    {
      _context = context;
      _mapper = mapper;
    }

    public async Task<List<JobDTO>> GetAllJobsAsync()
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

      return jobs;
    }

    public async Task<JobDTO> GetJobByIdAsync(int id)
    {
      var job = await _context.Jobs.FindAsync(id);
      if (job == null)
      {
        return null;
      }

      var jobDTO = new JobDTO
      {
        Id = job.Id,
        Name = job.Title,
        Code = job.IsActive ? Guid.NewGuid().ToString("N").Substring(0, 8) : "",
        CreatedDate = job.CreatedAt
      };

      return jobDTO;
    }

    public async Task<JobDTO> GetJobDetailsByIdAsync(int id)
    {
      var job = await _context.Jobs
          .Include(j => j.Candidates)
          .Include(j => j.Exams)
          .FirstOrDefaultAsync(j => j.Id == id);

      if (job == null)
      {
        return null;
      }

      var jobDTO = new JobDTO
      {
        Id = job.Id,
        Name = job.Title,
        Code = job.IsActive ? Guid.NewGuid().ToString("N").Substring(0, 8) : "",
        CreatedDate = job.CreatedAt,
        CandidateCount = job.Candidates.Count,
        ExamCount = job.Exams.Count
      };

      return jobDTO;
    }

    public async Task<bool> CreateJobAsync(JobDTO jobDTO)
    {
      try
      {
        var job = new Job
        {
          Title = jobDTO.Name,
          Description = string.Empty,
          IsActive = true,
          CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        _context.Add(job);
        await _context.SaveChangesAsync();
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> UpdateJobAsync(int id, JobDTO jobDTO)
    {
      try
      {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null)
        {
          return false;
        }

        job.Title = jobDTO.Name;
        job.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        _context.Update(job);
        await _context.SaveChangesAsync();
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> DeleteJobAsync(int id)
    {
      try
      {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null)
        {
          return false;
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<bool> JobExistsAsync(int id)
    {
      return await _context.Jobs.AnyAsync(e => e.Id == id);
    }

    public async Task<bool> CanDeleteJobAsync(int id)
    {
      var job = await _context.Jobs
          .Include(j => j.Candidates)
          .Include(j => j.Exams)
          .FirstOrDefaultAsync(j => j.Id == id);

      if (job == null)
      {
        return false;
      }

      return !job.Candidates.Any() && !job.Exams.Any();
    }
  }
}
