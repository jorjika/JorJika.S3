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
        public void Object_name_with_score_should_be_valid()
        {
            Action act = () => Validation.ValidateObjectName("object-name");
            act.Should().NotThrow();
        }

        [Fact]
        public void Object_name_with_underscore_should_be_valid()
        {
            Action act = () => Validation.ValidateObjectName("object_name");
            act.Should().NotThrow();
        }

        [Fact]
        public void Object_name_with_dot_should_be_valid()
        {
            Action act = () => Validation.ValidateObjectName("object.name");
            act.Should().NotThrow();
        }

        [Fact]
        public void Object_name_with_slash_should_be_valid()
        {
            Action act = () => Validation.ValidateObjectName("object/name");
            act.Should().NotThrow();
        }
        
        [Fact]
        public void Object_name_with_numbers_should_be_valid()
        {
            Action act = () => Validation.ValidateObjectName("objectname1234");
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

        [Fact]
        public void Object_Id_Should_Throw_ObjectIdIsNotValidException_when_containing_not_allowed_characters()
        {
            Action act = () => BucketHelper.ExtractObjectInfo("object-image-%@.jpg");
            act.Should().Throw<ObjectIdIsNotValidException>();
        }

        [Fact]
        public void bucketName_should_be_null_when_does_not_contain_bucket_folder_at_start()
        {
            var obj = BucketHelper.ExtractObjectInfo("object-name");
            obj.bucketName.Should().Be(null);
        }
        
        [Fact]
        public void bucketName_should_be_bucket1_when_object_id_starts_with_bucket1_and_slash()
        {
            var obj = BucketHelper.ExtractObjectInfo("bucket1/object-name");
            obj.bucketName.Should().Be("bucket1");
        }
    }
}
