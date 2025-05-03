using Microsoft.EntityFrameworkCore;

namespace TawtheefTest.Data.Structure
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<Candidate> Candidates { get; set; } = null!;
    public DbSet<ContentSource> ContentSources { get; set; } = null!;
    public DbSet<UploadedFile> UploadedFiles { get; set; } = null!;
    public DbSet<QuestionSet> QuestionSets { get; set; } = null!;
    public DbSet<Exam> Exams { get; set; } = null!;
    public DbSet<ExamQuestionSet> ExamQuestionSets { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
    public DbSet<OptionChoice> OptionChoices { get; set; } = null!;
    public DbSet<MatchingPair> MatchingPairs { get; set; } = null!;
    public DbSet<OrderingItem> OrderingItems { get; set; } = null!;
    public DbSet<CandidateExam> CandidateExams { get; set; } = null!;
    public DbSet<CandidateAnswer> CandidateAnswers { get; set; } = null!;
    public DbSet<OTPVerification> OTPVerifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Configure relationships
      modelBuilder.Entity<Job>()
          .HasMany(j => j.Candidates)
          .WithOne(c => c.Job)
          .HasForeignKey(c => c.JobId);

      modelBuilder.Entity<Job>()
          .HasMany(j => j.Exams)
          .WithOne(e => e.Job)
          .HasForeignKey(e => e.JobId);

      modelBuilder.Entity<Exam>()
          .HasMany(e => e.Questions)
          .WithOne(q => q.Exam)
          .HasForeignKey(q => q.ExamId);

      modelBuilder.Entity<Question>()
          .HasMany(q => q.Options)
          .WithOne(o => o.Question)
          .HasForeignKey(o => o.QuestionId);

      modelBuilder.Entity<Candidate>()
          .HasMany(c => c.CandidateExams)
          .WithOne(ce => ce.Candidate)
          .HasForeignKey(ce => ce.CandidateId);

      modelBuilder.Entity<Exam>()
          .HasMany(e => e.CandidateExams)
          .WithOne(ce => ce.Exam)
          .HasForeignKey(ce => ce.ExamId);

      modelBuilder.Entity<CandidateExam>()
          .HasMany(ce => ce.CandidateAnswers)
          .WithOne(ca => ca.CandidateExam)
          .HasForeignKey(ca => ca.CandidateExamId);

      modelBuilder.Entity<Question>()
          .HasMany(q => q.CandidateAnswers)
          .WithOne(ca => ca.Question)
          .HasForeignKey(ca => ca.QuestionId);

      // QuestionSet relationships
      modelBuilder.Entity<QuestionSet>()
          .HasMany(qs => qs.Questions)
          .WithOne(q => q.QuestionSet)
          .HasForeignKey(q => q.QuestionSetId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<QuestionSet>()
          .HasMany(qs => qs.ContentSources)
          .WithOne(cs => cs.QuestionSet)
          .HasForeignKey(cs => cs.QuestionSetId)
          .OnDelete(DeleteBehavior.Cascade);

      // ContentSource relationships
      modelBuilder.Entity<ContentSource>()
          .HasOne(cs => cs.UploadedFile)
          .WithMany(uf => uf.ContentSources)
          .HasForeignKey(cs => cs.UploadedFileId)
          .OnDelete(DeleteBehavior.SetNull);

      // ExamQuestionSet relationships
      modelBuilder.Entity<ExamQuestionSet>()
          .HasOne(eqs => eqs.Exam)
          .WithMany(e => e.ExamQuestionSets)
          .HasForeignKey(eqs => eqs.ExamId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<ExamQuestionSet>()
          .HasOne(eqs => eqs.QuestionSet)
          .WithMany(qs => qs.ExamQuestionSets)
          .HasForeignKey(eqs => eqs.QuestionSetId)
          .OnDelete(DeleteBehavior.NoAction); // Avoid circular cascade

      // Question type specific relationships
      modelBuilder.Entity<Question>()
          .HasMany(q => q.OptionChoices)
          .WithOne(oc => oc.Question)
          .HasForeignKey(oc => oc.QuestionId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<Question>()
          .HasMany(q => q.MatchingPairs)
          .WithOne(mp => mp.Question)
          .HasForeignKey(mp => mp.QuestionId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<Question>()
          .HasMany(q => q.OrderingItems)
          .WithOne(oi => oi.Question)
          .HasForeignKey(oi => oi.QuestionId)
          .OnDelete(DeleteBehavior.Cascade);
    }
  }
}
