using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace CyberClub.Models
{
    // Database First: the model and mapping are loaded from CyberClubModel.edmx resources.
    public partial class CyberClubEntities : DbContext
    {
        static CyberClubEntities()
        {
            Database.SetInitializer<CyberClubEntities>(null);
        }

        public CyberClubEntities() : base("name=CyberClubEntities")
        {
            Configure();
        }

        public CyberClubEntities(string entityConnectionString) : base(entityConnectionString)
        {
            Configure();
        }

        void Configure()
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder builder)
        {
            throw new UnintentionalCodeFirstException();
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Client> Clients { get; set; }
        public virtual DbSet<Club> Clubs { get; set; }
        public virtual DbSet<Zone> Zones { get; set; }
        public virtual DbSet<Computer> Computers { get; set; }
        public virtual DbSet<Tariff> Tariffs { get; set; }
        public virtual DbSet<Booking> Bookings { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }
    }
}
