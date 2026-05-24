using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;
using PharmaGo.IBusinessLogic;
using PharmaGo.WebApi.Controllers;
using PharmaGo.WebApi.Models.In;
using PharmaGo.WebApi.Models.Out;

namespace PharmaGo.Test.WebApi.Test
{
    [TestClass]
    public class ReservationControllerTests
    {
        private Mock<IReservationManager> _reservationManagerMock;
        private ReservationController _reservationController;

        private ReservationModelRequest _reservationModel = null!;
        private Reservation _reservation = null!;
        private Reservation _reservationConfirmed = null!;

        [TestInitialize]
        public void Setup()
        {
            _reservationManagerMock = new Mock<IReservationManager>(MockBehavior.Strict);
            _reservationController = new ReservationController(_reservationManagerMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            _reservationModel = new ReservationModelRequest
            {
                Details = new List<ReservationModelRequest.ReservationDetailModelRequest>
                {
                    new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "DRUG-001", Quantity = 1 }
                },
                PharmacyId = 1,
                UserEmail = "user@test.com"
            };

            _reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "PUB-KEY-001",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { Id = 1, DrugCode = "DRUG-001", Quantity = 1 }
                },
                PharmacyId = 1,
                UserEmail = "user@test.com",
                ReservationDate = DateTime.Now,
                Status = ReservationStatus.Pending
            };

            _reservationConfirmed = new Reservation
            {
                Id = 2,
                Code = "RES-TEST-002",
                PublicKey = "CLAVE-CONFIRMADA-TEST",
                Status = ReservationStatus.Confirmed,
                UserEmail = "carlos@example.com",
                PharmacyId = 1,
                ReservationDate = new DateTime(2026, 5, 15),
                Details = [new ReservationDetail { Id = 1, DrugCode = "P-500", Quantity = 2, RequiresPrescription = false }]
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _reservationManagerMock.VerifyAll();
        }

        [TestMethod]
        public void PostReservationOk()
        {
            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Returns(_reservation);

            var result = _reservationController.Create(_reservationModel);
            var objectResult = result as ObjectResult;

            Assert.IsNotNull(objectResult);
            Assert.AreEqual(200, objectResult.StatusCode);
            var response = objectResult.Value as ReservationModelResponse;
            Assert.AreEqual(_reservation.Code, response.Code);
            Assert.AreEqual(_reservation.PublicKey, response.PublicKey);
            Assert.AreEqual(1, response.Details.Count);
            Assert.AreEqual("Pending", response.Status);
        }

        [TestMethod]
        public void GetByPublicKey_ReturnsPendingReservationWithPrescriptionWarning()
        {
            var publicKey = "CLAVE-PUBLICA-TEST";
            var reservationWithPrescription = new Reservation
            {
                Id = 1,
                Code = "RES-TEST-001",
                PublicKey = publicKey,
                Status = ReservationStatus.Pending,
                UserEmail = "carlos@example.com",
                PharmacyId = 1,
                ReservationDate = DateTime.Now,
                Details = [new ReservationDetail { Id = 1, DrugCode = "AMX-500", Quantity = 2, RequiresPrescription = true }]
            };

            _reservationManagerMock.Setup(m => m.GetByPublicKey(publicKey)).Returns(reservationWithPrescription);

            var result = _reservationController.GetByPublicKey(publicKey);
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            var response = okResult.Value as ReservationModelResponse;
            Assert.IsNotNull(response);
            Assert.AreEqual("Pending", response.Status);
            Assert.AreEqual(1, response.Details.Count);
            Assert.IsTrue(response.Details.First().RequiresPrescription);
        }

        [TestMethod]
        public void GetByPublicKey_ReturnsConfirmedReservationWithExpirationDate()
        {
            _reservationManagerMock.Setup(m => m.GetByPublicKey("CLAVE-CONFIRMADA-TEST")).Returns(_reservationConfirmed);

            var result = _reservationController.GetByPublicKey("CLAVE-CONFIRMADA-TEST");
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            var response = okResult.Value as ReservationModelResponse;
            Assert.IsNotNull(response);
            Assert.AreEqual("Confirmed", response.Status);
            Assert.AreEqual(new DateTime(2026, 5, 15).AddDays(30), response.ExpirationDate);
        }

        [TestMethod]
        public void GetByPublicKey_ReturnsNotFound_WhenPublicKeyDoesNotExist()
        {
            var publicKey = "CLAVE-INVALIDA-TEST";
            _reservationManagerMock.Setup(m => m.GetByPublicKey(publicKey)).Returns(default(Reservation));

            var result = _reservationController.GetByPublicKey(publicKey);
            var notFoundResult = result as NotFoundObjectResult;

            Assert.IsNotNull(notFoundResult);
            Assert.AreEqual(404, notFoundResult.StatusCode);
        }

        [TestMethod]
        public void Create_WithPrescription_ReturnsPrescriptionUploaded()
        {
            var modelWithPrescription = new ReservationModelRequest
            {
                PharmacyId = 1,
                UserEmail = "carlos@example.com",
                Details = [new ReservationModelRequest.ReservationDetailModelRequest { DrugCode = "A-500", Quantity = 1 }],
                PrescriptionBase64 = "base64content",
                PrescriptionFileName = "receta.pdf"
            };

            var reservationWithPrescription = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "PUB-KEY-001",
                Status = ReservationStatus.Pending,
                UserEmail = "carlos@example.com",
                PharmacyId = 1,
                ReservationDate = DateTime.Now,
                PrescriptionBase64 = "base64content",
                PrescriptionFileName = "receta.pdf",
                Details = [new ReservationDetail { Id = 1, DrugCode = "A-500", Quantity = 1 }]
            };

            _reservationManagerMock.Setup(x => x.Create(It.IsAny<Reservation>())).Returns(reservationWithPrescription);

            var result = _reservationController.Create(modelWithPrescription);
            var okResult = result as ObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            var response = okResult.Value as ReservationModelResponse;
            Assert.IsNotNull(response);
            Assert.IsTrue(response.PrescriptionUploaded);
        }

        [TestMethod]
        public void UploadPrescription_ReturnsOk_WhenPrescriptionUploaded()
        {
            var publicKey = "CLAVE-PUBLICA-TEST";
            var model = new UploadPrescriptionModelRequest
            {
                PrescriptionBase64 = "base64content",
                PrescriptionFileName = "receta.pdf"
            };

            _reservationManagerMock.Setup(m => m.UploadPrescription(publicKey, model.PrescriptionBase64, model.PrescriptionFileName))
                .Returns(true);

            var result = _reservationController.UploadPrescription(publicKey, model);
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }
    }
}
