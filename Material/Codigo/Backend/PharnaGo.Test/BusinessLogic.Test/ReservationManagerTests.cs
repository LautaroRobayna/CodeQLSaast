using Moq;
using PharmaGo.BusinessLogic;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;
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
            var drug = new Drug { Id = 1, Code = "D-001", Name = "Aspirina", Stock = 10 };

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
            var drug = new Drug { Id = 1, Code = "A-500", Name = "Amoxicilina 500mg", Stock = 5, Prescription = true };

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
            // Arrange
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

            // Act
            var result = _reservationManager.GetByPublicKey(publicKey);

            // Assert
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
    }
}
