using MyMediaList.Server;
using MyMediaList.Handlers;
using Npgsql;

namespace MyMediaList
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            TestDatabaseConnection();

            using HttpRestServer svr = new();

            svr.RequestReceived += (sender, evt) =>
            {
                Console.WriteLine($"Incoming request: {evt.Context.Request.HttpMethod} {evt.Context.Request.Url}");
            };

            svr.RequestReceived += Handler.HandleEvent;
            Console.WriteLine("Starting server on http://localhost:8080");
            svr.Run();
        }

        static void TestDatabaseConnection()
        {
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                using var cmd = new NpgsqlCommand("SELECT 1", conn);
                var result = cmd.ExecuteScalar();

                Console.WriteLine("✅ Database connected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Database connection failed:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}