using FormBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Infrastructure.Data;

public class FormBuilderDbContext : DbContext
{
    public FormBuilderDbContext(
        DbContextOptions<FormBuilderDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FormBuilderDbContext).Assembly);
    }


    public DbSet<Form> Forms => Set<Form>();

    public DbSet<FormVersion> FormVersions => Set<FormVersion>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<SubmissionAnswer> SubmissionAnswers => Set<SubmissionAnswer>();

}
