﻿using JorJika.S3.Models;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JorJika.S3
{
    public class MinioS3Client : IS3Client
    {
        #region Properties

        private MinioClient _client;
        private string _bucketName;
        private const string metaDataPrefix = "X-Amz-Meta-";

        #endregion

        #region Constructor

        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName)
        {
            _client = new MinioClient(endpoint, accessKey: accessKey, secretKey: secretKey);
            _bucketName = bucketName.ToLower();
        }

        #endregion

        #region Bucket operations

        public async Task CreateBucket(string bucketName)
        {
            var exists = await _client.BucketExistsAsync(bucketName.ToLower());
            if (!exists)
            {
                await _client.MakeBucketAsync(bucketName.ToLower());
            }
        }

        public async Task RemoveBucket(string bucketName)
        {
            var exists = await _client.BucketExistsAsync(bucketName.ToLower());
            if (!exists)
            {
                await _client.RemoveBucketAsync(bucketName.ToLower());
            }
        }

        #endregion


        public async Task<S3Object> GetObjectInfo(string objectName, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;

            ObjectStat result = null;

            try
            {
                result = await _client.StatObjectAsync(bucket, objectName);
            }
            catch (InvalidEndpointException ex)
            {
                throw;
            }
            catch (ConnectionException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                //Ignored
            }

            if (result == null) return null;

            return new S3Object(result.ObjectName, bucket, result.Size, result.ETag, result.ContentType, result.MetaData, null);
        }

        public async Task<S3Object> GetObject(string objectName, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;

            var objectInfo = await GetObjectInfo(objectName, bucket);

            if (objectInfo != null)
                await _client.GetObjectAsync(bucket, objectName, (s) =>
                {
                    using (var ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[objectInfo.Size];
                        int read;
                        while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        objectInfo.Data = ms.ToArray();
                    }
                });

            return objectInfo;
        }

        public async Task<string> GetObjectURL(string objectName, int expiresInSeconds = 600, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;
            try
            {
                return await _client.PresignedGetObjectAsync(bucket, objectName, expiresInSeconds);
            }
            catch (InvalidEndpointException ex)
            {
                throw;
            }
            catch (ConnectionException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                //Ignored
            }

            return null;
        }

        public async Task SaveObject(string objectName, byte[] objectData, string contentType = null, Dictionary<string, string> metaData = null, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;
            if (objectData != null)
                using (var ms = new MemoryStream(objectData))
                {
                    await _client.PutObjectAsync(bucket, objectName, data: ms, size: ms.Length, contentType: contentType, metaData: metaData);
                }
            else
                await _client.PutObjectAsync(bucket, objectName, data: null, size: 0, contentType: contentType, metaData: metaData);
        }

        public async Task RemoveObject(string objectName, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;
            await _client.RemoveObjectAsync(bucket, objectName);
        }

        public async Task SaveText(string objectName, string content, string fileName = null, string fileExtension = "txt", string bucketName = null)
        {
            var metaData = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(fileName))
                metaData.Add($"{metaDataPrefix}FileName", fileName);

            if (!string.IsNullOrWhiteSpace(fileExtension))
                metaData.Add($"{metaDataPrefix}FileExtension", fileExtension);


            var bucket = bucketName?.ToLower() ?? _bucketName;
            await SaveObject(objectName, System.Text.Encoding.UTF8.GetBytes(content), "text/plain", metaData, bucket);
        }

        public async Task SavePDF(string objectName, byte[] objectData, string fileName = null, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;
            var metaData = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(fileName))
                metaData.Add($"{metaDataPrefix}FileName", fileName);

            metaData.Add($"{metaDataPrefix}FileExtension", "pdf");

            await SaveObject(objectName, objectData, "application/pdf", metaData, bucket);
        }
    }
}