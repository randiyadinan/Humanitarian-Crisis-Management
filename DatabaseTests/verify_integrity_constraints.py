import sqlite3
import sys

DATABASE = "humanitarian_test.db"

tests = [
    (
        "Negative Resource Quantity",
        "INSERT INTO Resources (Name, Category, Quantity, AllocatedQuantity, Location, Status) "
        "VALUES ('Test', 'Water', -1, 0, 'Test', 'Available')",
        True
    ),
    (
        "Negative Resource AllocatedQuantity",
        "INSERT INTO Resources (Name, Category, Quantity, AllocatedQuantity, Location, Status) "
        "VALUES ('Test', 'Water', 10, -1, 'Test', 'Available')",
        True
    ),
    (
        "AllocatedQuantity Exceeds Quantity",
        "INSERT INTO Resources (Name, Category, Quantity, AllocatedQuantity, Location, Status) "
        "VALUES ('Test', 'Water', 10, 11, 'Test', 'Available')",
        True
    ),
    (
        "Negative Shelter Capacity",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy, Status) "
        "VALUES ('Test', 'Test', -1, 0, 'Open')",
        True
    ),
    (
        "Negative Shelter Occupancy",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy, Status) "
        "VALUES ('Test', 'Test', 10, -1, 'Open')",
        True
    ),
    (
        "Occupancy Exceeds Capacity",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy, Status) "
        "VALUES ('Test', 'Test', 10, 11, 'Open')",
        True
    ),
    (
        "Zero Resource Quantity",
        "INSERT INTO Resources (Name, Category, Quantity, AllocatedQuantity, Location, Status) "
        "VALUES ('Test', 'Water', 0, 0, 'Test', 'Available')",
        False
    ),
    (
        "Zero Shelter Capacity and Occupancy",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy, Status) "
        "VALUES ('Test', 'Test', 0, 0, 'Open')",
        False
    ),
    (
        "Zero Shelter Occupant PersonCount",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy, Status) "
        "VALUES ('Occupant Test Shelter', 'Test', 10, 0, 'Open'); "
        "INSERT INTO ShelterOccupants (ShelterId, HouseholdName, PersonCount) "
        "VALUES ((SELECT Id FROM Shelters WHERE Name = 'Occupant Test Shelter'), 'Test', 0)",
        True
    ),
    (
        "Positive Shelter Occupant PersonCount",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy, Status) "
        "VALUES ('Occupant Test Shelter', 'Test', 10, 0, 'Open'); "
        "INSERT INTO ShelterOccupants (ShelterId, HouseholdName, PersonCount) "
        "VALUES ((SELECT Id FROM Shelters WHERE Name = 'Occupant Test Shelter'), 'Test', 1)",
        False
    )
]

conn = sqlite3.connect(DATABASE)
failed = 0

try:
    for name, sql, should_reject in tests:
        conn.execute("SAVEPOINT constraint_test")

        try:
            for statement in sql.split(";"):
                if statement.strip():
                    conn.execute(statement)

            if should_reject:
                print(f"{name}: FAIL - Invalid value was accepted")
                failed += 1
            else:
                print(f"{name}: PASS - Valid value accepted")

        except sqlite3.IntegrityError:
            if should_reject:
                print(f"{name}: PASS - Invalid value rejected")
            else:
                print(f"{name}: FAIL - Valid value was rejected")
                failed += 1

        finally:
            conn.execute("ROLLBACK TO constraint_test")
            conn.execute("RELEASE constraint_test")

finally:
    conn.close()

print(f"\nResult: {len(tests) - failed}/{len(tests)} tests passed")
sys.exit(1 if failed else 0)
