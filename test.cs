using System;
using System.Linq;
using LabSystem.Data;

public class Runner {
    public static void Main() {
        try {
            using (var db = new LabDbContext()) {
                var q = db.GetUnifiedQueue();
                Console.WriteLine(q.ToString());
                var items = q.ToList();
                Console.WriteLine("Success: " + items.Count);
            }
        } catch (Exception ex) {
            Console.WriteLine("Exception: " + ex.ToString());
        }
    }
}
