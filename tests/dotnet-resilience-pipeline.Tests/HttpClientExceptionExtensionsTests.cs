using System;
using DotNetResiliencePipeline.Exceptions;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    public class HttpClientExceptionExtensionsTests
    {
        [Fact]
        public void GetFullErrorMessage_ReturnsEmptyString_WhenExceptionIsNull()
        {
            // Arrange
            var exception = null as HttpClientException;

            // Act
            var result = HttpClientExceptionExtensions.GetFullErrorMessage(exception);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetFullErrorMessage_ReturnsErrorMessage_WhenExceptionHasMessage()
        {
            // Arrange
            var exception = new HttpClientException("Test message");

            // Act
            var result = HttpClientExceptionExtensions.GetFullErrorMessage(exception);

            // Assert
            Assert.Equal("Test message", result);
        }

        [Fact]
        public void GetFullErrorMessage_ReturnsFullErrorMessage_WhenExceptionHasClientName()
        {
            // Arrange
            var exception = new HttpClientException("Test message", "TestClient");

            // Act
            var result = HttpClientExceptionExtensions.GetFullErrorMessage(exception);

            // Assert
            Assert.Equal("Test message | Client: TestClient", result);
        }

        [Fact]
        public void GetFullErrorMessage_ReturnsFullErrorMessage_WhenExceptionHasRequestUrl()
        {
            // Arrange
            var exception = new HttpClientException("Test message", requestUrl: "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.GetFullErrorMessage(exception);

            // Assert
            Assert.Equal("Test message | URL: https://example.com/api", result);
        }

        [Fact]
        public void GetFullErrorMessage_ReturnsFullErrorMessage_WhenExceptionHasHttpMethod()
        {
            // Arrange
            var exception = new InvalidHttpRequestException("Test message", "TestClient", "https://example.com/api", "POST");

            // Act
            var result = HttpClientExceptionExtensions.GetFullErrorMessage(exception);

            // Assert
            Assert.Equal("Test message | Client: TestClient | URL: https://example.com/api | Method: POST", result);
        }

        [Fact]
        public void GetFullErrorMessage_ReturnsFullErrorMessage_WhenExceptionHasStatusCode()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 404, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.GetFullErrorMessage(exception);

            // Assert
            Assert.Equal("Test message | Client: TestClient | URL: https://example.com/api | Status: 404 (NotFound)", result);
        }

        [Fact]
        public void GetFullErrorMessage_ReturnsFullErrorMessage_WhenExceptionHasTimeout()
        {
            // Arrange
            var exception = new HttpTimeoutException("Test message", TimeSpan.FromSeconds(30), "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.GetFullErrorMessage(exception);

            // Assert
            Assert.Equal("Test message | Client: TestClient | URL: https://example.com/api | Timeout: 30s", result);
        }

        [Fact]
        public void IsClientError_ReturnsTrue_WhenExceptionIsClientError()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 400, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.IsClientError(exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsClientError_ReturnsFalse_WhenExceptionIsNotClientError()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 500, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.IsClientError(exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsServerError_ReturnsTrue_WhenExceptionIsServerError()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 500, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.IsServerError(exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsServerError_ReturnsFalse_WhenExceptionIsNotServerError()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 400, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.IsServerError(exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTimeoutError_ReturnsTrue_WhenExceptionIsTimeoutError()
        {
            // Arrange
            var exception = new HttpTimeoutException("Test message", TimeSpan.FromSeconds(30), "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.IsTimeoutError(exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTimeoutError_ReturnsFalse_WhenExceptionIsNotTimeoutError()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 400, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.IsTimeoutError(exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetErrorCode_ReturnsErrorCode_WhenExceptionIsTimeoutError()
        {
            // Arrange
            var exception = new HttpTimeoutException("Test message", TimeSpan.FromSeconds(30), "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.GetErrorCode(exception);

            // Assert
            Assert.Equal("HTTP_TIMEOUT", result);
        }

        [Fact]
        public void GetErrorCode_ReturnsErrorCode_WhenExceptionIsClientError()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 400, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.GetErrorCode(exception);

            // Assert
            Assert.Equal($"HTTP_{exception.GetType().Name[4..^9]}", result);
        }

        [Fact]
        public void GetErrorCode_ReturnsErrorCode_WhenExceptionIsServerError()
        {
            // Arrange
            var exception = new HttpResponseException("Test message", 500, "TestClient", "https://example.com/api");

            // Act
            var result = HttpClientExceptionExtensions.GetErrorCode(exception);

            // Assert
            Assert.Equal($"HTTP_{exception.GetType().Name[4..^9]}", result);
        }

        [Fact]
        public void GetErrorCode_ReturnsErrorCode_WhenExceptionIsInvalidRequest()
        {
            // Arrange
            var exception = new InvalidHttpRequestException("Test message", "TestClient", "https://example.com/api", "POST");

            // Act
            var result = HttpClientExceptionExtensions.GetErrorCode(exception);

            // Assert
            Assert.Equal("HTTP_INVALID_REQUEST", result);
        }

        [Fact]
        public void GetErrorCode_ReturnsDefaultErrorCode_WhenExceptionIsUnknown()
        {
            // Arrange
            var exception = new HttpClientException("Test message");

            // Act
            var result = HttpClientExceptionExtensions.GetErrorCode(exception);

            // Assert
            Assert.Equal("HTTP_CLIENT_ERROR", result);
        }
    }
}
