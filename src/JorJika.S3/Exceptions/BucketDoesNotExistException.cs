using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class BucketDoesNotExistException : S3BaseException
    {
        public BucketDoesNotExistException(string bucketName) : 
                base($"Bucket: '{bucketName}' does not exist",
                     $"Bucket: '{bucketName}' does not exist")
        {

        }
    }
}
