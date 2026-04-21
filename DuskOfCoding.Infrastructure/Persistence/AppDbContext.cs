using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DuskOfCoding.Domain.Entities;

namespace DuskOfCoding.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskDefinition> Tasks => Set<TaskDefinition>();
    public DbSet<TaskTest> TaskTests => Set<TaskTest>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<FeedbackRecord> FeedbackRecords => Set<FeedbackRecord>();
    public DbSet<UserFeedback> UserFeedbacks => Set<UserFeedback>();
    public DbSet<LlmTelemetryLog> LlmTelemetryLogs => Set<LlmTelemetryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TaskDefinition configuration
        modelBuilder.Entity<TaskDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.DifficultyLevel).HasMaxLength(50);
            
            // 1-N relationship with TaskTest
            entity.HasMany(e => e.Tests)
                  .WithOne(t => t.TaskDefinition)
                  .HasForeignKey(t => t.TaskDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);

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

        // UserFeedback configuration
        modelBuilder.Entity<UserFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FeedbackType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Comment).HasMaxLength(2000);

            // Index for efficient per-task, per-user queries and aggregation
            entity.HasIndex(e => new { e.TaskId, e.UserId });
        });

        // LlmTelemetryLog configuration
        modelBuilder.Entity<LlmTelemetryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasOne(e => e.Task)
                  .WithMany()
                  .HasForeignKey(e => e.TaskId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed a default task
        var seedTaskId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<TaskDefinition>().HasData(new TaskDefinition
        {
            Id = seedTaskId,
            Title = "Hello World",
            Description = "Write a method that returns the string 'Hello World!'",
            DifficultyLevel = "Easy",
            Tags = new List<string> { "fundamentals" }
        });

        modelBuilder.Entity<TaskTest>().HasData(new TaskTest
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            TaskDefinitionId = seedTaskId,
            Name = "SolutionTests.cs",
            Code = @"using System;
using Xunit;

public class SolutionTests 
{
    [Fact]
    public void TestHelloWorld() 
    {
        var result = Solution.GetHelloWorld();
        Assert.Equal(""Hello World!"", result);
    }
}"
        });
    }
}
