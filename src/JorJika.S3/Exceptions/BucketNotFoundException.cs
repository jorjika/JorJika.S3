using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class BucketNotFoundException : S3BaseException
    {
        public BucketNotFoundException(string bucketName) : 
                base($"Bucket: '{bucketName}' does not exist",
                     $"Bucket: '{bucketName}' does not exist")
        {

        }
    }
}
