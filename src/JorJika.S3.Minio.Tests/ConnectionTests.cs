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
using System.IO;
using Minio.DataModel;
using System.Collections.Generic;

namespace JorJika.S3.Minio.Tests
{
    public class ConnectionTests
    {
        #region Mocked Objects

        private Mock<ILogger<IS3Client>> _loggerMock;
        private Mock<IObjectOperations> _objectOperationsClientMock;
        private Mock<IBucketOperations> _bucketOperationsClientMock;
        private readonly IS3Client _s3Client;


        #endregion

        public ConnectionTests()
        {
            _loggerMock = new Mock<ILogger<IS3Client>>();

            _bucketOperationsClientMock = new Mock<IBucketOperations>();
            _bucketOperationsClientMock.Setup(c => c.BucketExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InternalClientException("Connection refused"));
            _bucketOperationsClientMock.Setup(c => c.RemoveBucketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InternalClientException("Connection refused"));

            _objectOperationsClientMock = new Mock<IObjectOperations>();

            _objectOperationsClientMock.Setup(c => c.StatObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ServerSideEncryption>(), It.IsAny<CancellationToken>()))
                                       .ThrowsAsync(new InternalClientException("Connection refused"));

            _objectOperationsClientMock.Setup(c => c.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<ServerSideEncryption>(), It.IsAny<CancellationToken>()))
                                       .ThrowsAsync(new InternalClientException("Connection refused"));

            _objectOperationsClientMock.Setup(c => c.PresignedGetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime?>()))
                                       .ThrowsAsync(new InternalClientException("Connection refused"));

            _objectOperationsClientMock.Setup(c => c.PresignedGetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<DateTime?>()))
                                       .ThrowsAsync(new InternalClientException("Connection refused"));

            _objectOperationsClientMock.Setup(c => c.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<ServerSideEncryption>(), It.IsAny<CancellationToken>()))
                                       .ThrowsAsync(new InternalClientException("Connection refused"));

            _objectOperationsClientMock.Setup(c => c.RemoveObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                       .ThrowsAsync(new InternalClientException("Connection refused"));

            _s3Client = new MinioS3Client(_bucketOperationsClientMock.Object, _objectOperationsClientMock.Object, "127.0.0.1:1", "test", _loggerMock.Object, 0, 0);
        }


        [Fact]
        public void CreateBucket_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.CreateBucket("test");
            act.Should().Throw<EndpointUnreachableException>();
        }

        [Fact]
        public void RemoveBucket_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.RemoveBucket("test");
            act.Should().Throw<EndpointUnreachableException>();
        }

        [Fact]
        public void GetObjectInfo_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.GetObjectInfo("test");
            act.Should().Throw<EndpointUnreachableException>();
        }

        [Fact]
        public void GetObject_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.GetObject("test");
            act.Should().Throw<EndpointUnreachableException>();
        }

        [Fact]
        public void GetObjectURL_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.GetObjectURL("test");
            act.Should().Throw<EndpointUnreachableException>();
        }

        [Fact]
        public void SaveObject_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.SaveObject("test", new byte[1]);
            act.Should().Throw<EndpointUnreachableException>();
        }

        [Fact]
        public void RemoveObject_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.RemoveObject("test");
            act.Should().Throw<EndpointUnreachableException>();
        }

        [Fact]
        public void SaveText_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.SaveText("test", "test");
            act.Should().Throw<EndpointUnreachableException>();
        }
        [Fact]
        public void SavePDF_should_throw_EndpointUnreachableException_when_server_unavailable()
        {
            Func<Task> act = async () => await _s3Client.SavePDF("test", new byte[1]);
            act.Should().Throw<EndpointUnreachableException>();
        }
    }
}
