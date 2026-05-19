using PharmaGo.Domain.Entities;

namespace PharmaGo.DataAccess.Repositories
{
    public class ReservationRepository : BaseRepository<Reservation>
    {
        private readonly PharmacyGoDbContext _context;

        public ReservationRepository(PharmacyGoDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
