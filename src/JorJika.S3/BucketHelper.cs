using JorJika.S3.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JorJika.S3
{
    public static class BucketHelper
    {
        public static Regex bucketNameRegex = new Regex(@"((^[a-z0-9\.]*$)(^[a-zA-Z0-9\/\.\-_]*[^\/]$))|(^[a-zA-Z0-9\/\.\-_]*[^\/]$)", RegexOptions.Compiled);
        public static (string bucketName, string objectName) ExtractObjectInfo(string objectId)
        {
            if (!bucketNameRegex.IsMatch(objectId))
                throw new ObjectIdIsNotValidException();

            var split = objectId.Split('/');
            if (split.Length > 1)
            {
                var bucket = split[0];
                var objName = objectId.Substring(bucket.Length + 1);
                return (bucket, objName);
            }

            return (null, objectId);
        }
    }

}