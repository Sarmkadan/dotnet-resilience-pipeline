using System;
using System.Collections.Generic;
using DotNetResiliencePipeline.Exceptions;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    public class ValidationExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsMessage_And_EmptyValidationErrors()
        {
            // Arrange
            var message = "validation failed";

            // Act
            var ex = new ValidationException(message);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.NotNull(ex.ValidationErrors);
            Assert.Empty(ex.ValidationErrors);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsAllProperties()
        {
            // Arrange
            var message = "validation failed";
            var inner = new InvalidOperationException("inner cause");

            // Act
            var ex = new ValidationException(message, inner);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.NotNull(ex.ValidationErrors);
            Assert.Empty(ex.ValidationErrors);
        }

        [Fact]
        public void Constructor_WithMessageAndErrors_SetsMessage_And_ValidationErrors()
        {
            // Arrange
            var message = "validation failed";
            var errors = new Dictionary<string, string>
            {
                { "Field1", "must not be null" },
                { "Field2", "must be greater than zero" }
            };

            // Act
            var ex = new ValidationException(message, errors);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Same(errors, ex.ValidationErrors);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Constructor_WithMessageInnerExceptionAndErrors_SetsAllProperties()
        {
            // Arrange
            var message = "validation failed";
            var inner = new ArgumentException("bad argument");
            var errors = new Dictionary<string, string>
            {
                { "Param", "invalid value" }
            };

            // Act
            var ex = new ValidationException(message, inner, errors);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Same(errors, ex.ValidationErrors);
        }

        [Fact]
        public void ValidationErrors_Property_IsMutable()
        {
            // Arrange
            var ex = new ValidationException("msg");
            var newErrors = new Dictionary<string, string>
            {
                { "NewKey", "NewValue" }
            };

            // Act
            ex.ValidationErrors = newErrors;

            // Assert
            Assert.Same(newErrors, ex.ValidationErrors);
            Assert.Single(ex.ValidationErrors);
            Assert.Equal("NewValue", ex.ValidationErrors["NewKey"]);
        }

        [Fact]
        public void Constructor_WithEmptyDictionary_ResultsInEmptyValidationErrors()
        {
            // Arrange
            var message = "empty errors";
            var emptyDict = new Dictionary<string, string>();

            // Act
            var ex = new ValidationException(message, emptyDict);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Empty(ex.ValidationErrors);
            // Ensure the same instance is used (as per implementation)
            Assert.Same(emptyDict, ex.ValidationErrors);
        }
    }
}
