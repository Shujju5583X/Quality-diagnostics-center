using System;

class Program
{
    static void Main()
    {
        string hash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxep68lN/YpX4x9kK";
        bool result = BCrypt.Net.BCrypt.Verify("1234", hash);
        Console.WriteLine("Verifies: " + result);
    }
}
