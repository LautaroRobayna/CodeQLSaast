using Microsoft.AspNetCore.Mvc;
using PharmaGo.Domain.Entities;
using PharmaGo.IBusinessLogic;
using PharmaGo.WebApi.Enums;
using PharmaGo.WebApi.Filters;
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

        [HttpPost]
        public IActionResult Create([FromBody] ReservationModelRequest reservationModel)
        {
            var reservation = new Reservation
            {
                PharmacyId = reservationModel.PharmacyId,
                UserEmail = reservationModel.UserEmail,
                Details = reservationModel.Details.Select(d => new ReservationDetail
                {
                    DrugCode = d.DrugCode,
                    Quantity = d.Quantity
                }).ToList()
            };
            var createdReservation = _reservationManager.Create(reservation);
            return Ok(new ReservationModelResponse(createdReservation));
        }

        [HttpPut("{code}/confirm")]
        [AuthorizationFilter(new[] { nameof(RoleType.Employee) })]
        public IActionResult ConfirmReservation(string code)
        {
            var confirmed = _reservationManager.ConfirmReservation(code);
            return Ok(new ReservationModelResponse(confirmed));
        }
    }
}
