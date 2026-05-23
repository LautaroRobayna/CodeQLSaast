using Microsoft.EntityFrameworkCore;
using PharmaGo.Domain.Entities;
using System.Linq.Expressions;

namespace PharmaGo.DataAccess.Repositories
{
    public class ReservationRepository : BaseRepository<Reservation>
    {
        private readonly PharmacyGoDbContext _context;

        public ReservationRepository(PharmacyGoDbContext context) : base(context)
        {
            _context = context;
        }

        public override IEnumerable<Reservation> GetAllByExpression(Expression<Func<Reservation, bool>> expression)
        {
            return _context.Set<Reservation>()
                .Include(r => r.Details)
                .Where(expression);
        }

        public override Reservation GetOneByExpression(Expression<Func<Reservation, bool>> expression)
        {
            return _context.Set<Reservation>()
                .Include(r => r.Details)
                .FirstOrDefault(expression);
        }
    }
}
