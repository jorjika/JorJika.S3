using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class AccessDeniedException : S3BaseException
    {
        public AccessDeniedException(string bucketName, string backendMessage) :
                base($"Access denied for bucket '{bucketName}' or entire service", backendMessage)
        {

        }
    }
}
