using PharmaGo.Domain.Enums;

namespace PharmaGo.Domain.Entities
{
    public class Reservation
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public ICollection<ReservationDetail> Details { get; set; }
        public int PharmacyId { get; set; }
        public string UserEmail { get; set; }
        public DateTime ReservationDate { get; set; }
        public ReservationStatus Status { get; set; }
        public string? PrescriptionBase64 { get; set; }
        public string? PrescriptionFileName { get; set; }
    }
}
