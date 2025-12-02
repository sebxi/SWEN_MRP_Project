using MyMediaList.Server;

namespace MyMediaList
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            using HttpRestServer svr = new();
            
            svr.RequestReceived += (sender, evt) =>
            {
                Console.WriteLine($"Incoming request: {evt.Context.Request.HttpMethod} {evt.Context.Request.Url}");
                // keine Antwort senden, Server liefert automatisch 404
            };

            Console.WriteLine("Starting server on http://localhost:8080");
            svr.Run();
        }
    }
}