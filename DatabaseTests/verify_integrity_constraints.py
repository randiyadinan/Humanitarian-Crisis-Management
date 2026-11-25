import sqlite3
import sys

DATABASE = "humanitarian_test.db"

tests = [
    (
        "Negative Resource Quantity",
        "INSERT INTO Resources (Name, Quantity, Location, Status) "
        "VALUES ('Test', -1, 'Test', 'Available')",
        True
    ),
    (
        "Negative Shelter Capacity",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy) "
        "VALUES ('Test', 'Test', -1, 0)",
        True
    ),
    (
        "Negative Shelter Occupancy",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy) "
        "VALUES ('Test', 'Test', 10, -1)",
        True
    ),
    (
        "Occupancy Exceeds Capacity",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy) "
        "VALUES ('Test', 'Test', 10, 11)",
        True
    ),
    (
        "Zero Resource Quantity",
        "INSERT INTO Resources (Name, Quantity, Location, Status) "
        "VALUES ('Test', 0, 'Test', 'Available')",
        False
    ),
    (
        "Zero Shelter Capacity and Occupancy",
        "INSERT INTO Shelters (Name, Location, Capacity, CurrentOccupancy) "
        "VALUES ('Test', 'Test', 0, 0)",
        False
    )
]

conn = sqlite3.connect(DATABASE)
failed = 0

try:
    for name, sql, should_reject in tests:
        conn.execute("SAVEPOINT constraint_test")

        try:
            conn.execute(sql)

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
