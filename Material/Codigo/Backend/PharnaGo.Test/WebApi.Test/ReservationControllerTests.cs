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

        [TestMethod]
        public void PutRejectReservation_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-999",
                Status = ReservationStatus.Cancelled,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>()
            };

            _reservationManagerMock.Setup(x => x.RejectReservation("RES-999")).Returns(reservation);

            var result = _reservationController.RejectReservation("RES-999");
            var objectResult = result as ObjectResult;

            _reservationManagerMock.VerifyAll();
            Assert.AreEqual(200, objectResult.StatusCode);
            var response = objectResult.Value as ReservationModelResponse;
            Assert.AreEqual("RES-999", response.Code);
            Assert.AreEqual("Cancelled", response.Status);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void PutConfirmReservation_RequiresRecipeNoUpload_Throws()
        {
            _reservationManagerMock.Setup(x => x.ConfirmReservation("RES-NO-UPLOAD"))
                .Throws(new InvalidResourceException("La reserva requiere receta médica"));

            _reservationController.ConfirmReservation("RES-NO-UPLOAD");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void PutConfirmReservation_AlreadyConfirmed_Throws()
        {
            _reservationManagerMock.Setup(x => x.ConfirmReservation("RES-777"))
                .Throws(new InvalidResourceException("Solo se pueden confirmar reservas en estado pendiente"));

            _reservationController.ConfirmReservation("RES-777");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidResourceException))]
        public void PutRejectReservation_AlreadyCancelled_Throws()
        {
            _reservationManagerMock.Setup(x => x.RejectReservation("RES-999"))
                .Throws(new InvalidResourceException("Solo se pueden rechazar reservas en estado pendiente"));

            _reservationController.RejectReservation("RES-999");
        }

        [TestMethod]
        public void PutCancelReservation_Pending_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "CLAVE-CANCEL-TEST",
                Status = ReservationStatus.Cancelled,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>()
            };

            _reservationManagerMock.Setup(x => x.CancelReservation("CLAVE-CANCEL-TEST")).Returns(reservation);

            var result = _reservationController.CancelReservation("CLAVE-CANCEL-TEST");
            var objectResult = result as ObjectResult;

            _reservationManagerMock.VerifyAll();
            Assert.AreEqual(200, objectResult.StatusCode);
            var response = objectResult.Value as ReservationModelResponse;
            Assert.AreEqual("RES-001", response.Code);
            Assert.AreEqual("Cancelled", response.Status);
        }

        [TestMethod]
        public void PutCancelReservation_Confirmed_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "CLAVE-CANCEL-CONFIRMADA",
                Status = ReservationStatus.Cancelled,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>()
            };

            _reservationManagerMock.Setup(x => x.CancelReservation("CLAVE-CANCEL-CONFIRMADA")).Returns(reservation);

            var result = _reservationController.CancelReservation("CLAVE-CANCEL-CONFIRMADA");
            var objectResult = result as ObjectResult;

            _reservationManagerMock.VerifyAll();
            Assert.AreEqual(200, objectResult.StatusCode);
            var response = objectResult.Value as ReservationModelResponse;
            Assert.AreEqual("RES-001", response.Code);
            Assert.AreEqual("Cancelled", response.Status);
        }

        [TestMethod]
        [ExpectedException(typeof(ResourceNotFoundException))]
        public void PutCancelReservation_NotFound_Throws()
        {
            _reservationManagerMock.Setup(x => x.CancelReservation("NONEXISTENT"))
                .Throws(new ResourceNotFoundException("Reservation not found"));

            _reservationController.CancelReservation("NONEXISTENT");
        }
    }
}
