using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;
using PharmaGo.Domain.SearchCriterias;
using PharmaGo.Exceptions;
using PharmaGo.IBusinessLogic;
using PharmaGo.WebApi.Controllers;
using PharmaGo.WebApi.Models.In;
using PharmaGo.WebApi.Models.Out;
using System.Collections.Generic;

namespace PharmaGo.Test.WebApi.Test
{
    [TestClass]
    public class ReservationTest
    {
        private Mock<IReservationManager> _reservationManagerMock;
        private ReservationController _reservationController;

        [TestInitialize]
        public void Setup()
        {
            _reservationManagerMock = new Mock<IReservationManager>(MockBehavior.Strict);
            _reservationController = new ReservationController(_reservationManagerMock.Object);
            _reservationController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [TestMethod]
        public void PostReservationOk()
        {
            var reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>
                {
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-001", Quantity = 1 }
                },
                PharmacyId = 1,
                UserEmail = "user@test.com"
            };

            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "PUB-KEY-001",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { Id = 1, DrugCode = "DRUG-001", Quantity = 1 }
                },
                PharmacyId = reservationModel.PharmacyId,
                UserEmail = reservationModel.UserEmail,
                ReservationDate = DateTime.Now,
                Status = ReservationStatus.Pending
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Returns(reservation);

            var result = _reservationController.Create(reservationModel);
            var objectResult = result as ObjectResult;
            var statusCode = objectResult.StatusCode;

            _reservationManagerMock.VerifyAll();
            Assert.AreEqual(200, statusCode);
            var response = objectResult.Value as ReservationModelResponse;
            Assert.AreEqual(reservation.Code, response.Code);
            Assert.AreEqual(reservation.PublicKey, response.PublicKey);
            Assert.AreEqual(1, response.Details.Count);
            Assert.AreEqual("Pending", response.Status);
        }

        
        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void CreateReservation_EmptyDetails_ReturnsBadRequest()
        {
            var reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>(),
                PharmacyId = 1,
                UserEmail = "user@test.com"
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Throws(
                new InvalidResourceException("Invalid reservation details."));

            var result = _reservationController.Create(reservationModel);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void CreateReservation_TotalQuantityOverLimit_ReturnsBadRequest()
        {
            var reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>
                {
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-001", Quantity = 5 },
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-002", Quantity = 5 },
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-003", Quantity = 5 },
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-004", Quantity = 1 }
                },
                PharmacyId = 1,
                UserEmail = "user@test.com"
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Throws(
                new InvalidResourceException("La reserva no puede superar las 15 unidades totales"));

            var result = _reservationController.Create(reservationModel);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void CreateReservation_QuantityOverLimit_ReturnsBadRequest()
        {
            var reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>
                {
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-001", Quantity = 6 }
                },
                PharmacyId = 1,
                UserEmail = "user@test.com"
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Throws(
                new InvalidResourceException("No se permiten mas de 5 unidades del mismo medicamento"));

            var result = _reservationController.Create(reservationModel);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void CreateReservation_QuantityExceedsStock_ReturnsBadRequest()
        {
            var reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>
                {
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-001", Quantity = 4 }
                },
                PharmacyId = 1,
                UserEmail = "user@test.com"
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Throws(
                new InvalidResourceException("La cantidad solicitada supera el stock disponible"));

            var result = _reservationController.Create(reservationModel);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void CreateReservation_DrugFromDifferentPharmacy_ReturnsBadRequest()
        {
            var reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>
                {
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-001", Quantity = 3 }
                },
                PharmacyId = 1,
                UserEmail = "user@test.com"
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Throws(
                new InvalidResourceException("Una reserva solo puede contener medicamentos de una unica farmacia"));

            var result = _reservationController.Create(reservationModel);
        }
    }
}

