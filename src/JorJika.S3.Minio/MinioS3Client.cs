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
using Polly.Retry;
using System.Diagnostics;

namespace JorJika.S3
{
    public class MinioS3Client : IS3Client
    {
        #region Properties

        private readonly string _endpoint;
        private IObjectOperations _objectOperationsClient;
        private IBucketOperations _bucketOperationsClient;
        private string _bucketName;
        private const string metaDataPrefix = "X-Amz-Meta-";
        private readonly ILogger<IS3Client> _logger;
        private readonly int _retryCount = 1;
        private readonly int _retryInSeconds = 1;
        private readonly AsyncRetryPolicy _requestRetryPolicy;

        #endregion

        #region Constructor

        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName, bool ssl = false)
                       : this(endpoint, accessKey, secretKey, bucketName, null, 0, 0, ssl)
        {
        }


        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName, ILogger<IS3Client> logger, bool ssl = false)
                      : this(endpoint, accessKey, secretKey, bucketName, logger, 0, 0, ssl)
        {
        }
        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName, int retryCount, int retryInSeconds, bool ssl = false)
                      : this(endpoint, accessKey, secretKey, bucketName, null, retryCount, retryInSeconds, ssl)
        {
        }

        public MinioS3Client(string endpoint, string accessKey, string secretKey, string bucketName, ILogger<IS3Client> logger, int retryCount, int retryInSeconds, bool ssl = false)
            : this(ssl ? new MinioClient(endpoint, accessKey: accessKey, secretKey: secretKey).WithSSL() : new MinioClient(endpoint, accessKey: accessKey, secretKey: secretKey),
                  ssl ? new MinioClient(endpoint, accessKey: accessKey, secretKey: secretKey).WithSSL() : new MinioClient(endpoint, accessKey: accessKey, secretKey: secretKey),
                  endpoint, bucketName, logger, retryCount, retryInSeconds)
        {
        }

        public MinioS3Client(IBucketOperations bucketOperationsClient, IObjectOperations objectOperationsClient, string endpoint, string bucketName, ILogger<IS3Client> logger, int retryCount, int retryInSeconds)
        {
            if (retryCount < 0 || retryInSeconds < 0)
                throw new S3BaseException("Retry count and retry in seconds parameters should be > 0");

            _endpoint = endpoint;
            _objectOperationsClient = objectOperationsClient;
            _bucketOperationsClient = bucketOperationsClient;
            _bucketName = bucketName.ToLower();
            _logger = logger;
            _retryCount = retryCount;
            _retryInSeconds = retryInSeconds;

            _requestRetryPolicy = Policy.Handle<ConnectionException>().Or<InternalServerException>().Or<InternalClientException>()
                                  .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(_retryInSeconds), (ex, time, retryAttempt, ctx) =>
                                  {
                                      var message = $"Problem connecting to S3 endpoint. Retrying {retryAttempt}/{_retryCount}. Endpoint={_endpoint}";
                                      _logger?.LogWarning(ex, message);
                                      Debug.WriteLine(message);
                                  });
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

                await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    var exists = await _bucketOperationsClient.BucketExistsAsync(bucketName.ToLower());

                    if (exists)
                        throw new BucketExistsException(bucketName);

                    await _bucketOperationsClient.MakeBucketAsync(bucketName.ToLower());
                });
            }
            catch (MinioException ex) when (ex is ConnectionException
                                         || ex is InternalServerException
                                         || ex is InternalClientException
                                         || ex is InvalidEndpointException
                                            )
            {
                throw new EndpointUnreachableException(_endpoint, ex.ToString());
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
        /// <exception cref="BucketNotFoundException">Thrown when bucket does not exist.</exception>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task RemoveBucket(string bucketName)
        {
            try
            {
                await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    var exists = await _bucketOperationsClient.BucketExistsAsync(bucketName.ToLower());

                    if (!exists)
                        throw new Exceptions.BucketNotFoundException(bucketName);

                    await _bucketOperationsClient.RemoveBucketAsync(bucketName.ToLower());
                });
            }
            catch (MinioException ex) when (ex is ConnectionException
                                         || ex is InternalServerException
                                         || ex is InternalClientException
                                         || ex is InvalidEndpointException
                                            )
            {
                throw new EndpointUnreachableException(_endpoint, ex.ToString());
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

        /// <summary>
        /// Gets object information - Without file data
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Returns object information and metadata</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="ObjectNotFoundException">Thrown when object is not found.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task<S3Object> GetObjectInfo(string objectId)
        {
            var s3Obj = BucketHelper.ExtractObjectInfo(objectId);
            var bucket = s3Obj.bucketName?.ToLower() ?? _bucketName;
            var objectName = s3Obj.objectName;

            S3Object result = null;

            try
            {
                result = await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    var response = await _objectOperationsClient.StatObjectAsync(bucket, objectName);

                    if (response == null) throw new Exceptions.ObjectNotFoundException(objectName, bucket);

                    return new S3Object(response.ObjectName, bucket, response.Size, response.ETag, response.ContentType, response.MetaData, null);
                });
            }
            catch (MinioException ex) when (ex is ConnectionException
                                         || ex is InternalServerException
                                         || ex is InternalClientException
                                         || ex is InvalidEndpointException
                                            )
            {
                throw new EndpointUnreachableException(_endpoint, ex.ToString());
            }
            catch (S3BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new S3BaseException(ex.Message, ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// Get object - With data
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Returns actual object data - bytes</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="ObjectNotFoundException">Thrown when object is not found.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task<S3Object> GetObject(string objectId)
        {
            var s3Obj = BucketHelper.ExtractObjectInfo(objectId);
            var bucket = s3Obj.bucketName?.ToLower() ?? _bucketName;
            var objectName = s3Obj.objectName;

            var result = await GetObjectInfo(objectId);

            try
            {
                result.Data = await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    byte[] data = null;

                    await _objectOperationsClient.GetObjectAsync(bucket, objectName, (s) =>
                    {
                        using (var ms = new MemoryStream())
                        {
                            byte[] buffer = new byte[result.Size];
                            int read;
                            while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, read);
                            }

                            ms.Seek(0, SeekOrigin.Begin);
                            data = ms.ToArray();
                        }
                    });

                    return data;
                });
            }
            catch (MinioException ex) when (ex is ConnectionException
                                         || ex is InternalServerException
                                         || ex is InternalClientException
                                         || ex is InvalidEndpointException
                                            )
            {
                throw new EndpointUnreachableException(_endpoint, ex.ToString());
            }
            catch (S3BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new S3BaseException(ex.Message, ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// Gets object URL for downloading from storage (Temporary URL support varies by implementation)
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <param name="expiresInSeconds">Temporary link expiration time in seconds. Defaults to 12 hours</param>
        /// <returns>Returns temporary URL of object for download</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="Exceptions.ObjectNotFoundException">Thrown when object is not found.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task<string> GetObjectURL(string objectId, int expiresInSeconds = 600)
        {
            var s3Obj = BucketHelper.ExtractObjectInfo(objectId);
            var bucket = s3Obj.bucketName?.ToLower() ?? _bucketName;
            var objectName = s3Obj.objectName;

            string result = null;

            try
            {
                result = await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    return await _objectOperationsClient.PresignedGetObjectAsync(bucket, objectName, expiresInSeconds);
                });

                if (result == null)
                    throw new Exceptions.ObjectNotFoundException(objectId, bucket);

            }
            catch (MinioException ex) when (ex is ConnectionException
                                         || ex is InternalServerException
                                         || ex is InternalClientException
                                         || ex is InvalidEndpointException
                                            )
            {
                throw new EndpointUnreachableException(_endpoint, ex.ToString());
            }
            catch (S3BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new S3BaseException(ex.Message, ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// Save binary object
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <param name="objectData">Object byte array</param>
        /// <param name="contentType">Content type - Optional (Used for PDF and text files to directly show in browser when issuing temporary link)</param>
        /// <param name="metaData">Object meta data</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Object Id</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task<string> SaveObject(string objectName, byte[] objectData, string contentType = null, Dictionary<string, string> metaData = null, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;

            Validation.ValidateBucketName(bucket);
            Validation.ValidateObjectName(objectName);

            //var s3Obj = BucketHelper.ExtractObjectInfo(objectName);
            //var bucket = s3Obj.bucketName?.ToLower() ?? _bucketName;
            //var objectName = s3Obj.objectName;

            if (objectName.StartsWith(bucket))
                objectName = objectName.Replace($"{bucket}/", "");

            try
            {
                await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    if (objectData != null)
                        using (var ms = new MemoryStream(objectData))
                        {
                            await _objectOperationsClient.PutObjectAsync(bucket, objectName, data: ms, size: ms.Length, contentType: contentType, metaData: metaData);
                        }
                    else
                        await _objectOperationsClient.PutObjectAsync(bucket, objectName, data: null, size: 0, contentType: contentType, metaData: metaData);
                });
            }
            catch (MinioException ex) when (ex is ConnectionException
                                         || ex is InternalServerException
                                         || ex is InternalClientException
                                         || ex is InvalidEndpointException
                                            )
            {
                throw new EndpointUnreachableException(_endpoint, ex.ToString());
            }
            catch (S3BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new S3BaseException(ex.Message, ex.ToString());
            }

            return $"{bucket}/{objectName}";
        }

        /// <summary>
        /// Removes object from storage
        /// </summary>
        /// <param name="objectId">Object Id</param>
        /// <returns></returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task RemoveObject(string objectId)
        {
            var s3Obj = BucketHelper.ExtractObjectInfo(objectId);
            var bucket = s3Obj.bucketName?.ToLower() ?? _bucketName;
            var objectName = s3Obj.objectName;

            try
            {
                await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    await _objectOperationsClient.RemoveObjectAsync(bucket, objectName);
                });
            }
            catch (MinioException ex) when (ex is ConnectionException
                                         || ex is InternalServerException
                                         || ex is InternalClientException
                                         || ex is InvalidEndpointException
                                            )
            {
                throw new EndpointUnreachableException(_endpoint, ex.ToString());
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
        /// Save text file to storage
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <param name="content">Text file content</param>
        /// <param name="fileName">File name - Optional (If you are downloading file from browser file name is automatically filled with this value)</param>
        /// <param name="fileExtension">File extension, defaults to txt.</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Object Id</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task<string> SaveText(string objectName, string content, string fileName = null, string fileExtension = "txt", string bucketName = null)
        {
            var metaData = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(fileName))
                metaData.Add($"{metaDataPrefix}FileName", fileName);

            if (!string.IsNullOrWhiteSpace(fileExtension))
                metaData.Add($"{metaDataPrefix}FileExtension", fileExtension);


            var bucket = bucketName?.ToLower() ?? _bucketName;
            return await SaveObject(objectName, System.Text.Encoding.UTF8.GetBytes(content), "text/plain", metaData, bucket);
        }

        /// <summary>
        /// Save pdf file to storage
        /// </summary>
        /// <param name="objectName">Object name</param>
        /// <param name="objectData">PDF file byte array</param>
        /// <param name="fileName">File name - Optional (If you are downloading file from browser file name is automatically filled with this value)</param>
        /// <param name="bucketName">Bucket name - Optional if passed throuhg constructor</param>
        /// <returns>Object Id</returns>
        /// <exception cref="EndpointUnreachableException">Thrown when S3 endpoint is unreachable.</exception>
        /// <exception cref="S3BaseException">Thrown when exception is not handled.</exception>
        public async Task<string> SavePDF(string objectName, byte[] objectData, string fileName = null, string bucketName = null)
        {
            var bucket = bucketName?.ToLower() ?? _bucketName;
            var metaData = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(fileName))
                metaData.Add($"{metaDataPrefix}FileName", fileName);

            metaData.Add($"{metaDataPrefix}FileExtension", "pdf");

            return await SaveObject(objectName, objectData, "application/pdf", metaData, bucket);
        }

        #endregion
    }
}
