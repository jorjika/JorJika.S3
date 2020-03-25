using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class ObjectNotFoundException : S3BaseException
    {
        public ObjectNotFoundException(string objectName, string bucketName) :
                base($"Object not found. Object Name: {objectName}; Bucket: '{bucketName}';",
                     $"Object not found. Object Name: {objectName}; Bucket: '{bucketName}';")
        {

        }
    }
}
