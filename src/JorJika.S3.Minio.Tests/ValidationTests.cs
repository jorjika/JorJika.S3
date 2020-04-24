using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace JorJika.S3.Minio.Tests
{
    //document-front-image-510bfb20-b3cf-4fdb-bf02-f00e18640086.jpg
    public class ValidationTests
    {
        [Fact]
        public void ObjectNameShouldBeValid()
        {
            Action act = () => Validation.ValidateObjectName("document-front-image-510bfb20-b3cf-4fdb-bf02-f00e18640086.jpg");
            act.Should().NotThrow();
        }
    }
}
