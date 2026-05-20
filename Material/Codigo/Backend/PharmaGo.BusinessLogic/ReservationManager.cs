using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;
using PharmaGo.Exceptions;
using PharmaGo.IBusinessLogic;
using PharmaGo.IDataAccess;

namespace PharmaGo.BusinessLogic
{
    public class ReservationManager : IReservationManager
    {
        private readonly IRepository<Reservation> _reservationRepository;
        private readonly IRepository<Drug> _drugRepository;
        private readonly IRepository<Pharmacy> _pharmacyRepository;
        private const int MaxUnitsPerDrug = 5;

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
            if (reservation?.Details == null || reservation.Details.Count == 0)
            {
                throw new InvalidResourceException("Invalid reservation details.");
            }

            var groupedDetails = reservation.Details
                .GroupBy(d => d.DrugCode)
                .Select(g => new ReservationDetail
                {
                    DrugCode = g.Key,
                    Quantity = g.Sum(d => d.Quantity)
                })
                .ToList();

            var exceedsLimit = groupedDetails.Any(d => d.Quantity > MaxUnitsPerDrug);

            if (exceedsLimit)
            {
                throw new InvalidResourceException("No se permiten mas de 5 unidades del mismo medicamento");
            }

            var totalUnits = groupedDetails.Sum(d => d.Quantity);
            if (totalUnits > 15)
            {
                throw new InvalidResourceException("La reserva no puede superar las 15 unidades totales");
            }

            reservation.Details = groupedDetails;

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
    }
}
