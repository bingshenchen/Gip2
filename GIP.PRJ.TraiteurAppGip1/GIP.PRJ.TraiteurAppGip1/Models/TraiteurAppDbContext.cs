using Microsoft.EntityFrameworkCore;

namespace GIP.PRJ.TraiteurAppGip1.Models
{
    public class TraiteurAppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }

        public TraiteurAppDbContext(DbContextOptions options) : base(options)
        {

        }
    }
}
