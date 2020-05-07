using Amazon.Auth.AccessControlPolicy;
using Amazon.Runtime;
using Amazon.S3;
using JorJika.S3.Exceptions;
using JorJika.S3.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JorJika.S3.AWS
{
    public class AWSS3Client : IS3Client
    {
        #region Properties
        
        private readonly string _endpoint;
        private IAmazonS3 _client;
        private string _bucketName;
        private const string metaDataPrefix = "X-Amz-Meta-";
        private readonly ILogger<IS3Client> _logger;
        private readonly int _retryCount = 1;
        private readonly int _retryInSeconds = 1;
        private readonly AsyncRetryPolicy _requestRetryPolicy;

        #endregion

        #region Constructor

        public AWSS3Client(AmazonS3Config config, string accessKey, string secretKey, string bucketName)
            : this(new AmazonS3Client(awsAccessKeyId: accessKey, awsSecretAccessKey: secretKey, clientConfig: config), config.ServiceURL, bucketName, null, 0, 0)
        {
        }
        public AWSS3Client(AmazonS3Config config, string accessKey, string secretKey, string bucketName, ILogger<IS3Client> logger)
            : this(new AmazonS3Client(awsAccessKeyId: accessKey, awsSecretAccessKey: secretKey, clientConfig: config), config.ServiceURL, bucketName, logger, 0, 0)
        {
        }

        public AWSS3Client(AmazonS3Config config, string accessKey, string secretKey, string bucketName, int retryCount, int retryInSeconds)
            : this(new AmazonS3Client(awsAccessKeyId: accessKey, awsSecretAccessKey: secretKey, clientConfig: config), config.ServiceURL, bucketName, null, retryCount, retryInSeconds)
        {
        }

        public AWSS3Client(AmazonS3Config config, string accessKey, string secretKey, string bucketName, ILogger<IS3Client> logger, int retryCount, int retryInSeconds)
            : this(new AmazonS3Client(awsAccessKeyId: accessKey, awsSecretAccessKey: secretKey, clientConfig: config), config.ServiceURL, bucketName, logger, retryCount, retryInSeconds)
        {
        }

        public AWSS3Client(IAmazonS3 client, string endpoint, string bucketName, ILogger<IS3Client> logger, int retryCount, int retryInSeconds)
        {
            if (retryCount < 0 || retryInSeconds < 0)
                throw new S3BaseException("Retry count and retry in seconds parameters should be > 0");

            _endpoint = endpoint;
            _client = client;
            _bucketName = bucketName.ToLower();
            _logger = logger;
            _retryCount = retryCount;
            _retryInSeconds = retryInSeconds;

            _requestRetryPolicy = Polly.Policy.Handle<AmazonS3Exception>()
                                  .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(_retryInSeconds), (ex, time, retryAttempt, ctx) =>
                                  {
                                      var message = $"Problem connecting to S3 endpoint. Retrying {retryAttempt}/{_retryCount}. Endpoint={_endpoint}";
                                      _logger?.LogWarning(ex, message);
                                      Debug.WriteLine(message);
                                  });
        }

        #endregion

        public async Task CreateBucket(string bucketName)
        {
            try
            {
                Validation.ValidateBucketName(bucketName);

                await _requestRetryPolicy.ExecuteAsync(async () =>
                {
                    var exists = await _client.DoesS3BucketExistAsync(bucketName.ToLower());

                    if (exists)
                        throw new BucketExistsException(bucketName);

                    var response = await _client.PutBucketAsync(bucketName.ToLower());
                });
               
            }
            catch (AmazonServiceException ex) when (ex is AmazonS3Exception
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

        public Task<S3Object> GetObject(string objectName)
        {
            throw new NotImplementedException();
        }

        public Task<S3Object> GetObjectInfo(string objectName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetObjectURL(string objectName, int expiresInSeconds = 600)
        {
            throw new NotImplementedException();
        }

        public Task RemoveBucket(string bucketName)
        {
            throw new NotImplementedException();
        }

        public Task RemoveObject(string objectName)
        {
            throw new NotImplementedException();
        }

        public Task<string> SaveObject(string objectName, byte[] objectData, string contentType = null, Dictionary<string, string> metaData = null, string bucketName = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> SavePDF(string objectName, byte[] objectData, string fileName = null, string bucketName = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> SaveText(string objectName, string content, string fileName = null, string fileExtension = "txt", string bucketName = null)
        {
            throw new NotImplementedException();
        }
    }
}
