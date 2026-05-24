using ExportationModel.ExportDomain;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.SearchCriterias;

namespace PharmaGo.IBusinessLogic
{
    public interface IReservationManager
    {
        Reservation Create(Reservation reservation);
        Reservation? GetByPublicKey(string publicKey);
        bool UploadPrescription(string publicKey, string prescriptionBase64, string prescriptionFileName);
    }
}
