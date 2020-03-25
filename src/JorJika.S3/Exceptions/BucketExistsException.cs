using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class BucketExistsException : S3BaseException
    {
        public BucketExistsException(string bucketName) : 
                base($"Bucket: '{bucketName}' already exists",
                     $"Bucket: '{bucketName}' already exists")
        {

        }
    }
}
