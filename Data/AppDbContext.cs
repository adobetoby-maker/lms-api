using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using lms_api.Models;

namespace lms_api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<Invite> Invites => Set<Invite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Course → Videos
        builder.Entity<Video>()
            .HasOne(v => v.Course)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Course → Questions
        builder.Entity<Question>()
            .HasOne(q => q.Course)
            .WithMany(c => c.Questions)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrollment → ApplicationUser
        builder.Entity<Enrollment>()
            .HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrollment → Course
        builder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // QuizAttempt → Enrollment
        builder.Entity<QuizAttempt>()
            .HasOne(qa => qa.Enrollment)
            .WithMany(e => e.QuizAttempts)
            .HasForeignKey(qa => qa.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Invite → Course
        builder.Entity<Invite>()
            .HasOne(i => i.Course)
            .WithMany()
            .HasForeignKey(i => i.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on invite token
        builder.Entity<Invite>()
            .HasIndex(i => i.Token)
            .IsUnique();

        // Unique index on enrollment (user + course)
        builder.Entity<Enrollment>()
            .HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique();
    }
}
