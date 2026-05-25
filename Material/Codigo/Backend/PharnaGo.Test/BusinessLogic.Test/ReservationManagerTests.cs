using Moq;
using PharmaGo.BusinessLogic;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;
using PharmaGo.Exceptions;
using PharmaGo.IDataAccess;
using System.Linq.Expressions;

namespace PharmaGo.Test.BusinessLogic.Test
{
    [TestClass]
    public class ReservationManagerTests
    {
        private Mock<IRepository<Reservation>> _reservationRepository;
        private Mock<IRepository<Drug>> _drugRepository;
        private Mock<IRepository<Pharmacy>> _pharmacyRepository;
        private ReservationManager _reservationManager;

        [TestInitialize]
        public void InitTest()
        {
            _reservationRepository = new Mock<IRepository<Reservation>>();
            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(new List<Reservation>());
            _drugRepository = new Mock<IRepository<Drug>>();
            _pharmacyRepository = new Mock<IRepository<Pharmacy>>();
            _reservationManager = new ReservationManager(
                _reservationRepository.Object,
                _drugRepository.Object,
                _pharmacyRepository.Object);
        }

        [TestMethod]
        public void CreateReservation_OK()
        {
            var pharmacy = new Pharmacy { Id = 1, Name = "Test Pharmacy" };
            var drug = new Drug { Id = 1, Code = "D-001", Name = "Aspirina", Stock = 10, Pharmacy = pharmacy };

            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "D-001", Quantity = 5 }
                }
            };

            _pharmacyRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Pharmacy, bool>>>())).Returns(pharmacy);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>())).Returns(drug);
            _reservationRepository.Setup(r => r.InsertOne(It.IsAny<Reservation>()));
            _drugRepository.Setup(r => r.UpdateOne(It.IsAny<Drug>()));

            var result = _reservationManager.Create(reservation);

            Assert.AreEqual(ReservationStatus.Pending, result.Status);
            Assert.IsNotNull(result.Code);
            Assert.IsNotNull(result.PublicKey);
            Assert.IsNotNull(result.PrivateKey);
            Assert.AreEqual(5, drug.Stock);
            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
            _drugRepository.Verify(r => r.UpdateOne(It.Is<Drug>(d => d.Stock == 5)), Times.Once);
            _drugRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void Create_WithPrescription_StoresPrescriptionData()
        {
            var pharmacy = new Pharmacy { Id = 1, Name = "Test Pharmacy" };
            var drug = new Drug { Id = 1, Code = "A-500", Name = "Amoxicilina 500mg", Stock = 5, Prescription = true, Pharmacy = pharmacy };

            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "carlos@example.com",
                PrescriptionBase64 = "base64pdfcontent",
                PrescriptionFileName = "receta.pdf",
                Details = [new ReservationDetail { DrugCode = "A-500", Quantity = 1 }]
            };

            _pharmacyRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Pharmacy, bool>>>())).Returns(pharmacy);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>())).Returns(drug);
            _drugRepository.Setup(r => r.UpdateOne(It.IsAny<Drug>()));
            _drugRepository.Setup(r => r.Save());
            _reservationRepository.Setup(r => r.InsertOne(It.IsAny<Reservation>()));
            _reservationRepository.Setup(r => r.Save());

            var result = _reservationManager.Create(reservation);

            Assert.IsNotNull(result.PrescriptionBase64);
            Assert.AreEqual("receta.pdf", result.PrescriptionFileName);
        }

        [TestMethod]
        public void GetByPublicKey_ReturnsReservationWithPrescriptionInfo()
        {
            var publicKey = "CLAVE-PUBLICA-TEST";
            var drug = new Drug
            {
                Id = 1,
                Code = "AMX-500",
                Name = "Amoxicilina 500mg",
                Prescription = true
            };
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-TEST-001",
                PublicKey = publicKey,
                Status = ReservationStatus.Pending,
                UserEmail = "carlos@example.com",
                PharmacyId = 1,
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { Id = 1, DrugCode = "AMX-500", Quantity = 2 }
                }
            };

            _reservationRepository
                .Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);
            _drugRepository
                .Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>()))
                .Returns(drug);

            var result = _reservationManager.GetByPublicKey(publicKey);

            Assert.IsNotNull(result);
            Assert.AreEqual(ReservationStatus.Pending, result.Status);
            Assert.AreEqual(publicKey, result.PublicKey);
            Assert.AreEqual(1, result.Details.Count);
            Assert.IsTrue(result.Details.First().RequiresPrescription);
        }

        [TestMethod]
        public void UploadPrescription_ReturnsTrue_WhenReservationExists()
        {
            var publicKey = "CLAVE-PUBLICA-TEST";
            var reservation = new Reservation
            {
                Id = 1,
                PublicKey = publicKey,
                Status = ReservationStatus.Pending,
                UserEmail = "carlos@example.com",
                PharmacyId = 1
            };

            _reservationRepository
                .Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);
            _reservationRepository.Setup(r => r.UpdateOne(It.IsAny<Reservation>()));
            _reservationRepository.Setup(r => r.Save());

            var result = _reservationManager.UploadPrescription(publicKey, "base64content", "receta.pdf");

            Assert.IsTrue(result);
            Assert.AreEqual("base64content", reservation.PrescriptionBase64);
            Assert.AreEqual("receta.pdf", reservation.PrescriptionFileName);
        }

        [TestMethod]
        public void CreateReservation_WithPrescriptionDrug_SetsRequiresPrescription()
        {
            var pharmacy = new Pharmacy { Id = 1, Name = "Test Pharmacy" };
            var drug = new Drug { Id = 1, Code = "AMO-500", Name = "Amoxicilina 500mg", Stock = 10, Prescription = true, Pharmacy = pharmacy };

            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "AMO-500", Quantity = 2 }
                }
            };

            _pharmacyRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Pharmacy, bool>>>())).Returns(pharmacy);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>())).Returns(drug);
            _reservationRepository.Setup(r => r.InsertOne(It.IsAny<Reservation>()));
            _drugRepository.Setup(r => r.UpdateOne(It.IsAny<Drug>()));

            var result = _reservationManager.Create(reservation);

            Assert.IsTrue(result.RequiresPrescription);
            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void CreateReservation_WithoutPrescriptionDrug_DoesNotSetRequiresPrescription()
        {
            var pharmacy = new Pharmacy { Id = 1, Name = "Test Pharmacy" };
            var drug = new Drug { Id = 1, Code = "PAR-500", Name = "Paracetamol 500mg", Stock = 10, Prescription = false, Pharmacy = pharmacy };

            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "PAR-500", Quantity = 3 }
                }
            };

            _pharmacyRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Pharmacy, bool>>>())).Returns(pharmacy);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>())).Returns(drug);
            _reservationRepository.Setup(r => r.InsertOne(It.IsAny<Reservation>()));
            _drugRepository.Setup(r => r.UpdateOne(It.IsAny<Drug>()));

            var result = _reservationManager.Create(reservation);

            Assert.IsFalse(result.RequiresPrescription);
            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void CreateReservation_NullReservation_ThrowsInvalidResourceException()
        {
            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(null));
            Assert.AreEqual("Invalid reservation details.", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_NullDetails_ThrowsInvalidResourceException()
        {
            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = null
            };

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("Invalid reservation details.", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_EmptyDetails_ThrowsInvalidResourceException()
        {
            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>()
            };

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("Invalid reservation details.", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_TotalQuantityOverLimit_ThrowsInvalidResourceException()
        {
            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "D-001", Quantity = 5 },
                    new ReservationDetail { DrugCode = "D-002", Quantity = 5 },
                    new ReservationDetail { DrugCode = "D-003", Quantity = 5 },
                    new ReservationDetail { DrugCode = "D-004", Quantity = 1 }
                }
            };

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("La reserva no puede superar las 15 unidades totales", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_WithQuantityOverLimit_ThrowsInvalidResourceException()
        {
            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "D-001", Quantity = 3 },
                    new ReservationDetail { DrugCode = "D-001", Quantity = 3 }
                }
            };

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("No se permiten mas de 5 unidades del mismo medicamento", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_QuantityExceedsStock_ThrowsInvalidResourceException()
        {
            var pharmacy = new Pharmacy { Id = 1, Name = "Test Pharmacy" };
            var drug = new Drug { Id = 1, Code = "D-001", Name = "Ibuprofeno 400mg", Stock = 3, Pharmacy = pharmacy };

            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "D-001", Quantity = 4 }
                }
            };

            _pharmacyRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Pharmacy, bool>>>())).Returns(pharmacy);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>())).Returns(drug);

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("La cantidad solicitada supera el stock disponible", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_ActiveReservationLimitExceeded_ThrowsInvalidResourceException()
        {
            var pharmacy = new Pharmacy { Id = 1, Name = "Test Pharmacy" };
            var drug = new Drug { Id = 1, Code = "D-001", Name = "Aspirina", Stock = 10, Pharmacy = pharmacy };

            var activeReservations = Enumerable.Range(1, 10).Select(i => new Reservation
            {
                Id = i,
                UserEmail = "user@test.com",
                Status = ReservationStatus.Pending
            }).ToList();

            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(activeReservations);
            _pharmacyRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Pharmacy, bool>>>())).Returns(pharmacy);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>())).Returns(drug);

            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "user@test.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "D-001", Quantity = 3 }
                }
            };

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("No puedes tener más de 10 reservas activas simultáneamente", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_InvalidEmail_ThrowsInvalidResourceException()
        {
            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "email-invalido",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "D-001", Quantity = 3 }
                }
            };

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("El email ingresado no es válido", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void CreateReservation_DrugFromDifferentPharmacy_ThrowsInvalidResourceException()
        {
            var pharmacy = new Pharmacy { Id = 1, Name = "Farmacia Central" };
            var drug = new Drug
            {
                Id = 1,
                Code = "D-001",
                Name = "Paracetamol",
                Stock = 10,
                Pharmacy = new Pharmacy { Id = 2, Name = "Farmacia Norte" }
            };

            var reservation = new Reservation
            {
                PharmacyId = 1,
                UserEmail = "test@user.com",
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "D-001", Quantity = 3 }
                }
            };

            _pharmacyRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Pharmacy, bool>>>())).Returns(pharmacy);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>())).Returns(drug);

            var ex = Assert.ThrowsException<InvalidResourceException>(() => _reservationManager.Create(reservation));
            Assert.AreEqual("Una reserva solo puede contener medicamentos de una unica farmacia", ex.Message);

            _reservationRepository.Verify(r => r.InsertOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
            _drugRepository.Verify(r => r.UpdateOne(It.IsAny<Drug>()), Times.Never);
            _drugRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void GetAllPending_Ok()
        {
            var pendingReservations = new List<Reservation>
            {
                new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Pending, ReservationDate = DateTime.Now.AddDays(-5), PharmacyId = 1, UserEmail = "a@b.com", Details = new List<ReservationDetail>() },
                new Reservation { Id = 2, Code = "RES-002", Status = ReservationStatus.Pending, ReservationDate = DateTime.Now.AddDays(-3), PharmacyId = 1, UserEmail = "c@d.com", Details = new List<ReservationDetail>() }
            };

            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(pendingReservations);

            var result = _reservationManager.GetAllPending();

            Assert.AreEqual(2, result.Count());
            _reservationRepository.Verify(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()), Times.Exactly(2));
        }

        [TestMethod]
        public void GetAllPending_NoPending_ReturnsEmpty()
        {
            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(new List<Reservation>());

            var result = _reservationManager.GetAllPending();

            Assert.AreEqual(0, result.Count());
            _reservationRepository.Verify(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()), Times.Exactly(2));
        }

        [TestMethod]
        public void GetAllPending_ExpiresOverdueBeforeReturning()
        {
            var overdue = new List<Reservation>
            {
                new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Pending, ReservationDate = DateTime.Now.AddDays(-31), PharmacyId = 1, UserEmail = "a@b.com", Details = new List<ReservationDetail>() }
            };
            var pending = new List<Reservation>
            {
                new Reservation { Id = 2, Code = "RES-002", Status = ReservationStatus.Pending, ReservationDate = DateTime.Now.AddDays(-5), PharmacyId = 1, UserEmail = "c@d.com", Details = new List<ReservationDetail>() }
            };

            _reservationRepository.SetupSequence(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(overdue)
                .Returns(pending);
            _reservationRepository.Setup(r => r.UpdateOne(It.IsAny<Reservation>()));

            var result = _reservationManager.GetAllPending();

            Assert.AreEqual(ReservationStatus.Expired, overdue[0].Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Expired)), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void ExpireOverdueReservations_ExpiresPendingOlderThan30Days()
        {
            var overdue = new List<Reservation>
            {
                new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Pending, ReservationDate = DateTime.Now.AddDays(-31), Details = new List<ReservationDetail>() },
                new Reservation { Id = 2, Code = "RES-002", Status = ReservationStatus.Pending, ReservationDate = DateTime.Now.AddDays(-35), Details = new List<ReservationDetail>() }
            };

            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(overdue);
            _reservationRepository.Setup(r => r.UpdateOne(It.IsAny<Reservation>()));

            _reservationManager.ExpireOverdueReservations();

            Assert.AreEqual(ReservationStatus.Expired, overdue[0].Status);
            Assert.AreEqual(ReservationStatus.Expired, overdue[1].Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Expired)), Times.Exactly(2));
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void ExpireOverdueReservations_NoOverdue_DoesNothing()
        {
            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(new List<Reservation>());

            _reservationManager.ExpireOverdueReservations();

            _reservationRepository.Verify(r => r.UpdateOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void ExpireOverdueReservations_ExpiresConfirmedOlderThan30Days()
        {
            var overdue = new List<Reservation>
            {
                new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Confirmed, ReservationDate = DateTime.Now.AddDays(-31), Details = new List<ReservationDetail>() }
            };

            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(overdue);
            _reservationRepository.Setup(r => r.UpdateOne(It.IsAny<Reservation>()));

            _reservationManager.ExpireOverdueReservations();

            Assert.AreEqual(ReservationStatus.Expired, overdue[0].Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Expired)), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void ExpireOverdueReservations_DoesNotExpireCancelled()
        {
            var cancelled = new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Cancelled, ReservationDate = DateTime.Now.AddDays(-31), Details = new List<ReservationDetail>() };

            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns((Expression<Func<Reservation, bool>> expr) =>
                    new List<Reservation> { cancelled }.AsQueryable().Where(expr).ToList());

            _reservationManager.ExpireOverdueReservations();

            Assert.AreEqual(ReservationStatus.Cancelled, cancelled.Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.IsAny<Reservation>()), Times.Never);
            _reservationRepository.Verify(r => r.Save(), Times.Never);
        }

        [TestMethod]
        public void ConfirmReservation_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Pending,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var result = _reservationManager.ConfirmReservation("RES-777");

            Assert.AreEqual(ReservationStatus.Confirmed, result.Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Confirmed)), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void RejectReservation_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Pending,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var result = _reservationManager.RejectReservation("RES-777");

            Assert.AreEqual(ReservationStatus.Cancelled, result.Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Cancelled)), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void RejectReservation_NotFound_Throws()
        {
            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns((Reservation)null);

            Assert.ThrowsException<ResourceNotFoundException>(() =>
                _reservationManager.RejectReservation("NONEXISTENT"));
        }

        [TestMethod]
        public void ConfirmReservation_RequiresPrescriptionNoRecipe_Throws()
        {
            var drug = new Drug { Id = 1, Code = "AMO-500", Name = "Amoxicilina 500mg", Prescription = true };
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Pending,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                HasRecipe = false,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "AMO-500", Quantity = 2 }
                }
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);
            _drugRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>()))
                .Returns(drug);

            var ex = Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.ConfirmReservation("RES-777"));
            Assert.AreEqual("La reserva requiere receta médica", ex.Message);
        }

        [TestMethod]
        public void ConfirmReservation_RequiresPrescriptionWithRecipe_Ok()
        {
            var drug = new Drug { Id = 1, Code = "AMO-500", Name = "Amoxicilina 500mg", Prescription = true };
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Pending,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                HasRecipe = true,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "AMO-500", Quantity = 2 }
                }
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var result = _reservationManager.ConfirmReservation("RES-777");

            Assert.AreEqual(ReservationStatus.Confirmed, result.Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Confirmed)), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void ConfirmReservation_RequiresPrescriptionNoRecipeNoDetails_Throws()
        {
            var prescriptionDrug = new Drug { Id = 1, Code = "AMO-500", Name = "Amoxicilina 500mg", Prescription = true };
            var noPrescriptionDrug = new Drug { Id = 2, Code = "PAR-500", Name = "Paracetamol 500mg", Prescription = false };
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Pending,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                HasRecipe = false,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "PAR-500", Quantity = 3 },
                    new ReservationDetail { DrugCode = "AMO-500", Quantity = 2 }
                }
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);
            _drugRepository.SetupSequence(r => r.GetOneByExpression(It.IsAny<Expression<Func<Drug, bool>>>()))
                .Returns(noPrescriptionDrug)
                .Returns(prescriptionDrug);

            var ex = Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.ConfirmReservation("RES-777"));
            Assert.AreEqual("La reserva requiere receta médica", ex.Message);
        }

        [TestMethod]
        public void ConfirmReservation_AlreadyConfirmed_Throws()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Confirmed,
                PharmacyId = 1,
                UserEmail = "a@b.com",
                Details = new List<ReservationDetail>()
            };
            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var ex = Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.ConfirmReservation("RES-777"));
            Assert.AreEqual("Solo se pueden confirmar reservas en estado pendiente", ex.Message);
        }

        [TestMethod]
        public void ConfirmReservation_Cancelled_Throws()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Cancelled,
                PharmacyId = 1,
                UserEmail = "a@b.com",
                Details = new List<ReservationDetail>()
            };
            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.ConfirmReservation("RES-777"));
        }

        [TestMethod]
        public void RejectReservation_AlreadyCancelled_Throws()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Cancelled,
                PharmacyId = 1,
                UserEmail = "a@b.com",
                Details = new List<ReservationDetail>()
            };
            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var ex = Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.RejectReservation("RES-777"));
            Assert.AreEqual("Solo se pueden rechazar reservas en estado pendiente", ex.Message);
        }

        [TestMethod]
        public void RejectReservation_Confirmed_Throws()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-777",
                Status = ReservationStatus.Confirmed,
                PharmacyId = 1,
                UserEmail = "a@b.com",
                Details = new List<ReservationDetail>()
            };
            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.RejectReservation("RES-777"));
        }

        [TestMethod]
        public void CancelReservation_AlreadyCancelled_Throws()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "CLAVE-CANCEL-TEST",
                Status = ReservationStatus.Cancelled,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var ex = Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.CancelReservation("CLAVE-CANCEL-TEST"));
            Assert.AreEqual("La reserva ya se encuentra cancelada", ex.Message);
        }

        [TestMethod]
        public void CancelReservation_Pending_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "CLAVE-CANCEL-PENDIENTE",
                Status = ReservationStatus.Pending,
                ReservationDate = DateTime.Now.AddDays(-20),
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var result = _reservationManager.CancelReservation("CLAVE-CANCEL-PENDIENTE");

            Assert.AreEqual(ReservationStatus.Cancelled, result.Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Cancelled)), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void CancelReservation_Confirmed_Ok()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "CLAVE-CANCEL-CONFIRMADA",
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTime.Now.AddDays(-20),
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var result = _reservationManager.CancelReservation("CLAVE-CANCEL-CONFIRMADA");

            Assert.AreEqual(ReservationStatus.Cancelled, result.Status);
            _reservationRepository.Verify(r => r.UpdateOne(It.Is<Reservation>(res => res.Status == ReservationStatus.Cancelled)), Times.Once);
            _reservationRepository.Verify(r => r.Save(), Times.Once);
        }

        [TestMethod]
        public void CancelReservation_TooCloseToExpiration_Throws()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "CLAVE-CANCEL-CERCANA",
                Status = ReservationStatus.Pending,
                ReservationDate = DateTime.Now.AddDays(-27),
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var ex = Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.CancelReservation("CLAVE-CANCEL-CERCANA"));
            Assert.AreEqual("No se puede cancelar una reserva a menos de 5 días de su expiración", ex.Message);
        }

        [TestMethod]
        public void CancelReservation_Expired_Throws()
        {
            var reservation = new Reservation
            {
                Id = 1,
                Code = "RES-001",
                PublicKey = "CLAVE-CANCEL-EXPIRADA",
                Status = ReservationStatus.Expired,
                PharmacyId = 1,
                UserEmail = "cliente@example.com",
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(reservation);

            var ex = Assert.ThrowsException<InvalidResourceException>(() =>
                _reservationManager.CancelReservation("CLAVE-CANCEL-EXPIRADA"));
            Assert.AreEqual("La reserva se encuentra expirada", ex.Message);
        }

        [TestMethod]
        public void CancelReservation_NotFound_Throws()
        {
            _reservationRepository.Setup(r => r.GetOneByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns((Reservation)null);

            Assert.ThrowsException<ResourceNotFoundException>(() =>
                _reservationManager.CancelReservation("NONEXISTENT"));
        }
    }
}
