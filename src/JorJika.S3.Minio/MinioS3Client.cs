using JorJika.S3.Models;
using JorJika.S3.Exceptions;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace JorJika.S3
{
    public class MinioS3Client : IS3Client
    {
        #region Properties

        private readonly string _endpoint;
        private MinioClient _client;
        private string _bucketName;
        private const string metaDataPrefix = "X-Amz-Meta-";
        private readonly ILogger<IS3Client> _logger;
        private readonly int _retryCount = 1;
        private readonly int _retryInSeconds = 1;

        #endregion

        #region Constructor

        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName)
                      : this(endpoint, accessKey, secretKey, bucketName, null, 1, 1)
        {
        }


        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName, ILogger<IS3Client> logger)
                      : this(endpoint, accessKey, secretKey, bucketName, logger, 1, 1)
        {
        }
        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName, int retryCount, int retryInSeconds)
                      : this(endpoint, accessKey, secretKey, bucketName, null, retryCount, retryInSeconds)
        {
        }

        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName, ILogger<IS3Client> logger, int retryCount, int retryInSeconds)
        {
            if (retryCount <= 0 || retryInSeconds <= 0)
                throw new S3BaseException("Retry count and retry in seconds parameters should be greater than zero.");

            _endpoint = endpoint;
            _client = new MinioClient(endpoint, accessKey: accessKey, secretKey: secretKey);
            _bucketName = bucketName.ToLower();
            _logger = logger;
            _retryCount = retryCount;
            _retryInSeconds = retryInSeconds;
        }

        #endregion

        #region Bucket operations

        /// <summary>
        /// Create bucket
        /// </summary>
        /// <param name="bucketName">Bucket name - Validation: lower case alpha numeric characters plus dots.</param>
        /// <returns></returns>
        /// <exception cref="BucketNameIsNotValidException">Thrown when bucket name is invalid.</exception>
        /// <exception cref="BucketExistsException">Thrown when bucket already exists with this name.</exception>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task CreateBucket(string bucketName)
        {
            try
            {
                Validation.ValidateBucketName(bucketName);

                var currentRetry = 0;
                var polly = Policy.Handle<ConnectionException>().Or<InternalServerException>()
                                  .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(_retryInSeconds), (ex, time) =>
                                  {
                                      currentRetry++;
                                      _logger.LogWarning(ex, $"Problem connecting to S3 endpoint. Retrying {currentRetry}/{_retryCount}. Endpoint={_endpoint}");

                                  });

                await polly.Execute(async () =>
                {
                    var exists = await _client.BucketExistsAsync(bucketName.ToLower());

                    if (exists)
                        throw new BucketExistsException(bucketName);

                    await _client.MakeBucketAsync(bucketName.ToLower());
                });
            }
            catch (MinioException ex) when (ex is ConnectionException ||
                                            ex is InternalServerException
                                            )
            {
                throw new EndpointUnreachableException("", ex.ToString());
            }
            catch (S3BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new S3BaseException(ex.Message, ex.ToString());
            }
        }

        /// <summary>
        /// Remove bucket
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        /// <exception cref="BucketNameIsNotValidException">Thrown when bucket name is invalid.</exception>
        /// <exception cref="BucketNotFoundException">Thrown when bucket does not exist.</exception>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task RemoveBucket(string bucketName)
        {
            try
            {
                Validation.ValidateBucketName(bucketName);

                var currentRetry = 0;
                var polly = Policy.Handle<ConnectionException>().Or<InternalServerException>()
                                  .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(_retryInSeconds), (ex, time) =>
                                  {
                                      currentRetry++;
                                      _logger.LogWarning(ex, $"Problem connecting to S3 endpoint. Retrying {currentRetry}/{_retryCount}. Endpoint={_endpoint}");

                                  });

                await polly.Execute(async () =>
                {
                    var exists = await _client.BucketExistsAsync(bucketName.ToLower());

                    if (!exists)
                        throw new Exceptions.BucketNotFoundException(bucketName);

                    await _client.RemoveBucketAsync(bucketName.ToLower());
                });
            }
            catch (MinioException ex) when (ex is ConnectionException ||
                                            ex is InternalServerException
                                            )
            {
                throw new EndpointUnreachableException("", ex.ToString());
            }
            catch (S3BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new S3BaseException(ex.Message, ex.ToString());
            }
        }

        #endregion

        #region Object operations

        public async Task<S3Object> GetObjectInfo(string objectName, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;

            Validation.ValidateBucketName(bucket);

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

            Validation.ValidateBucketName(bucket);

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

            Validation.ValidateBucketName(bucket);

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

            Validation.ValidateBucketName(bucket);
            Validation.ValidateObjectName(objectName);

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

        #endregion
    }
}
