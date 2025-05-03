using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace TawtheefTest.ViewModels
{
  public class CreateCandidatesVM
  {
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public int JobId { get; set; }
  }
}
