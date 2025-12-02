using MyMediaList.Server;
using MyMediaList.Handlers;

namespace MyMediaList
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            using HttpRestServer svr = new();
            
            // log incoming requests
            svr.RequestReceived += (sender, evt) =>
            {
                Console.WriteLine($"Incoming request: {evt.Context.Request.HttpMethod} {evt.Context.Request.Url}");
            };

            // route requests to available handlers
            svr.RequestReceived += Handler.HandleEvent;

            Console.WriteLine("Starting server on http://localhost:8080");
            svr.Run();
        }
    }
}