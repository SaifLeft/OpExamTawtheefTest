using Microsoft.EntityFrameworkCore;
using TawtheefTest.Models;

namespace TawtheefTest.Data.Structure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<CandidateExam> CandidateExams { get; set; }
        public DbSet<CandidateAnswer> CandidateAnswers { get; set; }
        public DbSet<OTPVerification> OTPVerifications { get; set; }

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
        }
    }
}
