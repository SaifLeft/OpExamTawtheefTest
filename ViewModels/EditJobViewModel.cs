using System;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.ViewModels
{
    public class EditJobViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الوظيفة مطلوب")]
        public string Name { get; set; }

        public string Code { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}