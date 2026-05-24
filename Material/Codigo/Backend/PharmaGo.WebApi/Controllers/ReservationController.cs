using Microsoft.AspNetCore.Mvc;
using PharmaGo.Domain.Entities;
using PharmaGo.IBusinessLogic;
using PharmaGo.WebApi.Models.In;
using PharmaGo.WebApi.Models.Out;

namespace PharmaGo.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationManager _reservationManager;

        public ReservationController(IReservationManager reservationManager)
        {
            _reservationManager = reservationManager;
        }

        [HttpGet]
        public IActionResult GetByPublicKey([FromQuery] string publicKey)
        {
            var reservation = _reservationManager.GetByPublicKey(publicKey);
            if (reservation == null)
                return NotFound(new { message = "Reserva no encontrada." });
            return Ok(new ReservationModelResponse(reservation));
        }

        [HttpPatch]
        public IActionResult UploadPrescription([FromQuery] string publicKey, [FromBody] UploadPrescriptionModelRequest model)
        {
            var success = _reservationManager.UploadPrescription(publicKey, model.PrescriptionBase64, model.PrescriptionFileName);
            if (!success)
                return NotFound(new { message = "Reserva no encontrada." });
            return Ok(new { prescriptionUploaded = true });
        }

        [HttpPost]
        public IActionResult Create([FromBody] ReservationModelRequest reservationModel)
        {
            var reservation = new Reservation
            {
                PharmacyId = reservationModel.PharmacyId,
                UserEmail = reservationModel.UserEmail,
                PrescriptionBase64 = reservationModel.PrescriptionBase64,
                PrescriptionFileName = reservationModel.PrescriptionFileName,
                Details = reservationModel.Details.Select(d => new ReservationDetail
                {
                    DrugCode = d.DrugCode,
                    Quantity = d.Quantity
                }).ToList()
            };
            var createdReservation = _reservationManager.Create(reservation);
            return Ok(new ReservationModelResponse(createdReservation));
        }
    }
}
