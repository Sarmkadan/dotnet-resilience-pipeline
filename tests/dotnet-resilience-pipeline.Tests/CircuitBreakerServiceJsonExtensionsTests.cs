using System;
using DotNetResiliencePipeline.Services;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    public class CircuitBreakerServiceJsonExtensionsTests
    {
        [Fact]
        public void ToJson_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            CircuitBreakerService? service = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service!.ToJson());
        }

        [Fact]
        public void ToJson_ValidService_ReturnsNonEmptyJson()
        {
            // Arrange
            var service = new CircuitBreakerService();

            // Act
            string json = service.ToJson();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(json));
            // The JSON should contain at least the opening brace
            Assert.StartsWith("{", json);
        }

        [Fact]
        public void FromJson_EmptyOrWhiteSpace_ReturnsNull()
        {
            // Arrange
            string empty = "";
            string whitespace = "   ";

            // Act
            var resultEmpty = CircuitBreakerServiceJsonExtensions.FromJson(empty);
            var resultWhite = CircuitBreakerServiceJsonExtensions.FromJson(whitespace);

            // Assert
            Assert.Null(resultEmpty);
            Assert.Null(resultWhite);
        }

        [Fact]
        public void FromJson_ValidJson_ReturnsInstance()
        {
            // Arrange
            var original = new CircuitBreakerService();
            string json = original.ToJson(indented: true);

            // Act
            var deserialized = CircuitBreakerServiceJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.IsType<CircuitBreakerService>(deserialized);
        }

        [Fact]
        public void TryFromJson_NullJson_ThrowsArgumentNullException()
        {
            // Arrange
            string? json = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CircuitBreakerServiceJsonExtensions.TryFromJson(json!, out _));
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            string invalidJson = "{ this is not valid json }";

            // Act
            bool success = CircuitBreakerServiceJsonExtensions.TryFromJson(invalidJson, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrueAndInstance()
        {
            // Arrange
            var original = new CircuitBreakerService();
            string json = original.ToJson();

            // Act
            bool success = CircuitBreakerServiceJsonExtensions.TryFromJson(json, out var result);

            // Assert
            Assert.True(success);
            Assert.NotNull(result);
            Assert.IsType<CircuitBreakerService>(result);
        }

        [Fact]
        public void ToJson_IndentedFlag_RespectsWriteIndentedSetting()
        {
            // Arrange
            var service = new CircuitBreakerService();

            // Act
            string jsonIndented = service.ToJson(indented: true);
            string jsonCompact = service.ToJson(indented: false);

            // Assert
            Assert.NotEqual(jsonIndented, jsonCompact);
            // Indented JSON should contain line breaks
            Assert.Contains(Environment.NewLine, jsonIndented);
            // Compact JSON should not contain line breaks
            Assert.DoesNotContain(Environment.NewLine, jsonCompact);
        }
    }
}
