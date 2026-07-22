using System;
using DotNetResiliencePipeline.Exceptions;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    public class ResiliencyExceptionExtensionsTests
    {
        [Fact]
        public void ToDetailedErrorMessage_HappyPath_ReturnsDetailedErrorMessage()
        {
            // Arrange
            var exception = new ResiliencyException("Test message");

            // Act
            var result = ResiliencyExceptionExtensions.ToDetailedErrorMessage(exception);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Resilience Pipeline Error Report", result);
        }

        [Fact]
        public void ToDetailedErrorMessage_NullException_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ResiliencyExceptionExtensions.ToDetailedErrorMessage(null));
        }

        [Fact]
        public void IsRetryable_HappyPath_ReturnsTrueForRetryableExceptions()
        {
            // Arrange
            var exception = new ResiliencyException("Test message");

            // Act
            var result = ResiliencyExceptionExtensions.IsRetryable(exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRetryable_NullException_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ResiliencyExceptionExtensions.IsRetryable(null));
        }

        [Fact]
        public void GetFriendlyName_HappyPath_ReturnsFriendlyName()
        {
            // Arrange
            var exception = new ResiliencyException("Test message");

            // Act
            var result = ResiliencyExceptionExtensions.GetFriendlyName(exception);

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GetFriendlyName_NullException_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ResiliencyExceptionExtensions.GetFriendlyName(null));
        }

        [Fact]
        public void GetSeverityLevel_HappyPath_ReturnsSeverityLevel()
        {
            // Arrange
            var exception = new ResiliencyException("Test message");

            // Act
            var result = ResiliencyExceptionExtensions.GetSeverityLevel(exception);

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GetSeverityLevel_NullException_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ResiliencyExceptionExtensions.GetSeverityLevel(null));
        }
    }
}
