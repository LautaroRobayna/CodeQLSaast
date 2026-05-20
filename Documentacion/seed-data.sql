SET NOCOUNT ON;

-- Roles
DECLARE @RoleAdminId INT;
DECLARE @RoleEmployeeId INT;

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
    INSERT INTO Roles (Name) VALUES ('Admin');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Employee')
    INSERT INTO Roles (Name) VALUES ('Employee');

SELECT @RoleAdminId = Id FROM Roles WHERE Name = 'Admin';
SELECT @RoleEmployeeId = Id FROM Roles WHERE Name = 'Employee';

-- Pharmacies
DECLARE @PharmacyCentralId INT;
DECLARE @PharmacyNorteId INT;

IF NOT EXISTS (SELECT 1 FROM Pharmacys WHERE Name = 'PharmaGo Central')
    INSERT INTO Pharmacys (Name, Address) VALUES ('PharmaGo Central', 'Av. Central 123');

IF NOT EXISTS (SELECT 1 FROM Pharmacys WHERE Name = 'PharmaGo Norte')
    INSERT INTO Pharmacys (Name, Address) VALUES ('PharmaGo Norte', 'Calle Norte 456');

SELECT @PharmacyCentralId = Id FROM Pharmacys WHERE Name = 'PharmaGo Central';
SELECT @PharmacyNorteId = Id FROM Pharmacys WHERE Name = 'PharmaGo Norte';

-- Users
DECLARE @AdminUserId INT;
DECLARE @EmployeeUserId INT;

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'admin')
    INSERT INTO Users (UserName, Email, Password, Address, RegistrationDate, RoleId, PharmacyId)
    VALUES ('admin', 'admin@pharmago.test', 'Admin123', 'Av. Central 123', GETDATE(), @RoleAdminId, @PharmacyCentralId);

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = 'employee1')
    INSERT INTO Users (UserName, Email, Password, Address, RegistrationDate, RoleId, PharmacyId)
    VALUES ('employee1', 'employee1@pharmago.test', 'Emp123', 'Calle Norte 456', GETDATE(), @RoleEmployeeId, @PharmacyNorteId);

SELECT @AdminUserId = Id FROM Users WHERE UserName = 'admin';
SELECT @EmployeeUserId = Id FROM Users WHERE UserName = 'employee1';

-- UnitMeasures
DECLARE @UnitTabletId INT;
DECLARE @UnitMlId INT;

IF NOT EXISTS (SELECT 1 FROM UnitMeasures WHERE Name = 'Tablet')
    INSERT INTO UnitMeasures (Name, Deleted) VALUES ('Tablet', 0);

IF NOT EXISTS (SELECT 1 FROM UnitMeasures WHERE Name = 'Milliliter')
    INSERT INTO UnitMeasures (Name, Deleted) VALUES ('Milliliter', 0);

SELECT @UnitTabletId = Id FROM UnitMeasures WHERE Name = 'Tablet';
SELECT @UnitMlId = Id FROM UnitMeasures WHERE Name = 'Milliliter';

-- Presentations
DECLARE @PresentationBoxId INT;
DECLARE @PresentationBottleId INT;

IF NOT EXISTS (SELECT 1 FROM Presentations WHERE Name = 'Box')
    INSERT INTO Presentations (Name, Deleted) VALUES ('Box', 0);

IF NOT EXISTS (SELECT 1 FROM Presentations WHERE Name = 'Bottle')
    INSERT INTO Presentations (Name, Deleted) VALUES ('Bottle', 0);

SELECT @PresentationBoxId = Id FROM Presentations WHERE Name = 'Box';
SELECT @PresentationBottleId = Id FROM Presentations WHERE Name = 'Bottle';

-- Drugs
DECLARE @DrugAspirinId INT;
DECLARE @DrugAmoxicillinId INT;

IF NOT EXISTS (SELECT 1 FROM Drugs WHERE Code = 'DRG-001')
    INSERT INTO Drugs (Code, Name, Symptom, Quantity, Price, Stock, Prescription, Deleted, UnitMeasureId, PresentationId, PharmacyId)
    VALUES ('DRG-001', 'Aspirin', 'Headache', 10, 3.50, 100, 0, 0, @UnitTabletId, @PresentationBoxId, @PharmacyCentralId);

