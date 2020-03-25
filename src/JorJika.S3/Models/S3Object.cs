using System;
using System.Collections.Generic;
using System.Text;

namespace JorJika.S3.Models
{
    public class S3Object
    {
        public S3Object(string objectName,
           string bucketName,
           long size,
           string eTag,
           string contentType,
           Dictionary<string, string> metaData,
           byte[] data)
        {
            ObjectName = objectName;
            BucketName = bucketName;
            Size = size;
            ETag = eTag;
            ContentType = contentType;
            MetaData = metaData;
            Data = data;
        }

        public string ObjectName { get; set; }
        public string BucketName { get; set; }
        public long Size { get; set; }
        public string ETag { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> MetaData { get; set; }

        public byte[] Data { get; set; }

        public string DataAsString() => Data == null ? null : Encoding.UTF8.GetString(Data);

        public override string ToString()
        {
            return $"{BucketName}/{ObjectName}";
        }
    }
}
