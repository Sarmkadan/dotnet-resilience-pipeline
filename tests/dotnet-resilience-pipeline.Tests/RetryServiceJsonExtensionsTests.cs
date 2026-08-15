using Xunit;
using System.Text.Json;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Tests
{
    public class RetryServiceJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var retryService = new RetryService();

            // Act
            var json = RetryServiceJsonExtensions.ToJson(retryService);

            // Assert
            Assert.NotEmpty(json);
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsRetryService()
        {
            // Arrange
            var retryService = new RetryService();
            var json = RetryServiceJsonExtensions.ToJson(retryService);

            // Act
            var deserializedRetryService = RetryServiceJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(deserializedRetryService);
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrueAndRetryService()
        {
            // Arrange
            var retryService = new RetryService();
            var json = RetryServiceJsonExtensions.ToJson(retryService);

            // Act
            var success = RetryServiceJsonExtensions.TryFromJson(json, out var deserializedRetryService);

            // Assert
            Assert.True(success);
            Assert.NotNull(deserializedRetryService);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => RetryServiceJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => RetryServiceJsonExtensions.FromJson(null));
        }

        [Fact]
        public void TryFromJson_NullInput_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => RetryServiceJsonExtensions.TryFromJson(null, out _));
        }

        [Fact]
        public void FromJson_InvalidJson_ThrowsJsonException()
        {
            // Act and Assert
            Assert.Throws<JsonException>(() => RetryServiceJsonExtensions.FromJson("Invalid Json"));
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalse()
        {
            // Act
            var success = RetryServiceJsonExtensions.TryFromJson("Invalid Json", out _);

            // Assert
            Assert.False(success);
        }
    }
}
