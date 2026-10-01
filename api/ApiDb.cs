using Microsoft.EntityFrameworkCore;

class ApiDb : DbContext
{
    public ApiDb(DbContextOptions<ApiDb> options) : base(options) {}

    public DbSet<Tire> Tires => Set<Tire>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Keep the schema in line with the validation rules in Program.cs.
        modelBuilder.Entity<Tire>(tire =>
        {
            tire.Property(t => t.Brand).HasMaxLength(50);
            tire.Property(t => t.Price).HasPrecision(10, 2);
        });
    }
}