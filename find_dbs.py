import os
import glob

print("Finding all lab.db files:")
for path in glob.glob("**/lab.db", recursive=True):
    print(f"Path: {path}, Size: {os.path.getsize(path)} bytes")
