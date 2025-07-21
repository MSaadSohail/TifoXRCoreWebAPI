using Microsoft.EntityFrameworkCore;
using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> User { get; set; }
        public DbSet<UserSessionManagement> UserSession { get; set; }
    }
}
