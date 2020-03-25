using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class EndpointUnreachableException : S3BaseException
    {
        public EndpointUnreachableException(string endpoint, string backendMessage) :
                base($"Could not connect to endpoint: '{endpoint}'", backendMessage)
        {

        }
    }
}
