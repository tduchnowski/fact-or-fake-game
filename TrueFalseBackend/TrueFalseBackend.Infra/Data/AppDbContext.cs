using Microsoft.EntityFrameworkCore;
using TrueFalseBackend.Models;

namespace TrueFalseBackend.Infra.Data;

public class AppDbContext : DbContext
{
    public DbSet<Question> Questions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
}
