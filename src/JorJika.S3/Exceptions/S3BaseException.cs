using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    /// <summary>
    /// Base exception type
    /// </summary>
    public class S3BaseException : Exception
    {
        /// <summary>
        /// Backend message. By default includes stack trace if applicable.
        /// </summary>
        public string BackendMessage { get; }

        /// <summary>
        /// This constructor sets Exception.Message property
        /// </summary>
        /// <param name="message">Exception message</param>
        public S3BaseException(string message) : base(message)
        {
            BackendMessage = message;
        }

        /// <summary>
        /// This constructor sets message to Exception.Message property and backendMessage to S3BaseException.BackendMessage property
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="backendMessage">Backend message. By default includes stack trace if applicable.</param>
        public S3BaseException(string message, string backendMessage) : base(message)
        {
            BackendMessage = backendMessage;
        }
    }
}
