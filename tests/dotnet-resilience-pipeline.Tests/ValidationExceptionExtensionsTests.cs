using System;
using System.Collections.Generic;
using DotNetResiliencePipeline.Exceptions;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    public class ValidationExceptionExtensionsTests
    {
        private readonly ValidationException _exception;
        private const string FieldName = "TestField";
        private const string ErrorMessage = "Test Error";

        public ValidationExceptionExtensionsTests()
        {
            _exception = new ValidationException("Error");
            _exception.ValidationErrors.Add(FieldName, ErrorMessage);
        }

        [Fact]
        public void HasErrorFor_NullException_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ValidationException)null!).HasErrorFor(FieldName));
        }

        [Theory]
        [InlineData(null)]
        public void HasErrorFor_NullFieldName_ThrowsArgumentNullException(string fieldName)
        {
            Assert.Throws<ArgumentNullException>(() => _exception.HasErrorFor(fieldName!));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void HasErrorFor_InvalidFieldName_ThrowsArgumentException(string fieldName)
        {
            Assert.Throws<ArgumentException>(() => _exception.HasErrorFor(fieldName));
        }

        [Fact]
        public void HasErrorFor_ValidField_ReturnsTrue()
        {
            Assert.True(_exception.HasErrorFor(FieldName));
        }

        [Fact]
        public void HasErrorFor_InvalidField_ReturnsFalse()
        {
            Assert.False(_exception.HasErrorFor("NonExistentField"));
        }

        [Fact]
        public void GetErrorMessage_NullException_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ValidationException)null!).GetErrorMessage(FieldName));
        }

        [Theory]
        [InlineData(null)]
        public void GetErrorMessage_NullFieldName_ThrowsArgumentNullException(string fieldName)
        {
            Assert.Throws<ArgumentNullException>(() => _exception.GetErrorMessage(fieldName!));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void GetErrorMessage_InvalidFieldName_ThrowsArgumentException(string fieldName)
        {
            Assert.Throws<ArgumentException>(() => _exception.GetErrorMessage(fieldName));
        }

        [Fact]
        public void GetErrorMessage_ValidField_ReturnsErrorMessage()
        {
            Assert.Equal(ErrorMessage, _exception.GetErrorMessage(FieldName));
        }

        [Fact]
        public void GetErrorMessage_InvalidField_ReturnsNull()
        {
            Assert.Null(_exception.GetErrorMessage("NonExistentField"));
        }

        [Fact]
        public void GetErrorFields_NullException_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ValidationException)null!).GetErrorFields());
        }

        [Fact]
        public void GetErrorFields_ReturnsAllFields()
        {
            _exception.ValidationErrors.Add("AnotherField", "Another Error");
            
            var fields = _exception.GetErrorFields();
            
            var fieldList = new List<string>(fields);
            Assert.Contains(FieldName, fieldList);
            Assert.Contains("AnotherField", fieldList);
            Assert.Equal(2, fieldList.Count);
        }
    }
}
