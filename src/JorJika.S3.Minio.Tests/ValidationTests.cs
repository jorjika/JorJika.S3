using FluentAssertions;
using JorJika.S3.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace JorJika.S3.Minio.Tests
{
    //document-front-image-510bfb20-b3cf-4fdb-bf02-f00e18640086_123.jpg
    public class ValidationTests
    {
        [Fact]
        public void Object_Name_Should_Be_Valid()
        {
            Action act = () => Validation.ValidateObjectName("object-image-name-510bfb20-b3cf-4fdb-bf02-f00e18640086_123.jpg");
            act.Should().NotThrow();
        }

        [Fact]
        public void Object_Name_Should_Throw_ObjectNameIsNotValidException_when_ending_with_lash()
        {
            Action act = () => Validation.ValidateObjectName("object-image-name-510bfb20-b3cf-4fdb-bf02-f00e18640086_123.jpg/");
            act.Should().Throw<ObjectNameIsNotValidException>();
        }

        [Fact]
        public void Object_Name_Should_Throw_ObjectNameIsNotValidException_when_containing_not_allowed_characters()
        {
            Action act = () => Validation.ValidateObjectName("object-image-%@.jpg");
            act.Should().Throw<ObjectNameIsNotValidException>();
        }
    }
}
