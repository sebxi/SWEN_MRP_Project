using MyMediaList.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMediaList.Handlers
{
    /// <summary>Classes capable of handling request implement this interface.</summary>
    public interface IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Handles a request if possible.</summary>
        /// <param name="e">Event arguments.</param>
        public void Handle(HttpRestEventArgs e);
    }
}