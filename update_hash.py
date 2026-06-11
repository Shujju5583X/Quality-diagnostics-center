import sqlite3

db_path = r"E:\Quality diagnostics center\LabSystem.UI\bin\Debug\net451\lab.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

correct_hash = "$2a$11$/kj.NC923I71HcIDmIOASeJhA7Il5NLBh6Mb/nO8Thz/J2ooDHwIC"

cursor.execute("UPDATE Staff SET PinHash = ?", (correct_hash,))
conn.commit()
print(f"Updated {cursor.rowcount} rows with the correct BCrypt hash for '1234'")
conn.close()
