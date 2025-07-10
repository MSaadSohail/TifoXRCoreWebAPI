using Microsoft.EntityFrameworkCore;
using TifoXRWebApi.Models;

namespace TifoXRWebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> User { get; set; }
        public DbSet<UserSessionManagement> UserSession { get; set; }
    }
}
