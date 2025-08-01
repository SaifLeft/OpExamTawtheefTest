﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace TawtheefTest.Data.Structure;

public partial class Exam
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public string Name { get; set; }

    public string Status { get; set; }

    public int Duration { get; set; }

    public int TotalQuestionsPerCandidate { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool ShowResultsImmediately { get; set; }

    public bool SendExamLinkToApplicants { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual ICollection<ExamQuestionSetMapping> ExamQuestionSetMappings { get; set; } = new List<ExamQuestionSetMapping>();

    public virtual Job Job { get; set; }
}