using Microsoft.EntityFrameworkCore;
using PharmaGo.DataAccess;
using PharmaGo.DataAccess.Repositories;
using PharmaGo.Domain.Entities;
using PharmaGo.Domain.Enums;

namespace PharmaGo.Test.DataAccess.Test
{
    [TestClass]
    public class ReservationRepositoryTests
    {
        private PharmacyGoDbContext context;
        private ReservationRepository _reservationRepository;

        [TestCleanup]
        public void CleanUp()
        {
            context.Database.EnsureDeleted();
        }

        private void CreateDataBase(string name)
        {
            var options = new DbContextOptionsBuilder<PharmacyGoDbContext>()
                .UseInMemoryDatabase(databaseName: name)
                .Options;
            context = new PharmacyGoDbContext(options);
            _reservationRepository = new ReservationRepository(context);
        }

        [TestMethod]
        public void CreateReservationOk()
        {
            CreateDataBase("createReservationDb");
            var reservation = new Reservation
            {
                Code = "RES-001",
                PublicKey = "PUB-KEY-001",
                PrivateKey = "PRIV-KEY-001",
                UserEmail = "test@user.com",
                PharmacyId = 1,
                Status = ReservationStatus.Pending,
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "DRUG-001", Quantity = 2 }
                }
            };

            _reservationRepository.InsertOne(reservation);
            _reservationRepository.Save();

            var savedReservation = _reservationRepository.GetOneByExpression(r => r.Code == "RES-001");
            Assert.AreEqual("test@user.com", savedReservation.UserEmail);
            Assert.AreEqual(1, savedReservation.Details.Count);
        }
    }
}