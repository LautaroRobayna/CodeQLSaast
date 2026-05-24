using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;
using PharmaGo.IBusinessLogic;
using PharmaGo.IDataAccess;

namespace PharmaGo.BusinessLogic
{
    public class ReservationManager : IReservationManager
    {
        private readonly IRepository<Reservation> _reservationRepository;
        private readonly IRepository<Drug> _drugRepository;
        private readonly IRepository<Pharmacy> _pharmacyRepository;

        public ReservationManager(IRepository<Reservation> reservationRepository,
                                  IRepository<Drug> drugRepository,
                                  IRepository<Pharmacy> pharmacyRepository)
        {
            _reservationRepository = reservationRepository;
            _drugRepository = drugRepository;
            _pharmacyRepository = pharmacyRepository;
        }

        public Reservation Create(Reservation reservation)
        {
            var pharmacy = _pharmacyRepository.GetOneByExpression(p => p.Id == reservation.PharmacyId);

            foreach (var detail in reservation.Details)
            {
                var drug = _drugRepository.GetOneByExpression(d => d.Code == detail.DrugCode);
                if (drug != null)
                {
                    drug.Stock -= detail.Quantity;
                    _drugRepository.UpdateOne(drug);
                }
            }
            _drugRepository.Save();

            reservation.Status = ReservationStatus.Pending;
            reservation.Code = Guid.NewGuid().ToString();

            using (var rsa = System.Security.Cryptography.RSA.Create())
            {
                reservation.PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                reservation.PrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            }

            reservation.ReservationDate = DateTime.Now;

            _reservationRepository.InsertOne(reservation);
            _reservationRepository.Save();

            return reservation;
        }

        public bool UploadPrescription(string publicKey, string prescriptionBase64, string prescriptionFileName)
        {
            var reservation = _reservationRepository.GetOneByExpression(r => r.PublicKey == publicKey);
            if (reservation == null)
                return false;

            reservation.PrescriptionBase64 = prescriptionBase64;
            reservation.PrescriptionFileName = prescriptionFileName;
            _reservationRepository.UpdateOne(reservation);
            _reservationRepository.Save();
            return true;
        }

        public Reservation? GetByPublicKey(string publicKey)
        {
            var reservation = _reservationRepository.GetOneByExpression(r => r.PublicKey == publicKey);

            if (reservation == null)
                return null;

            foreach (var detail in reservation.Details)
            {
                var drug = _drugRepository.GetOneByExpression(d => d.Code == detail.DrugCode);
                if (drug != null)
                {
                    detail.RequiresPrescription = drug.Prescription;
                }
            }

            return reservation;
        }
    }
}
