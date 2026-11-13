using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineLearning.Models;

namespace OnlineLearning.Data.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.FullName).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Email).IsRequired().HasMaxLength(150);
        builder.Property(i => i.Bio).HasMaxLength(500);
        builder.Property(i => i.JoinedAt).IsRequired();

        builder.HasIndex(i => i.Email).IsUnique();


        builder.HasMany(i => i.Courses)
               .WithOne(c => c.Instructor)
               .HasForeignKey(c => c.InstructorId)
               .OnDelete(DeleteBehavior.Restrict);


        Seed(builder);
    }

    private static void Seed(EntityTypeBuilder<Instructor> builder)
    {
        builder.HasData(
            new Instructor
            {
                Id = 1,
                FullName = "Ahmed Hassan",
                Email = "ahmed.hassan@learn.com",
                Bio = "Senior .NET developer and trainer.",
                JoinedAt = new DateTime(2022, 1, 15)
            },
            new Instructor
            {
                Id = 2,
                FullName = "Sara Mostafa",
                Email = "sara.mostafa@learn.com",
                Bio = "Software architect, 10 years of experience.",
                JoinedAt = new DateTime(2022, 6, 1)
            },
            new Instructor
            {
                Id = 3,
                FullName = "Omar Khaled",
                Email = "omar.khaled@learn.com",
                Bio = null,
                JoinedAt = new DateTime(2023, 3, 20)
            });
    }
}
