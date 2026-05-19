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
    }
}

