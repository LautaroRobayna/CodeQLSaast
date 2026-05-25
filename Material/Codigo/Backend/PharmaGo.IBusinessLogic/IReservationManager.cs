using PharmaGo.Domain.Entities;

namespace PharmaGo.IBusinessLogic
{
    public interface IReservationManager
    {
        Reservation Create(Reservation reservation);
        Reservation? GetByPublicKey(string publicKey);
        bool UploadPrescription(string publicKey, string prescriptionBase64, string prescriptionFileName);
        Reservation ConfirmReservation(string code);
        Reservation RejectReservation(string code);
        Reservation CancelReservation(string publicKey);
        void ExpireOverdueReservations();
        IEnumerable<Reservation> GetAllPending();
    }
}
