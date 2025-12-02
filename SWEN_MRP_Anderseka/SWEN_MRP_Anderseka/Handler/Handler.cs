using MyMediaList.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyMediaList.Handlers
{
    public abstract class Handler : IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static members                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Available handlers.</summary>
        private static List<IHandler>? _Handlers = null;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static methods                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets a list of available handlers.</summary>
        /// <returns>Returns a list of handlers.</returns>
        private static List<IHandler> _GetHandlers()
        {
            List<IHandler> rval = new();

            foreach (Type i in Assembly.GetExecutingAssembly().GetTypes()
                .Where(m => m.IsAssignableTo(typeof(IHandler)) && !m.IsAbstract))
            {
                IHandler? h = (IHandler?)Activator.CreateInstance(i);
                if (h is not null) { rval.Add(h); }
            }

            return rval;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Provides an event handler for the <see cref="HttpRestServer.RequestReceived"/> event.</summary>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        public static void HandleEvent(object? sender, HttpRestEventArgs e)
        {
            foreach (IHandler i in (_Handlers ??= _GetHandlers()))
            {
                i.Handle(e);
                if (e.Responded) break;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [interface] IHandler                                                                                             //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Handles a request if possible.</summary>
        /// <param name="e">Event arguments.</param>
        public abstract void Handle(HttpRestEventArgs e);
    }
}