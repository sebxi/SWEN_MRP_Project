using System;
using System.Net;
using System.Threading.Tasks;

namespace MyMediaList.Server;

public sealed class HttpRestServer : IDisposable
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private members                                                                                                  //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>HTTP listener object.</summary>
    private readonly HttpListener _Listener;



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // constructors                                                                                                     //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Creates a new instance of this class.</summary>
    /// <param name="port">Port number for the server. Default: 8080</param>
    public HttpRestServer(int port = 8080)
    {
        _Listener = new();
        // Bind to all network interfaces on the given port. Use "http://localhost:8080/" if you prefer loopback only.
        _Listener.Prefixes.Add($"http://localhost:{port}/");
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public events                                                                                                    //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>The event is raised when a request has been received.</summary>
    public event EventHandler<HttpRestEventArgs>? RequestReceived;



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public properties                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Gets a value indicating if the server is running.</summary>
    public bool Running { get; private set; }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public methods                                                                                                   //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Starts and runs the server. This method blocks the calling thread until Stop() is called.</summary>
    public void Run()
    {
        if (Running) return;

        _Listener.Start();
        Running = true;

        while (Running)
        {
            // Synchronously wait for an incoming context; dispatch handling to a background task
            HttpListenerContext context = _Listener.GetContext();

            _ = Task.Run(() =>
            {
                HttpRestEventArgs args = new(context);
                try
                {
                    RequestReceived?.Invoke(this, args);
                }
                catch (Exception ex)
                {
                    // If an exception bubbles up from the handler, return a 500 (unless already responded).
                    if (!args.Responded)
                    {
                        args.Respond(HttpStatusCode.InternalServerError, new { success = false, reason = "Server error", detail = ex.Message });
                    }
                }

                // If no handler produced a response, return a 404 by default
                if (!args.Responded)
                {
                    args.Respond(HttpStatusCode.NotFound, new { success = false, reason = "Not found." });
                }
            });
        }
    }


    /// <summary>Stops the server.</summary>
    public void Stop()
    {
        // Closing the listener will cause GetContext / Start to throw/return; also set Running=false.
        try
        {
            _Listener.Close();
        }
        catch { }
        Running = false;
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // [interface] IDisposable                                                                                          //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Disposes the object and releases used resources.</summary>
    public void Dispose()
    {
        ((IDisposable)_Listener).Dispose();
    }
}