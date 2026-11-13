using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineLearning.Models;

namespace OnlineLearning.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {

        builder.HasKey(e => new { e.StudentId, e.CourseId });

        builder.Property(e => e.EnrolledAt).IsRequired();
        builder.Property(e => e.Progress).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.FinalGrade).HasPrecision(5, 2);

        builder.ToTable(t => t.HasCheckConstraint("CK_Enrollment_Progress", "[Progress] BETWEEN 0 AND 100"));


    }
}
