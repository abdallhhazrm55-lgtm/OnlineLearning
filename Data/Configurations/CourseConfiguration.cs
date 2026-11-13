using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineLearning.Models;

namespace OnlineLearning.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Price).HasPrecision(10, 2);
        builder.Property(c => c.Level).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.IsPublished).HasDefaultValue(false);
        builder.Property(c => c.PublishedDate);

        builder.ToTable(t => t.HasCheckConstraint("CK_Course_Price", "[Price] >= 0"));


        builder.HasMany(c => c.Enrollments)
               .WithOne(e => e.Course)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);


        Seed(builder);
    }

    private static void Seed(EntityTypeBuilder<Course> builder)
    {
        builder.HasData(
            new Course
            {
                Id = 1,
                Title = "ASP.NET Core Fundamentals",
                Description = "Build web apps from scratch.",
                Price = 499.99m,
                Level = CourseLevel.Beginner,
                IsPublished = true,
                PublishedDate = new DateTime(2023, 2, 1),
                InstructorId = 1,
                CategoryId = 3
            },
            new Course
            {
                Id = 2,
                Title = "Advanced EF Core",
                Description = "Performance, tracking and migrations.",
                Price = 799.00m,
                Level = CourseLevel.Advanced,
                IsPublished = true,
                PublishedDate = new DateTime(2023, 5, 10),
                InstructorId = 1,
                CategoryId = 3
            },
            new Course
            {
                Id = 3,
                Title = "C# Basics",
                Description = "Learn C# step by step.",
                Price = 299.00m,
                Level = CourseLevel.Beginner,
                IsPublished = true,
                PublishedDate = new DateTime(2022, 9, 5),
                InstructorId = 2,
                CategoryId = 1
            },
            new Course
            {
                Id = 4,
                Title = "Design Patterns in C#",
                Description = "GoF patterns with real examples.",
                Price = 650.00m,
                Level = CourseLevel.Intermediate,
                IsPublished = false,
                PublishedDate = null,
                InstructorId = 2,
                CategoryId = 2
            },
            new Course
            {
                Id = 5,
                Title = "UI Design Principles",
                Description = "Layout, color and typography.",
                Price = 350.00m,
                Level = CourseLevel.Beginner,
                IsPublished = true,
                PublishedDate = new DateTime(2023, 8, 15),
                InstructorId = 3,
                CategoryId = 5
            },
            new Course
            {
                Id = 6,
                Title = "Figma for Developers",
                Description = null,
                Price = 420.00m,
                Level = CourseLevel.Intermediate,
                IsPublished = false,
                PublishedDate = null,
                InstructorId = 3,
                CategoryId = 5
            });
    }
}
