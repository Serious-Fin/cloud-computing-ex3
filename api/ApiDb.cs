using Microsoft.EntityFrameworkCore;

class ApiDb : DbContext
{
    public ApiDb(DbContextOptions<ApiDb> options) : base(options) {}

    public DbSet<Tire> Tires => Set<Tire>();
}