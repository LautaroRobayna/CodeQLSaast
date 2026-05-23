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

        [TestMethod]
        public void GetAllByExpression_ReturnsReservationsWithDetails()
        {
            CreateDataBase("getAllByExpressionDb");
            var res1 = new Reservation
            {
                Code = "RES-001",
                PublicKey = "PK-001",
                PrivateKey = "PRIV-001",
                UserEmail = "a@a.com",
                PharmacyId = 1,
                Status = ReservationStatus.Pending,
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "DRUG-A", Quantity = 2 },
                    new ReservationDetail { DrugCode = "DRUG-B", Quantity = 1 }
                }
            };
            var res2 = new Reservation
            {
                Code = "RES-002",
                PublicKey = "PK-002",
                PrivateKey = "PRIV-002",
                UserEmail = "b@b.com",
                PharmacyId = 1,
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "DRUG-C", Quantity = 5 }
                }
            };

            _reservationRepository.InsertOne(res1);
            _reservationRepository.InsertOne(res2);
            _reservationRepository.Save();

            var pending = _reservationRepository.GetAllByExpression(r => r.Status == ReservationStatus.Pending);
            var pendingList = pending.ToList();

            Assert.AreEqual(1, pendingList.Count);
            Assert.AreEqual("RES-001", pendingList[0].Code);
            Assert.AreEqual(2, pendingList[0].Details.Count);
            Assert.AreEqual("DRUG-A", pendingList[0].Details.ElementAt(0).DrugCode);
            Assert.AreEqual("DRUG-B", pendingList[0].Details.ElementAt(1).DrugCode);
        }

        [TestMethod]
        public void GetAllByExpression_NoMatch_ReturnsEmpty()
        {
            CreateDataBase("getAllByExpressionEmptyDb");
            var res = new Reservation
            {
                Code = "RES-001",
                PublicKey = "PK-001",
                PrivateKey = "PRIV-001",
                UserEmail = "a@a.com",
                PharmacyId = 1,
                Status = ReservationStatus.Pending,
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>()
            };

            _reservationRepository.InsertOne(res);
            _reservationRepository.Save();

            var result = _reservationRepository.GetAllByExpression(r => r.Status == ReservationStatus.Expired);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetOneByExpression_ReturnsReservationWithDetails()
        {
            CreateDataBase("getOneByExpressionDb");
            var reservation = new Reservation
            {
                Code = "RES-001",
                PublicKey = "PK-001",
                PrivateKey = "PRIV-001",
                UserEmail = "a@a.com",
                PharmacyId = 1,
                Status = ReservationStatus.Pending,
                ReservationDate = DateTime.Now,
                Details = new List<ReservationDetail>
                {
                    new ReservationDetail { DrugCode = "DRUG-A", Quantity = 3 },
                    new ReservationDetail { DrugCode = "DRUG-B", Quantity = 7 }
                }
            };

            _reservationRepository.InsertOne(reservation);
            _reservationRepository.Save();

            var saved = _reservationRepository.GetOneByExpression(r => r.Code == "RES-001");

            Assert.IsNotNull(saved);
            Assert.AreEqual("a@a.com", saved.UserEmail);
            Assert.AreEqual(2, saved.Details.Count);
            Assert.AreEqual("DRUG-A", saved.Details.ElementAt(0).DrugCode);
            Assert.AreEqual(3, saved.Details.ElementAt(0).Quantity);
            Assert.AreEqual("DRUG-B", saved.Details.ElementAt(1).DrugCode);
            Assert.AreEqual(7, saved.Details.ElementAt(1).Quantity);
        }

        [TestMethod]
        public void GetOneByExpression_NoMatch_ReturnsNull()
        {
            CreateDataBase("getOneByExpressionNullDb");
            var result = _reservationRepository.GetOneByExpression(r => r.Code == "NONEXISTENT");
            Assert.IsNull(result);
        }
    }
}