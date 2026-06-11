import sqlite3
import os

db_path = r"E:\Quality diagnostics center\LabSystem.UI\bin\Debug\net451\lab.db"
init_sql = r"E:\Quality diagnostics center\LabSystem.Data\Migrations\V1__init.sql"
seed_sql = r"E:\Quality diagnostics center\seed.sql"

if not os.path.exists(os.path.dirname(db_path)):
    os.makedirs(os.path.dirname(db_path))

conn = sqlite3.connect(db_path)
cursor = conn.cursor()

with open(init_sql, 'r') as f:
    sql = f.read()
    try:
        cursor.executescript(sql)
        print("Init SQL executed successfully.")
    except Exception as e:
        print("Init SQL failed:", e)

with open(seed_sql, 'r') as f:
    sql = f.read()
    try:
        cursor.executescript(sql)
        print("Seed SQL executed successfully.")
    except Exception as e:
        print("Seed SQL failed:", e)

conn.commit()
conn.close()
