using System.Collections.Generic;

namespace PharmaGo.WebApi.Models.In
{
    public class ReservationModelRequest
    {
        public ICollection<ReservationDetailModelRequest> Details { get; set; }
        public int PharmacyId { get; set; }
        public string UserEmail { get; set; }

        public class ReservationDetailModelRequest
        {
            public string DrugCode { get; set; }
            public int Quantity { get; set; }
        }
    }
}
