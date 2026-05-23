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
                new Reservation { Id = 1, Code = "RES-001", Status = ReservationStatus.Pending, PharmacyId = 1, UserEmail = "a@b.com", Details = new List<ReservationDetail>() },
                new Reservation { Id = 2, Code = "RES-002", Status = ReservationStatus.Pending, PharmacyId = 1, UserEmail = "c@d.com", Details = new List<ReservationDetail>() }
            };

            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(pendingReservations);

            var result = _reservationManager.GetAllPending();

            Assert.AreEqual(2, result.Count());
            _reservationRepository.Verify(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()), Times.Once);
        }

        [TestMethod]
        public void GetAllPending_NoPending_ReturnsEmpty()
        {
            _reservationRepository.Setup(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()))
                .Returns(new List<Reservation>());

            var result = _reservationManager.GetAllPending();

            Assert.AreEqual(0, result.Count());
            _reservationRepository.Verify(r => r.GetAllByExpression(It.IsAny<Expression<Func<Reservation, bool>>>()), Times.Once);
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
    }
}
