using PharmaGo.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace PharmaGo.WebApi.Models.Out
{
    public class ReservationModelResponse
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string PublicKey { get; set; }
        public ICollection<ReservationDetailModelResponse> Details { get; set; }
        public int PharmacyId { get; set; }
        public string UserEmail { get; set; }
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool PrescriptionUploaded { get; set; }

        public ReservationModelResponse(Reservation reservation)
        {
            Id = reservation.Id;
            Code = reservation.Code;
            PublicKey = reservation.PublicKey;
            Details = reservation.Details.Select(d => new ReservationDetailModelResponse
            {
                Id = d.Id,
                DrugCode = d.DrugCode,
                Quantity = d.Quantity,
                RequiresPrescription = d.RequiresPrescription
            }).ToList();
            PharmacyId = reservation.PharmacyId;
            UserEmail = reservation.UserEmail;
            ReservationDate = reservation.ReservationDate;
            Status = reservation.Status.ToString();
            ExpirationDate = reservation.ReservationDate.AddDays(30);
            PrescriptionUploaded = !string.IsNullOrEmpty(reservation.PrescriptionBase64);
        }

        public class ReservationDetailModelResponse
        {
            public int Id { get; set; }
            public string DrugCode { get; set; }
            public int Quantity { get; set; }
            public bool RequiresPrescription { get; set; }
        }
    }
}
