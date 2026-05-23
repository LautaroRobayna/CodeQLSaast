using ExportationModel.ExportDomain;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.SearchCriterias;

namespace PharmaGo.IBusinessLogic
{
    public interface IReservationManager
    {
        Reservation Create(Reservation reservation);
        Reservation GetByPublicKey(string publicKey);
    }
}