IF NOT EXISTS (SELECT 1 FROM Drugs WHERE Code = 'DRG-002')
    INSERT INTO Drugs (Code, Name, Symptom, Quantity, Price, Stock, Prescription, Deleted, UnitMeasureId, PresentationId, PharmacyId)
    VALUES ('DRG-002', 'Amoxicillin', 'Infection', 20, 12.00, 50, 1, 0, @UnitTabletId, @PresentationBoxId, @PharmacyNorteId);

SELECT @DrugAspirinId = Id FROM Drugs WHERE Code = 'DRG-001';
SELECT @DrugAmoxicillinId = Id FROM Drugs WHERE Code = 'DRG-002';

-- Invitations
IF NOT EXISTS (SELECT 1 FROM Invitations WHERE UserCode = 'INV-001')
    INSERT INTO Invitations (UserName, UserCode, IsActive, Created, PharmacyId, RoleId)
    VALUES ('newuser', 'INV-001', 1, GETDATE(), @PharmacyCentralId, @RoleEmployeeId);

-- Purchases
DECLARE @PurchaseId INT;

IF NOT EXISTS (SELECT 1 FROM Purchases WHERE TrackingCode = 'TRK-001')
    INSERT INTO Purchases (PurchaseDate, TotalAmount, BuyerEmail, TrackingCode)
    VALUES (GETDATE(), 15.50, 'buyer@pharmago.test', 'TRK-001');

SELECT @PurchaseId = Id FROM Purchases WHERE TrackingCode = 'TRK-001';

IF NOT EXISTS (SELECT 1 FROM PurchaseDetails WHERE PurchaseId = @PurchaseId AND DrugId = @DrugAspirinId)
    INSERT INTO PurchaseDetails (DrugId, PharmacyId, Price, PurchaseId, Quantity, Status)
    VALUES (@DrugAspirinId, @PharmacyCentralId, 3.50, @PurchaseId, 2, 'Paid');

-- Stock requests
DECLARE @StockRequestId INT;

IF NOT EXISTS (SELECT 1 FROM StockRequests WHERE EmployeeId = @EmployeeUserId AND Status = 1)
    INSERT INTO StockRequests (EmployeeId, RequestDate, Status)
    VALUES (@EmployeeUserId, GETDATE(), 1);

SELECT @StockRequestId = Id FROM StockRequests WHERE EmployeeId = @EmployeeUserId AND Status = 1;

IF NOT EXISTS (SELECT 1 FROM StockRequestDetails WHERE StockRequestId = @StockRequestId AND DrugId = @DrugAmoxicillinId)
    INSERT INTO StockRequestDetails (DrugId, Quantity, StockRequestId)
    VALUES (@DrugAmoxicillinId, 10, @StockRequestId);

-- Sessions
IF NOT EXISTS (SELECT 1 FROM Sessions WHERE UserId = @AdminUserId)
    INSERT INTO Sessions (UserId, Token) VALUES (@AdminUserId, NEWID());

-- Reservations
DECLARE @ReservationId INT;

IF NOT EXISTS (SELECT 1 FROM Reservations WHERE Code = 'RSV-001')
    INSERT INTO Reservations (Code, PharmacyId, UserEmail, ReservationDate, Status, PublicKey, PrivateKey)
    VALUES ('RSV-001', @PharmacyCentralId, 'buyer@pharmago.test', GETDATE(), 0, 'public-key-001', 'private-key-001');

SELECT @ReservationId = Id FROM Reservations WHERE Code = 'RSV-001';

IF NOT EXISTS (SELECT 1 FROM ReservationDetail WHERE ReservationId = @ReservationId AND DrugCode = 'DRG-001')
    INSERT INTO ReservationDetail (DrugCode, Quantity, ReservationId)
    VALUES ('DRG-001', 1, @ReservationId);

SET NOCOUNT OFF;
