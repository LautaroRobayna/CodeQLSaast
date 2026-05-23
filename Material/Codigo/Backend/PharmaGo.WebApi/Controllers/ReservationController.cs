using Microsoft.AspNetCore.Mvc;
using PharmaGo.Domain.Entities;
using PharmaGo.IBusinessLogic;
using PharmaGo.WebApi.Enums;
using PharmaGo.WebApi.Filters;
using PharmaGo.WebApi.Models.In;
using PharmaGo.WebApi.Models.Out;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PharmaGo.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationManager _reservationManager;
        public string RecipeBasePath { get; set; }

        public ReservationController(IReservationManager reservationManager)
        {
            _reservationManager = reservationManager;
            RecipeBasePath = Path.Combine(Directory.GetCurrentDirectory(), "ReservationRecipes");
        }

        [HttpGet("pending")]
        [AuthorizationFilter(new[] { nameof(RoleType.Employee) })]
        public IActionResult GetAllPending()
        {
            var reservations = _reservationManager.GetAllPending();
            var response = reservations.Select(r =>
            {
                var model = new ReservationModelResponse(r);
                var recipeDir = Path.Combine(RecipeBasePath, r.Id.ToString());
                if (Directory.Exists(recipeDir))
                {
                    var recipeFiles = Directory.GetFiles(recipeDir, "*.pdf").ToList();
                    if (recipeFiles.Any())
                    {
                        model.HasRecipe = true;
                        model.RecipeFiles = recipeFiles.Select(f => Convert.ToBase64String(System.IO.File.ReadAllBytes(f))).ToList();
                    }
                }
                return model;
            }).ToList();
            return Ok(response);
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

        [HttpPut("{code}/reject")]
        [AuthorizationFilter(new[] { nameof(RoleType.Employee) })]
        public IActionResult RejectReservation(string code)
        {
            var rejected = _reservationManager.RejectReservation(code);
            return Ok(new ReservationModelResponse(rejected));
        }
    }
}
