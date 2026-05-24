using PharmaGo.Domain.Entities;

namespace PharmaGo.IBusinessLogic
{
    public interface IReservationManager
    {
        Reservation Create(Reservation reservation);
        Reservation ConfirmReservation(string code);
        Reservation RejectReservation(string code);
        IEnumerable<Reservation> GetAllPending();
    }
}
