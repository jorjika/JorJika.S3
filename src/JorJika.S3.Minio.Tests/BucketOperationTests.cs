using Microsoft.Extensions.Logging;
using Minio;
using Minio.Exceptions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using JorJika.S3.Exceptions;

namespace JorJika.S3.Minio.Tests
{
    public class BucketOperationTests
    {
        #region Mocked Objects

        private Mock<ILogger<IS3Client>> _loggerMock;
        private Mock<IBucketOperations> _bucketOperationsClientMock;
        private readonly IS3Client _s3Client;


        #endregion

        public BucketOperationTests()
        {
            _loggerMock = new Mock<ILogger<IS3Client>>();

            _bucketOperationsClientMock = new Mock<IBucketOperations>();
            _bucketOperationsClientMock.Setup(c => c.BucketExistsAsync("existingbucket", It.IsAny<CancellationToken>())).ReturnsAsync(value: true);
            _bucketOperationsClientMock.Setup(c => c.BucketExistsAsync("nonexistingbucket", It.IsAny<CancellationToken>())).ReturnsAsync(value: false);

            _s3Client = new MinioS3Client(_bucketOperationsClientMock.Object, null, "127.0.0.1:1", "test", _loggerMock.Object, 0, 0);
        }

        [Fact]
        public void CreateBucket_Should_throw_BucketExistsException_when_bucket_exists()
        {
            Func<Task> act = async () => await _s3Client.CreateBucket("existingbucket");
            act.Should().Throw<BucketExistsException>();
        }

        [Fact]
        public void CreateBucket_Should_throw_BucketNameIsNotValidException_when_invalid_name_privided()
        {
            Func<Task> act = async () => await _s3Client.CreateBucket("test@Asd");
            act.Should().Throw<BucketNameIsNotValidException>();
        }

        [Fact]
        public void RemoveBucket_Should_throw_BucketNotFoundException_when_bucket_exists()
        {
            Func<Task> act = async () => await _s3Client.RemoveBucket("nonexistingbucket");
            act.Should().Throw<Exceptions.BucketNotFoundException>();
        }


    }
}
