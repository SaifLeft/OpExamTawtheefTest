using System.Collections.Generic;
using System.Threading.Tasks;
using TawtheefTest.DTOs.ExamModels;

namespace TawtheefTest.Services.Interfaces
{
  public interface IJobService
  {
    Task<List<JobDTO>> GetAllJobsAsync();
    Task<JobDTO> GetJobByIdAsync(int id);
    Task<JobDTO> GetJobDetailsByIdAsync(int id);
    Task<bool> CreateJobAsync(JobDTO jobDTO);
    Task<bool> UpdateJobAsync(int id, JobDTO jobDTO);
    Task<bool> DeleteJobAsync(int id);
    Task<bool> JobExistsAsync(int id);
    Task<bool> CanDeleteJobAsync(int id);
  }
}
