using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
    public class CreateJobViewModel
    {
        [Required(ErrorMessage = "اسم الوظيفة مطلوب")]
        public string Name { get; set; }
    }
}