namespace PharmaGo.Domain.Entities
{
    public class ReservationDetail
    {
        public int Id { get; set; }
        public string DrugCode { get; set; }
        public int Quantity { get; set; }
        public bool RequiresPrescription { get; set; }
    }
}
