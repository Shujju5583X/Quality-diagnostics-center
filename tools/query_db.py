import sqlite3

db_path = r"E:\Quality diagnostics center\LabSystem.UI\bin\Debug\net451\lab.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

def dump_table(table_name):
    print(f"\n--- Data in {table_name} ---")
    try:
        cursor.execute(f"SELECT * FROM {table_name}")
        rows = cursor.fetchall()
        for r in rows:
            print(r)
    except Exception as e:
        print(f"Error querying {table_name}: {e}")

dump_table("Doctors")
dump_table("TestPanels")
dump_table("PanelTestTypes")
dump_table("ReferenceRanges")
dump_table("Specimens")

conn.close()
