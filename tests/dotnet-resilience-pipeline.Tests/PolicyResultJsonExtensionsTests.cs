using Xunit;
using System.Text.Json;
using DotNetResiliencePipeline.Domain;

namespace DotNetResiliencePipeline.Tests
{
    public class PolicyResultJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var result = PolicyResult.Success("my-policy", 42, attempts: 1);

            // Act
            var json = PolicyResultJsonExtensions.ToJson(result);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("my-policy", json);
        }

        [Fact]
        public void ToJson_Indented_ContainsNewlines()
        {
            // Arrange
            var result = PolicyResult.Success("my-policy", 42);

            // Act
            var json = PolicyResultJsonExtensions.ToJson(result, indented: true);

            // Assert
            Assert.Contains("\n", json);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => PolicyResultJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_HappyPath_RoundTripsPolicyResult()
        {
            // Arrange
            var result = PolicyResult.Success("my-policy", 42, attempts: 2);
            var json = PolicyResultJsonExtensions.ToJson(result);

            // Act
            var deserialized = PolicyResultJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.True(deserialized.IsSuccess);
            Assert.Equal("my-policy", deserialized.PolicyName);
            Assert.Equal(42, deserialized.ExecutionTimeMs);
            Assert.Equal(2, deserialized.AttemptCount);
        }

        [Fact]
        public void FromJson_Whitespace_ReturnsNull()
        {
            // Act
            var result = PolicyResultJsonExtensions.FromJson("   ");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => PolicyResultJsonExtensions.FromJson(null));
        }

        [Fact]
        public void FromJson_InvalidJson_ThrowsJsonException()
        {
            // Act and Assert
            Assert.Throws<JsonException>(() => PolicyResultJsonExtensions.FromJson("not json"));
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrueAndValue()
        {
            // Arrange
            var result = PolicyResult.Failure(new InvalidOperationException("boom"), "fail-policy", 100);
            var json = PolicyResultJsonExtensions.ToJson(result);

            // Act
            var success = PolicyResultJsonExtensions.TryFromJson(json, out var deserialized);

            // Assert
            Assert.True(success);
            Assert.NotNull(deserialized);
            Assert.False(deserialized.IsSuccess);
            Assert.Equal("fail-policy", deserialized.PolicyName);
        }

        [Fact]
        public void TryFromJson_Whitespace_ReturnsTrueWithNullValue()
        {
            // Act
            var success = PolicyResultJsonExtensions.TryFromJson("   ", out var value);

            // Assert
            Assert.True(success);
            Assert.Null(value);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalse()
        {
            // Act
            var success = PolicyResultJsonExtensions.TryFromJson("not json", out var value);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }

        [Fact]
        public void TryFromJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => PolicyResultJsonExtensions.TryFromJson(null, out _));
        }
    }
}