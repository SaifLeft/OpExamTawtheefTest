using System;

namespace TawtheefTest.DTOs.ExamModels
{
    public class EditJobDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}