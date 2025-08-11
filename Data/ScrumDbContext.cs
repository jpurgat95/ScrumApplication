using Microsoft.EntityFrameworkCore;
using ScrumApplication.Models;

namespace ScrumApplication.Data;

public class ScrumDbContext : DbContext
{
    public ScrumDbContext(DbContextOptions<ScrumDbContext> options): base(options) { }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<ScrumEvent> Events { get; set; }
}
