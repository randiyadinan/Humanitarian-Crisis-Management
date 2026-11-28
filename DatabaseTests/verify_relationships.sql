.bail on
.headers on
.mode column

PRAGMA foreign_keys = ON;

-- 1. Database integrity check
SELECT 'Database Integrity' AS Test,
       CASE WHEN (SELECT integrity_check FROM pragma_integrity_check) = 'ok'
       THEN 'PASS' ELSE 'FAIL' END AS Result;

-- 2. Foreign key validation
SELECT 'Foreign Key Validation' AS Test,
       CASE WHEN NOT EXISTS (SELECT 1 FROM pragma_foreign_key_check)
       THEN 'PASS' ELSE 'FAIL' END AS Result;

BEGIN TRANSACTION;

CREATE TEMP TABLE TestIds (
    Name TEXT PRIMARY KEY,
    Id INTEGER
);

-- 3. Cascade delete test
INSERT INTO AssistanceRequests
(Category, Urgency, Description, Location, Status)
VALUES ('Test', 'Low', 'Database relationship test', 'Test Location', 'Pending');

INSERT INTO TestIds VALUES ('Request', last_insert_rowid());

INSERT INTO AIRecommendations
(ConfidenceScore, MatchingResource, RequestId, SuggestedAction)
SELECT 0.95, 'Test Resource', Id, 'Test Action'
FROM TestIds WHERE Name = 'Request';

DELETE FROM AssistanceRequests
WHERE Id = (SELECT Id FROM TestIds WHERE Name = 'Request');

SELECT 'Cascade Delete' AS Test,
       CASE WHEN NOT EXISTS (
           SELECT 1 FROM AIRecommendations
           WHERE RequestId = (SELECT Id FROM TestIds WHERE Name = 'Request')
       ) THEN 'PASS' ELSE 'FAIL' END AS Result;

-- 4. Crisis Event SetNull test
INSERT INTO CrisisEvents
(Title, EventType, Location, Severity, DateReported)
VALUES ('Test Event', 'Test', 'Test Location', 'Low', '2026-11-25');

INSERT INTO TestIds VALUES ('CrisisEvent', last_insert_rowid());

INSERT INTO Shelters
(Capacity, CrisisEventId, CurrentOccupancy, Location, Name, Status)
SELECT 100, Id, 10, 'Test Location', 'Test Shelter', 'Open'
FROM TestIds WHERE Name = 'CrisisEvent';

INSERT INTO Resources
(AllocatedQuantity, Category, CrisisEventId, Location, Name, Quantity, ShelterId, Status)
SELECT 0, 'Water', Id, 'Test Location', 'Test Resource', 10,
       (SELECT Id FROM Shelters WHERE Name = 'Test Shelter'),
       'Available'
FROM TestIds WHERE Name = 'CrisisEvent';

INSERT INTO Volunteers
(AssignedTask, ContactNumber, CrisisEventId, Name, ShelterId, Skills, Specialisation, Status)
SELECT '', '0000000000', Id, 'Test Volunteer',
       (SELECT Id FROM Shelters WHERE Name = 'Test Shelter'),
       'First Aid', 'General Relief', 'Available'
FROM TestIds WHERE Name = 'CrisisEvent';

INSERT INTO Donations
(Amount, CrisisEventId, Date, Description, DonationType, DonorName)
SELECT '100', Id, '2026-11-25', 'Test Donation', 'Cash', 'Test Donor'
FROM TestIds WHERE Name = 'CrisisEvent';

DELETE FROM CrisisEvents
WHERE Id = (SELECT Id FROM TestIds WHERE Name = 'CrisisEvent');

SELECT 'Crisis Event SetNull' AS Test,
       CASE WHEN
           (SELECT CrisisEventId FROM Shelters WHERE Name = 'Test Shelter') IS NULL
           AND (SELECT CrisisEventId FROM Resources WHERE Name = 'Test Resource') IS NULL
           AND (SELECT CrisisEventId FROM Volunteers WHERE Name = 'Test Volunteer') IS NULL
           AND (SELECT CrisisEventId FROM Donations WHERE DonorName = 'Test Donor') IS NULL
       THEN 'PASS' ELSE 'FAIL' END AS Result;

-- 5. Shelter SetNull test
INSERT INTO Shelters
(Capacity, CurrentOccupancy, Location, Name, Status)
VALUES (50, 5, 'Test Location', 'Shelter Delete Test', 'Open');

INSERT INTO TestIds VALUES ('Shelter', last_insert_rowid());

INSERT INTO Resources
(AllocatedQuantity, Category, Location, Name, Quantity, ShelterId, Status)
SELECT 0, 'Food', 'Test Location', 'Shelter Test Resource', 5, Id, 'Available'
FROM TestIds WHERE Name = 'Shelter';

INSERT INTO Volunteers
(AssignedTask, ContactNumber, Name, ShelterId, Skills, Specialisation, Status)
SELECT '', '0000000001', 'Shelter Test Volunteer', Id,
       'Medical', 'General Relief', 'Available'
FROM TestIds WHERE Name = 'Shelter';

DELETE FROM Shelters
WHERE Id = (SELECT Id FROM TestIds WHERE Name = 'Shelter');

SELECT 'Shelter SetNull' AS Test,
       CASE WHEN
           (SELECT ShelterId FROM Resources WHERE Name = 'Shelter Test Resource') IS NULL
           AND (SELECT ShelterId FROM Volunteers WHERE Name = 'Shelter Test Volunteer') IS NULL
       THEN 'PASS' ELSE 'FAIL' END AS Result;

-- 6. Shelter Occupant Cascade Delete test
INSERT INTO Shelters
(Capacity, CurrentOccupancy, Location, Name, Status)
VALUES (50, 4, 'Test Location', 'Occupant Cascade Test', 'Open');

INSERT INTO TestIds VALUES ('OccupantShelter', last_insert_rowid());

INSERT INTO ShelterOccupants
(HouseholdName, PersonCount, ShelterId)
SELECT 'Test Household', 4, Id
FROM TestIds WHERE Name = 'OccupantShelter';

DELETE FROM Shelters
WHERE Id = (SELECT Id FROM TestIds WHERE Name = 'OccupantShelter');

SELECT 'Shelter Occupant Cascade Delete' AS Test,
       CASE WHEN NOT EXISTS (
           SELECT 1 FROM ShelterOccupants
           WHERE ShelterId = (SELECT Id FROM TestIds WHERE Name = 'OccupantShelter')
       ) THEN 'PASS' ELSE 'FAIL' END AS Result;

ROLLBACK;

SELECT 'Test data cleanup' AS Test, 'PASS' AS Result;
