using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PracticePlatform.Domain.Entities;

namespace PracticePlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskDefinition> Tasks => Set<TaskDefinition>();
    public DbSet<Submission> Submissions => Set<Submission>();

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
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasOne<TaskDefinition>().WithMany().HasForeignKey(e => e.TaskId);
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
