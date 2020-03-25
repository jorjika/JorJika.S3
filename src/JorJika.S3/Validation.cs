using JorJika.S3.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JorJika.S3
{
    public static class Validation
    {
        public static Regex bucketNameRegex = new Regex("^[a-z0-9\\.]*$", RegexOptions.Compiled);
        public static Regex objectNameRegex = new Regex("^[a-zA-Z0-9\\/\\.]*[^\\/]$", RegexOptions.Compiled);

        /// <summary>
        /// Validates bucket name
        /// </summary>
        /// <param name="bucketName">Bucket name</param>
        /// <exception cref="BucketNameIsNotValidException">Thrown when bucket name is invalid.</exception>
        public static void ValidateBucketName(string bucketName)
        {
            if (!bucketNameRegex.IsMatch(bucketName))
                throw new BucketNameIsNotValidException();
        }

        /// <summary>
        /// Validates object name
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <exception cref="ObjectNameIsNotValidException">Thrown when bucket name is invalid.</exception>
        public static void ValidateObjectName(string objectName)
        {
            if (!objectNameRegex.IsMatch(objectName))
                throw new ObjectNameIsNotValidException();
        }
    }
}
