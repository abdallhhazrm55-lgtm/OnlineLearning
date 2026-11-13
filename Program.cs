using Microsoft.EntityFrameworkCore;
using OnlineLearning.Data;
using OnlineLearning.Models;

namespace OnlineLearning;

public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        SeedStudentsAndEnrollments();

        // ---------------- CREATE ----------------
        Header("CREATE");
        int? instructorId = AddInstructor("Mona Adel", "mona.adel@learn.com", "Cloud and DevOps trainer.");
        int? newCourseId = AddCourse("Minimal APIs in Depth", "Fast APIs with .NET.", 550m,
                                     CourseLevel.Intermediate, instructorId ?? 1, categoryId: 3);
        AddCourse("Ghost Course", null, 100m, CourseLevel.Beginner, instructorId: 999, categoryId: 3); // invalid instructor
        AddStudents();
        if (newCourseId.HasValue)
        {
            EnrollStudent(1, newCourseId.Value);   // refused: course is not published yet
        }
        EnrollStudent(1, 5);                        // student 1 -> published course 5 (ok)
        EnrollStudent(1, 5);                        // refused: already enrolled
        EnrollStudent(999, 5);                      // refused: student not found

        // ---------------- READ ----------------
        Header("READ");
        FindCourse(1, "Advanced EF Core");
        FindCourse(999, "Does Not Exist");
        FilterCourses(300m, 700m);
        PrintSubCategories(1);
        PrintTopLevelCategories();
        PrintCourseRoster(1);

        // ---------------- UPDATE ----------------
        Header("UPDATE");
        ChangePrice(3, 349m);
        ChangePrice(999, 100m);
        UpdateProgress(studentId: 1, courseId: 1, newProgress: 70);
        UpdateProgress(studentId: 1, courseId: 1, newProgress: 100);   // -> Completed
        UpdateProgress(studentId: 1, courseId: 1, newProgress: 150);   // rejected
        if (newCourseId.HasValue)
        {
            PublishCourse(newCourseId.Value);
            EnrollStudent(2, newCourseId.Value);   // now accepted
        }
        PublishCourse(4);

        // ---------------- DELETE ----------------
        Header("DELETE");
        Unenroll(studentId: 2, courseId: 3);
        Unenroll(studentId: 2, courseId: 3);       // second time -> friendly message
        DeleteCategoryUnsafe(1);                   // has sub-categories -> DbUpdateException
        DeleteCategorySafe(1);                     // refused with a message
        int? tempCategoryId = AddTempCategory();
        if (tempCategoryId.HasValue) DeleteCategorySafe(tempCategoryId.Value);   // deleted
        if (newCourseId.HasValue) DeleteCourseWithEnrollments(newCourseId.Value);

        // ---------------- BULK ----------------
        Header("BULK OPERATIONS");
        BulkRaisePrice(CourseLevel.Beginner, 10m);
        BulkDeleteDroppedEnrollments();

        Console.WriteLine("\nDone.");
    }

    // =====================================================================
    //  SEEDING FROM CODE
    // =====================================================================
    private static void SeedStudentsAndEnrollments()
    {
        using var db = new AppDbContext();

        // Seed only when the table is empty => running the app twice does not duplicate data.
        if (!db.Students.Any())
        {
            var students = new List<Student>
            {
                new() { FullName = "Youssef Ali",   Email = "youssef.ali@mail.com",   DateOfBirth = new DateTime(2000, 4, 12) },
                new() { FullName = "Mariam Samir",  Email = "mariam.samir@mail.com",  DateOfBirth = new DateTime(1999, 9, 3) },
                new() { FullName = "Karim Nabil",   Email = "karim.nabil@mail.com",   DateOfBirth = new DateTime(2001, 1, 25) },
                new() { FullName = "Nour Ibrahim",  Email = "nour.ibrahim@mail.com",  DateOfBirth = new DateTime(1998, 11, 30) },
                new() { FullName = "Hana Tarek",    Email = "hana.tarek@mail.com",    DateOfBirth = new DateTime(2002, 6, 18) },
            };
            db.Students.AddRange(students);
            db.SaveChanges();
            Console.WriteLine($"Seeded {students.Count} students.");
        }

        if (!db.Enrollments.Any())
        {
            // Look the students up in order so we do not depend on specific Id values.
            var ids = db.Students.OrderBy(s => s.Id).Select(s => s.Id).Take(5).ToList();
            if (ids.Count < 5) return;

            var date = new DateTime(2024, 1, 10);
            var enrollments = new List<Enrollment>
            {
                new() { StudentId = ids[0], CourseId = 1, EnrolledAt = date, Progress = 40,  Status = EnrollmentStatus.Active },
                new() { StudentId = ids[0], CourseId = 2, EnrolledAt = date, Progress = 100, Status = EnrollmentStatus.Completed, FinalGrade = 92.5m },
                new() { StudentId = ids[1], CourseId = 1, EnrolledAt = date, Progress = 100, Status = EnrollmentStatus.Completed, FinalGrade = 88m },
                new() { StudentId = ids[1], CourseId = 3, EnrolledAt = date, Progress = 65,  Status = EnrollmentStatus.Active },
                new() { StudentId = ids[2], CourseId = 3, EnrolledAt = date, Progress = 20,  Status = EnrollmentStatus.Dropped },
                new() { StudentId = ids[2], CourseId = 5, EnrolledAt = date, Progress = 10,  Status = EnrollmentStatus.Active },
                new() { StudentId = ids[3], CourseId = 1, EnrolledAt = date, Progress = 5,   Status = EnrollmentStatus.Dropped },
                new() { StudentId = ids[4], CourseId = 5, EnrolledAt = date, Progress = 100, Status = EnrollmentStatus.Completed, FinalGrade = 95m },
            };
            db.Enrollments.AddRange(enrollments);
            db.SaveChanges();
            Console.WriteLine($"Seeded {enrollments.Count} enrollments.");
        }
    }

    // =====================================================================
    //  CREATE
    // =====================================================================
    private static int? AddInstructor(string name, string email, string? bio)
    {
        using var db = new AppDbContext();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine("Instructor name and email are required.");
            return null;
        }
        if (db.Instructors.Any(i => i.Email == email))
        {
            Console.WriteLine($"An instructor with email '{email}' already exists.");
            return db.Instructors.Where(i => i.Email == email).Select(i => i.Id).First();
        }

        var instructor = new Instructor { FullName = name, Email = email, Bio = bio, JoinedAt = DateTime.UtcNow };
        db.Instructors.Add(instructor);
        db.SaveChanges();

        Console.WriteLine($"Instructor added. Generated Id = {instructor.Id}");
        return instructor.Id;
    }

    private static int? AddCourse(string title, string? description, decimal price, CourseLevel level,
                                  int instructorId, int categoryId)
    {
        using var db = new AppDbContext();

        if (!db.Instructors.Any(i => i.Id == instructorId))
        {
            Console.WriteLine($"Cannot add course '{title}': instructor {instructorId} does not exist.");
            return null;
        }
        if (!db.Categories.Any(c => c.Id == categoryId))
        {
            Console.WriteLine($"Cannot add course '{title}': category {categoryId} does not exist.");
            return null;
        }
        if (price < 0)
        {
            Console.WriteLine("Cannot add course: price cannot be negative.");
            return null;
        }

        var course = new Course
        {
            Title = title,
            Description = description,
            Price = price,
            Level = level,
            IsPublished = false,
            InstructorId = instructorId,
            CategoryId = categoryId
        };
        db.Courses.Add(course);
        db.SaveChanges();

        Console.WriteLine($"Course '{title}' added. Generated Id = {course.Id}");
        return course.Id;
    }

    private static void AddStudents()
    {
        using var db = new AppDbContext();

        // Make the demo re-runnable: emails are unique, so use a unique suffix per run.
        var suffix = DateTime.UtcNow.ToString("HHmmssfff");
        var students = new List<Student>
        {
            new() { FullName = "Laila Fathy",  Email = $"laila.{suffix}@mail.com",  DateOfBirth = new DateTime(2000, 2, 2) },
            new() { FullName = "Ziad Mahmoud", Email = $"ziad.{suffix}@mail.com",   DateOfBirth = new DateTime(1997, 7, 7) },
            new() { FullName = "Salma Reda",   Email = $"salma.{suffix}@mail.com",  DateOfBirth = new DateTime(2003, 3, 3) },
        };

        db.Students.AddRange(students);   // one AddRange ...
        db.SaveChanges();                 // ... and one SaveChanges

        Console.WriteLine("Students added. Generated Ids: " + string.Join(", ", students.Select(s => s.Id)));
    }

    private static void EnrollStudent(int studentId, int courseId)
    {
        using var db = new AppDbContext();

        var student = db.Students.Find(studentId);
        if (student is null)
        {
            Console.WriteLine($"Enrollment refused: student {studentId} not found.");
            return;
        }

        var course = db.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Enrollment refused: course {courseId} not found.");
            return;
        }

        if (!course.IsPublished)
        {
            Console.WriteLine($"Enrollment refused: course '{course.Title}' is not published.");
            return;
        }

        if (db.Enrollments.Any(e => e.StudentId == studentId && e.CourseId == courseId))
        {
            Console.WriteLine($"Enrollment refused: {student.FullName} is already enrolled in '{course.Title}'.");
            return;
        }

        db.Enrollments.Add(new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow,
            Progress = 0,
            Status = EnrollmentStatus.Active
        });
        db.SaveChanges();

        Console.WriteLine($"{student.FullName} enrolled in '{course.Title}'.");
    }

    // =====================================================================
    //  READ
    // =====================================================================
    private static void FindCourse(int id, string title)
    {
        using var db = new AppDbContext();

        var byId = db.Courses.Find(id);   // Find: by primary key (checks the change tracker first)
        Console.WriteLine(byId is null
            ? $"Find({id}): course not found."
            : $"Find({id}): {byId.Title} ({byId.Level}, {byId.Price:C})");

        var byTitle = db.Courses.FirstOrDefault(c => c.Title == title);
        Console.WriteLine(byTitle is null
            ? $"FirstOrDefault('{title}'): course not found."
            : $"FirstOrDefault('{title}'): Id = {byTitle.Id}, {byTitle.Level}, {byTitle.Price:C}");
    }

    private static void FilterCourses(decimal min, decimal max)
    {
        using var db = new AppDbContext();

        Console.WriteLine($"\nCourses priced between {min} and {max} (highest price first):");
        var courses = db.Courses
            .Where(c => c.Price >= min && c.Price <= max)
            .OrderByDescending(c => c.Price)
            .Select(c => new { c.Title, c.Level, c.Price })   // only the columns we print
            .ToList();

        if (courses.Count == 0) Console.WriteLine("  (no courses in this price range)");
        foreach (var c in courses)
            Console.WriteLine($"  {c.Title,-30} {c.Level,-13} {c.Price,8:N2}");
    }

    private static void PrintSubCategories(int parentId)
    {
        using var db = new AppDbContext();

        var parent = db.Categories.Find(parentId);
        if (parent is null)
        {
            Console.WriteLine($"\nCategory {parentId} not found.");
            return;
        }

        Console.WriteLine($"\nDirect sub-categories of '{parent.Name}':");
        var subs = db.Categories.Where(c => c.ParentCategoryId == parentId).Select(c => c.Name).ToList();
        if (subs.Count == 0) Console.WriteLine("  (none)");
        foreach (var name in subs) Console.WriteLine($"  - {name}");
    }

    private static void PrintTopLevelCategories()
    {
        using var db = new AppDbContext();

        Console.WriteLine("\nTop-level categories:");
        var top = db.Categories.Where(c => c.ParentCategoryId == null).Select(c => c.Name).ToList();
        foreach (var name in top) Console.WriteLine($"  - {name}");
    }

    private static void PrintCourseRoster(int courseId)
    {
        using var db = new AppDbContext();

        var course = db.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"\nCourse {courseId} not found.");
            return;
        }

        // No joins: step 1 => student Ids from the enrollments, step 2 => students with Contains.
        var studentIds = db.Enrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => e.StudentId)
            .ToList();

        var names = db.Students
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => s.FullName)
            .ToList();

        Console.WriteLine($"\nCourse '{course.Title}' has {studentIds.Count} enrolled student(s):");
        foreach (var name in names) Console.WriteLine($"  - {name}");
    }

    // =====================================================================
    //  UPDATE
    // =====================================================================
    private static void ChangePrice(int courseId, decimal newPrice)
    {
        using var db = new AppDbContext();

        if (newPrice < 0)
        {
            Console.WriteLine("Price cannot be negative.");
            return;
        }

        var course = db.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Cannot change price: course {courseId} not found.");
            return;
        }

        var old = course.Price;
        course.Price = newPrice;
        db.SaveChanges();

        Console.WriteLine($"Price of '{course.Title}' changed from {old:N2} to {newPrice:N2}.");
    }

    private static void UpdateProgress(int studentId, int courseId, int newProgress)
    {
        using var db = new AppDbContext();

        if (newProgress < 0 || newProgress > 100)
        {
            Console.WriteLine($"Rejected: progress {newProgress} is outside the 0-100 range.");
            return;
        }

        var enrollment = db.Enrollments.Find(studentId, courseId);   // composite key (StudentId, CourseId)
        if (enrollment is null)
        {
            Console.WriteLine($"Enrollment (student {studentId}, course {courseId}) not found.");
            return;
        }

        enrollment.Progress = newProgress;
        if (newProgress == 100)
            enrollment.Status = EnrollmentStatus.Completed;

        db.SaveChanges();
        Console.WriteLine($"Progress updated to {newProgress}% (status: {enrollment.Status}).");
    }

    private static void PublishCourse(int courseId)
    {
        using var db = new AppDbContext();

        var course = db.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Cannot publish: course {courseId} not found.");
            return;
        }
        if (course.IsPublished)
        {
            Console.WriteLine($"Course '{course.Title}' is already published.");
            return;
        }

        course.IsPublished = true;
        course.PublishedDate = DateTime.UtcNow;
        db.SaveChanges();

        Console.WriteLine($"Course '{course.Title}' published on {course.PublishedDate:yyyy-MM-dd}.");
    }

    // =====================================================================
    //  DELETE
    // =====================================================================
    private static void Unenroll(int studentId, int courseId)
    {
        using var db = new AppDbContext();

        var enrollment = db.Enrollments.Find(studentId, courseId);
        if (enrollment is null)
        {
            Console.WriteLine($"Cannot unenroll: enrollment (student {studentId}, course {courseId}) not found.");
            return;
        }

        db.Enrollments.Remove(enrollment);
        db.SaveChanges();
        Console.WriteLine($"Student {studentId} was unenrolled from course {courseId}.");
    }

    // Naive version: just tries to delete and lets the database complain.
    private static void DeleteCategoryUnsafe(int categoryId)
    {
        using var db = new AppDbContext();

        var category = db.Categories.Find(categoryId);
        if (category is null)
        {
            Console.WriteLine($"Category {categoryId} not found.");
            return;
        }

        try
        {
            db.Categories.Remove(category);
            db.SaveChanges();
            Console.WriteLine($"Category '{category.Name}' deleted.");
        }
        catch (DbUpdateException ex)
        {
            // The self-reference uses DeleteBehavior.Restrict, so SQL Server rejects the DELETE (FK conflict).
            Console.WriteLine($"Unsafe delete of '{category.Name}' failed: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Safe version: checks first, so no exception is needed.
    private static void DeleteCategorySafe(int categoryId)
    {
        using var db = new AppDbContext();

        var category = db.Categories.Find(categoryId);
        if (category is null)
        {
            Console.WriteLine($"Category {categoryId} not found.");
            return;
        }

        if (db.Categories.Any(c => c.ParentCategoryId == categoryId))
        {
            Console.WriteLine($"Safe delete refused: '{category.Name}' still has sub-categories.");
            return;
        }
        if (db.Courses.Any(c => c.CategoryId == categoryId))
        {
            Console.WriteLine($"Safe delete refused: '{category.Name}' still has courses.");
            return;
        }

        db.Categories.Remove(category);
        db.SaveChanges();
        Console.WriteLine($"Safe delete: category '{category.Name}' deleted.");
    }

    // Helper so the safe delete has a category it can actually remove.
    private static int? AddTempCategory()
    {
        using var db = new AppDbContext();
        var category = new Category { Name = "Temporary Category", Description = "Will be deleted", ParentCategoryId = null };
        db.Categories.Add(category);
        db.SaveChanges();
        Console.WriteLine($"Temporary category added (Id = {category.Id}).");
        return category.Id;
    }

    private static void DeleteCourseWithEnrollments(int courseId)
    {
        using var db = new AppDbContext();

        var course = db.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Cannot delete: course {courseId} not found.");
            return;
        }

        var before = db.Enrollments.Count(e => e.CourseId == courseId);
        Console.WriteLine($"Enrollments of '{course.Title}' before delete: {before}");

        db.Courses.Remove(course);
        db.SaveChanges();

        var after = db.Enrollments.Count(e => e.CourseId == courseId);
        Console.WriteLine($"Enrollments of the deleted course after delete: {after}");

        // WHY were the enrollments removed? The Course -> Enrollment relationship is required
        // (CourseId is a non-nullable part of the composite key) and is configured with
        // DeleteBehavior.Cascade. That creates ON DELETE CASCADE on the foreign key in SQL Server,
        // so when the course row is deleted the database deletes its enrollment rows automatically.
    }

    // =====================================================================
    //  BULK OPERATIONS
    // =====================================================================
    private static void BulkRaisePrice(CourseLevel level, decimal percent)
    {
        using var db = new AppDbContext();

        var factor = 1 + percent / 100m;

        // One UPDATE statement, nothing is loaded into memory.
        var affected = db.Courses
            .Where(c => c.IsPublished && c.Level == level)
            .ExecuteUpdate(s => s.SetProperty(c => c.Price, c => c.Price * factor));

        Console.WriteLine($"Raised the price of published {level} courses by {percent}%. Rows affected: {affected}");
    }

    private static void BulkDeleteDroppedEnrollments()
    {
        using var db = new AppDbContext();

        // One DELETE statement, nothing is loaded into memory.
        var affected = db.Enrollments
            .Where(e => e.Status == EnrollmentStatus.Dropped)
            .ExecuteDelete();

        Console.WriteLine($"Deleted all Dropped enrollments. Rows affected: {affected}");
    }

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"========== {title} ==========");
    }
}
