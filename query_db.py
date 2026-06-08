import sqlite3

db_path = r"E:\Quality diagnostics center\LabSystem.UI\bin\Debug\net462\lab.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()
cursor.execute("SELECT StaffId, FullName, PinHash FROM Staff")
rows = cursor.fetchall()
print("Staff Records:")
for r in rows:
    print(r)
conn.close()
