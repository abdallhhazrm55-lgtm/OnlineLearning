using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineLearning.Models;

namespace OnlineLearning.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);


        builder.HasOne(c => c.ParentCategory)
               .WithMany(c => c.SubCategories)
               .HasForeignKey(c => c.ParentCategoryId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);


        builder.HasMany(c => c.Courses)
               .WithOne(c => c.Category)
               .HasForeignKey(c => c.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);


        Seed(builder);
    }

    private static void Seed(EntityTypeBuilder<Category> builder)
    {

        builder.HasData(
            new Category { Id = 1, Name = "Programming", Description = "All programming topics", ParentCategoryId = null },
            new Category { Id = 2, Name = "Backend", Description = "Server-side development", ParentCategoryId = 1 },
            new Category { Id = 3, Name = "ASP.NET", Description = "Web development with ASP.NET", ParentCategoryId = 2 },
            new Category { Id = 4, Name = "Design", Description = null, ParentCategoryId = null },
            new Category { Id = 5, Name = "UI Design", Description = "User interface design", ParentCategoryId = 4 });
    }
}
