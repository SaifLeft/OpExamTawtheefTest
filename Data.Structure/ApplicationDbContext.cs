using Microsoft.EntityFrameworkCore;
using System;
using TawtheefTest.Enums;

namespace TawtheefTest.Data.Structure
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<Candidate> Candidates { get; set; } = null!;
    public DbSet<Exam> Exams { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
    public DbSet<QuestionSet> QuestionSets { get; set; } = null!;
    public DbSet<ExamQuestionSet> ExamQuestionSets { get; set; } = null!;
    public DbSet<CandidateExam> CandidateExams { get; set; } = null!;
    public DbSet<CandidateAnswer> CandidateAnswers { get; set; } = null!;
    public DbSet<OTPVerification> OTPVerifications { get; set; } = null!;
    public DbSet<OptionChoice> OptionChoices { get; set; } = null!;
    public DbSet<MatchingPair> MatchingPairs { get; set; } = null!;
    public DbSet<OrderingItem> OrderingItems { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Job Entity Configuration
      modelBuilder.Entity<Job>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Description)
            .HasMaxLength(500);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        entity.HasMany(e => e.Candidates)
            .WithOne(c => c.Job)
            .HasForeignKey(c => c.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.Exams)
            .WithOne(e => e.Job)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Restrict);
      });

      // Candidate Entity Configuration
      modelBuilder.Entity<Candidate>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);


        entity.Property(e => e.Phone)
            .HasMaxLength(20);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        entity.HasOne(e => e.Job)
            .WithMany(j => j.Candidates)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.CandidateExams)
            .WithOne(ce => ce.Candidate)
            .HasForeignKey(ce => ce.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      // Exam Entity Configuration
      modelBuilder.Entity<Exam>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Description)
            .HasMaxLength(500);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        entity.HasOne(e => e.Job)
            .WithMany(j => j.Exams)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Restrict);


        entity.HasMany(e => e.ExamQuestionSets)
            .WithOne(eqs => eqs.Exam)
            .HasForeignKey(eqs => eqs.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.CandidateExams)
            .WithOne(ce => ce.Exam)
            .HasForeignKey(ce => ce.ExamId)
            .OnDelete(DeleteBehavior.Restrict);
      });

      // QuestionSet Entity Configuration
      modelBuilder.Entity<QuestionSet>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Description)
            .HasMaxLength(500);

        entity.Property(e => e.Language)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Arabic");

        entity.Property(e => e.Difficulty)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("medium");

        entity.Property(e => e.Status)
            .HasDefaultValue(QuestionSetStatus.Pending);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        entity.HasMany(e => e.Questions)
            .WithOne(q => q.QuestionSet)
            .HasForeignKey(q => q.QuestionSetId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.ExamQuestionSets)
            .WithOne(eqs => eqs.QuestionSet)
            .HasForeignKey(eqs => eqs.QuestionSetId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      // ExamQuestionSet Entity Configuration
      modelBuilder.Entity<ExamQuestionSet>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.HasIndex(e => new { e.ExamId, e.QuestionSetId })
            .IsUnique();

        entity.HasOne(e => e.Exam)
            .WithMany(exam => exam.ExamQuestionSets)
            .HasForeignKey(e => e.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.QuestionSet)
            .WithMany(qs => qs.ExamQuestionSets)
            .HasForeignKey(e => e.QuestionSetId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      // Question Entity Configuration
      modelBuilder.Entity<Question>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.QuestionText)
            .IsRequired()
            .HasMaxLength(1000);

        entity.Property(e => e.Answer)
            .HasMaxLength(1000);

        entity.Property(e => e.InstructionText)
            .HasMaxLength(1000);

        entity.Property(e => e.SampleAnswer)
            .HasMaxLength(2000);

        entity.Property(e => e.ExternalId)
            .HasMaxLength(100);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");


        entity.HasOne(e => e.QuestionSet)
            .WithMany(qs => qs.Questions)
            .HasForeignKey(e => e.QuestionSetId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.OptionChoices)
            .WithOne(oc => oc.Question)
            .HasForeignKey(oc => oc.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.MatchingPairs)
            .WithOne(mp => mp.Question)
            .HasForeignKey(mp => mp.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.OrderingItems)
            .WithOne(oi => oi.Question)
            .HasForeignKey(oi => oi.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.CandidateAnswers)
            .WithOne(ca => ca.Question)
            .HasForeignKey(ca => ca.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
      });

      // QuestionOption Entity Configuration
      modelBuilder.Entity<QuestionOption>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(1000);

        entity.HasOne(e => e.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
      });



      // CandidateExam Entity Configuration
      modelBuilder.Entity<CandidateExam>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Status)
            .HasDefaultValue(CandidateExamStatus.NotStarted);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        entity.HasOne(e => e.Candidate)
            .WithMany(c => c.CandidateExams)
            .HasForeignKey(e => e.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Exam)
            .WithMany(exam => exam.CandidateExams)
            .HasForeignKey(e => e.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.CandidateAnswers)
            .WithOne(ca => ca.CandidateExam)
            .HasForeignKey(ca => ca.CandidateExamId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      // CandidateAnswer Entity Configuration
      modelBuilder.Entity<CandidateAnswer>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.AnswerText)
            .HasMaxLength(2000);

        entity.Property(e => e.SelectedOptionsJson)
            .HasMaxLength(2000);

        entity.Property(e => e.MatchingPairsJson)
            .HasMaxLength(2000);

        entity.Property(e => e.OrderingJson)
            .HasMaxLength(2000);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        entity.HasOne(e => e.CandidateExam)
            .WithMany(ce => ce.CandidateAnswers)
            .HasForeignKey(e => e.CandidateExamId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Question)
            .WithMany(q => q.CandidateAnswers)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
      });

      // OptionChoice Entity Configuration
      modelBuilder.Entity<OptionChoice>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(1000);

        entity.HasOne(e => e.Question)
            .WithMany(q => q.OptionChoices)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      // MatchingPair Entity Configuration
      modelBuilder.Entity<MatchingPair>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.LeftItem)
            .IsRequired()
            .HasMaxLength(1000);

        entity.Property(e => e.RightItem)
            .IsRequired()
            .HasMaxLength(1000);

        entity.HasOne(e => e.Question)
            .WithMany(q => q.MatchingPairs)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      // OrderingItem Entity Configuration
      modelBuilder.Entity<OrderingItem>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(1000);

        entity.HasOne(e => e.Question)
            .WithMany(q => q.OrderingItems)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      // OTPVerification Entity Configuration
      modelBuilder.Entity<OTPVerification>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.PhoneNumber)
            .IsRequired();

        entity.Property(e => e.OTPCode)
            .IsRequired()
            .HasMaxLength(6);

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
      });

      // Notification Entity Configuration
      modelBuilder.Entity<Notification>(entity =>
      {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(e => e.Message)
            .IsRequired();

        entity.Property(e => e.Type)
            .HasMaxLength(50)
            .HasDefaultValue("info");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        entity.HasOne(e => e.Candidate)
            .WithMany()
            .HasForeignKey(e => e.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
      });

      modelBuilder.Entity<Candidate>()
          .HasIndex(e => e.Phone)
          .IsUnique();

      modelBuilder.Entity<Exam>()
          .HasIndex(e => e.JobId);


      modelBuilder.Entity<Question>()
          .HasIndex(e => e.QuestionSetId);

      modelBuilder.Entity<CandidateExam>()
          .HasIndex(e => new { e.CandidateId, e.ExamId })
          .IsUnique();

      modelBuilder.Entity<OTPVerification>()
          .HasIndex(e => e.PhoneNumber);
    }
  }
}
