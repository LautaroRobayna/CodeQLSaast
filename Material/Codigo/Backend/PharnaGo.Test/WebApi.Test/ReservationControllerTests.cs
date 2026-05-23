using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;
using PharmaGo.Exceptions;
using PharmaGo.IBusinessLogic;
using PharmaGo.WebApi.Controllers;
using PharmaGo.WebApi.Models.In;
using PharmaGo.WebApi.Models.Out;
using System.IO;
using System.Linq;
using System.Text;

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
        public void CreateReservation_ActiveReservationLimitExceeded_ReturnsBadRequest()
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
                new InvalidResourceException("No puedes tener más de 10 reservas activas simultáneamente"));

            var result = _reservationController.Create(reservationModel);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void CreateReservation_InvalidEmail_ReturnsBadRequest()
        {
            var reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>
                {
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-001", Quantity = 3 }
                },
                PharmacyId = 1,
                UserEmail = "email-invalido"
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Throws(
                new InvalidResourceException("El email ingresado no es válido"));

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

        [TestMethod]
        public void GetAllPending_Ok()
        {
            var pendingReservations = new List<Reservation>
            {
                new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Pending, PharmacyId = 1, UserEmail = "a@b.com", ReservationDate = DateTime.Now, Details = new List<ReservationDetail>() },
                new Reservation { Id = 2, Code = "RES-002", Status = ReservationStatus.Pending, PharmacyId = 1, UserEmail = "c@d.com", ReservationDate = DateTime.Now, Details = new List<ReservationDetail>() }
            };

            _reservationManagerMock.Setup(x => x.GetAllPending()).Returns(pendingReservations);

            var result = _reservationController.GetAllPending();
            var objectResult = result as ObjectResult;

            _reservationManagerMock.VerifyAll();
            Assert.AreEqual(200, objectResult.StatusCode);
            var response = objectResult.Value as List<ReservationModelResponse>;
            Assert.AreEqual(2, response.Count);
            Assert.AreEqual("RES-001", response[0].Code);
            Assert.AreEqual("RES-002", response[1].Code);
            Assert.IsFalse(response[0].HasRecipe);
            Assert.IsFalse(response[1].HasRecipe);
        }

        [TestMethod]
        public void GetAllPending_NoPending_ReturnsEmptyList()
        {
            _reservationManagerMock.Setup(x => x.GetAllPending()).Returns(new List<Reservation>());

            var result = _reservationController.GetAllPending();
            var objectResult = result as ObjectResult;

            _reservationManagerMock.VerifyAll();
            Assert.AreEqual(200, objectResult.StatusCode);
            var response = objectResult.Value as List<ReservationModelResponse>;
            Assert.AreEqual(0, response.Count);
        }

        [TestMethod]
        public void GetAllPending_WithOneRecipe()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var recipeDir = Path.Combine(tempDir, "1");
            Directory.CreateDirectory(recipeDir);
            var pdfPath = Path.Combine(recipeDir, "receta.pdf");
            File.WriteAllBytes(pdfPath, Encoding.UTF8.GetBytes("dummy pdf content"));

            try
            {
                var pending = new List<Reservation>
                {
                    new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Pending, PharmacyId = 1, UserEmail = "a@b.com", ReservationDate = DateTime.Now, Details = new List<ReservationDetail>() }
                };
                _reservationManagerMock.Setup(x => x.GetAllPending()).Returns(pending);

                var controller = new ReservationController(_reservationManagerMock.Object);
                controller.RecipeBasePath = tempDir;
                controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

                var result = controller.GetAllPending();
                var objectResult = result as ObjectResult;

                Assert.AreEqual(200, objectResult.StatusCode);
                var response = objectResult.Value as List<ReservationModelResponse>;
                Assert.AreEqual(1, response.Count);
                Assert.IsTrue(response[0].HasRecipe);
                Assert.AreEqual(1, response[0].RecipeFiles.Count);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void GetAllPending_WithMultipleRecipes()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var recipeDir = Path.Combine(tempDir, "1");
            Directory.CreateDirectory(recipeDir);
            File.WriteAllBytes(Path.Combine(recipeDir, "receta-amoxicilina.pdf"), Encoding.UTF8.GetBytes("pdf content 1"));
            File.WriteAllBytes(Path.Combine(recipeDir, "receta-ibuprofeno.pdf"), Encoding.UTF8.GetBytes("pdf content 2"));

            try
            {
                var pending = new List<Reservation>
                {
                    new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Pending, PharmacyId = 1, UserEmail = "a@b.com", ReservationDate = DateTime.Now, Details = new List<ReservationDetail>() }
                };
                _reservationManagerMock.Setup(x => x.GetAllPending()).Returns(pending);

                var controller = new ReservationController(_reservationManagerMock.Object);
                controller.RecipeBasePath = tempDir;
                controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

                var result = controller.GetAllPending();
                var objectResult = result as ObjectResult;

                Assert.AreEqual(200, objectResult.StatusCode);
                var response = objectResult.Value as List<ReservationModelResponse>;
                Assert.AreEqual(1, response.Count);
                Assert.IsTrue(response[0].HasRecipe);
                Assert.AreEqual(2, response[0].RecipeFiles.Count);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void PutConfirmReservation_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Confirmed,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>()
            };

            _reservationManagerMock.Setup(x => x.ConfirmReservation("RES-777")).Returns(reservation);

            var result = _reservationController.ConfirmReservation("RES-777");
            var objectResult = result as ObjectResult;

            _reservationManagerMock.VerifyAll();
            Assert.AreEqual(200, objectResult.StatusCode);
            var response = objectResult.Value as ReservationModelResponse;
            Assert.AreEqual("RES-777", response.Code);
            Assert.AreEqual("Confirmed", response.Status);
        }
    }
}

