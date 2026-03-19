using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DuskOfCoding.Domain.Entities;

namespace DuskOfCoding.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskDefinition> Tasks => Set<TaskDefinition>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<FeedbackRecord> FeedbackRecords => Set<FeedbackRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TaskDefinition configuration
        modelBuilder.Entity<TaskDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.DifficultyLevel).HasMaxLength(50);
            entity.Property(e => e.TestBundleReference).HasMaxLength(500);

            // Convert List<string> Tags to JSON string for SQLite storage
            var tagsConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
            entity.Property(e => e.Tags).HasConversion(tagsConverter);
        });

        // Submission configuration
        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceCode).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
            entity.HasOne<TaskDefinition>().WithMany().HasForeignKey(e => e.TaskId);
        });

        // FeedbackRecord configuration
        modelBuilder.Entity<FeedbackRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Summary).IsRequired();
            entity.HasOne<Submission>().WithOne(s => s.Feedback).HasForeignKey<FeedbackRecord>(e => e.SubmissionId);
        });

        // Seed a default task
        modelBuilder.Entity<TaskDefinition>().HasData(new TaskDefinition
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Title = "Hello World",
            Description = "Write a method that returns the string 'Hello World!'",
            DifficultyLevel = "Easy",
            Tags = new List<string> { "fundamentals" },
            TestBundleReference = "tasks/helloworld/tests.csproj"
        });
    }
}
