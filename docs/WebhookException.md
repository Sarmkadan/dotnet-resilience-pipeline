# WebhookException

Exception thrown when a webhook operation fails, providing details about the webhook, the event, and the failure context.

## API

### Properties

#### `WebhookId`
- **Purpose**: Gets the identifier of the webhook that failed.
- **Type**: `string?`
- **Remarks**: May be `null` if the webhook ID is not available in the failure context.

#### `WebhookUrl`
- **Purpose**: Gets the URL of the webhook that failed.
- **Type**: `string?`
- **Remarks**: May be `null` if the webhook URL is not available in the failure context.

#### `AttemptCount`
- **Purpose**: Gets the number of delivery attempts made before the failure.
- **Type**: `int`
- **Remarks**: Always a non-negative integer representing the total attempts.

#### `EventType`
- **Purpose**: Gets the type of event that triggered the webhook.
- **Type**: `string?`
- **Remarks**: May be `null` if the event type is not available in the failure context.

### Exception Types

#### `WebhookDeliveryFailedException`
- **Purpose**: Exception thrown when a webhook delivery fails after one or more attempts.
- **Inheritance**: Derived from `WebhookException`.
- **Remarks**: Includes details about the failure reason and retry attempts.

#### `WebhookRegistrationException`
- **Purpose**: Exception thrown when a webhook registration operation fails.
- **Inheritance**: Derived from `WebhookException`.
- **Remarks**: Typically indicates a problem with the webhook configuration or registration process.

#### `InvalidWebhookException`
- **Purpose**: Exception thrown when a webhook is invalid or malformed.
- **Inheritance**: Derived from `WebhookException`.
- **Remarks**: Indicates that the webhook itself is not valid for the intended operation.

## Usage

### Handling a webhook delivery failure
