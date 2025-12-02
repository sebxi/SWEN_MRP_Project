using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MyMediaList.Server;

/// <summary>
/// Event args for the HttpRestServer.RequestReceived event.
/// Encapsulates the HttpListenerContext and offers a convenient Respond(...) method.
/// </summary>
public sealed class HttpRestEventArgs : EventArgs
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private members                                                                                                  //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // constructors                                                                                                     //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Creates a new instance for the given context.</summary>
    public HttpRestEventArgs(HttpListenerContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public properties                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Gets the underlying HttpListenerContext.</summary>
    public HttpListenerContext Context { get; }

    /// <summary>Gets a value indicating whether a response has already been sent for this request.</summary>
    public bool Responded { get; private set; }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public methods                                                                                                   //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Sends a JSON response with the specified status code and payload object.
    /// If payload is null, an empty body is sent.
    /// </summary>
    /// <param name="status">HTTP status code to send.</param>
    /// <param name="payload">An object that will be serialized to JSON and written to the response body. May be null.</param>
    public void Respond(HttpStatusCode status, object? payload = null)
    {
        if (Responded) return;

        try
        {
            var resp = Context.Response;
            resp.StatusCode = (int)status;
            resp.ContentType = "application/json; charset=utf-8";

            if (payload != null)
            {
                string json = JsonSerializer.Serialize(payload, _jsonOptions);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                resp.ContentLength64 = bytes.Length;
                using var output = resp.OutputStream;
                output.Write(bytes, 0, bytes.Length);
            }
            else
            {
                resp.ContentLength64 = 0;
                // ensure OutputStream is closed so client receives response
                resp.OutputStream.Close();
            }
        }
        catch
        {
            // swallow any exceptions during response writing - we cannot do more here
        }
        finally
        {
            Responded = true;
        }
    }


    /// <summary>
    /// Reads the request body as UTF-8 text asynchronously.
    /// Useful for handlers that need to deserialize JSON from the client.
    /// </summary>
    /// <returns>Request body as string (may be empty).</returns>
    public async System.Threading.Tasks.Task<string> ReadRequestBodyAsync()
    {
        using var sr = new StreamReader(Context.Request.InputStream, Context.Request.ContentEncoding);
        return await sr.ReadToEndAsync().ConfigureAwait(false);
    }
}