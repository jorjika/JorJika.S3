using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Exceptions
{
    public class BucketNameIsNotValidException : S3BaseException
    {
        public BucketNameIsNotValidException() : 
                base("Bucket name is not valid.", 
                    $"Bucket name is not valid. Allowed characters are 'a-z' lowercase and period '.'")
        {

        }
    }
}
