using System;
using DotNetResiliencePipeline.Exceptions;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    public class ConfigurationExceptionTests
    {
        [Fact]
        public void Constructor_SetsMessageAndDefaultKey()
        {
            // Arrange
            var message = "Config error";

            // Act
            var ex = new ConfigurationException(message);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal("", ex.ConfigurationKey);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Constructor_SetsMessageAndConfigurationKey()
        {
            // Arrange
            var message = "Config error";
            var key = "RetryPolicy.MaxRetries";

            // Act
            var ex = new ConfigurationException(message, key);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal(key, ex.ConfigurationKey);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Constructor_WithInnerException_SetsProperties()
        {
            // Arrange
            var message = "Config error";
            var inner = new InvalidOperationException("Inner");
            var key = "CircuitBreaker.Threshold";

            // Act
            var ex = new ConfigurationException(message, inner, key);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal(key, ex.ConfigurationKey);
            Assert.Equal(inner, ex.InnerException);
        }

        [Fact]
        public void Constructor_WithInnerException_UsesDefaultKey()
        {
            // Arrange
            var message = "Config error";
            var inner = new InvalidOperationException("Inner");

            // Act
            var ex = new ConfigurationException(message, inner);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal("", ex.ConfigurationKey);
            Assert.Equal(inner, ex.InnerException);
        }

        [Fact]
        public void ConfigurationKey_IsSettable()
        {
            // Arrange
            var ex = new ConfigurationException("msg", "InitialKey");

            // Act
            ex.ConfigurationKey = "UpdatedKey";

            // Assert
            Assert.Equal("UpdatedKey", ex.ConfigurationKey);
        }

        [Fact]
        public void ConfigurationException_InheritsFromDotnetResiliencePipelineException()
        {
            // Arrange & Act
            var ex = new ConfigurationException("msg");

            // Assert
            Assert.IsAssignableFrom<DotnetResiliencePipelineException>(ex);
        }
    }
}
