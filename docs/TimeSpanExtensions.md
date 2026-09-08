# TimeSpanExtensions

Extension methods for `System.TimeSpan` that provide jitter, min/max helpers, and a concise human-readable string representation.

## WithJitter

Applies a random jitter to the time span.

### Signature

```csharp
public static TimeSpan WithJitter(this TimeSpan timeSpan, double factor)
```

### Behavior

- If `factor <= 0`, returns the original `timeSpan` unchanged.
- If `factor > 1`, clamps `factor` to `1.0`.
- Calculates a jitter value in milliseconds: `jitterMs = timeSpan.TotalMilliseconds * factor * rnd`, where `rnd` is a random double in the range `[-1, 1]`.
- Adds the jitter to the original milliseconds, then ensures the result is not negative (if negative, sets to `0`).
- Returns a new `TimeSpan` from the resulting milliseconds.

The jitter is applied symmetrically around zero (both positive and negative adjustments) but the final duration is never negative.

### Usage

```csharp
var original = TimeSpan.FromSeconds(10);
var jittered = original.WithJitter(0.1); // Applies up to ±10% jitter
```

## Min

Returns the smaller of two `TimeSpan` values.

### Signature

```csharp
public static TimeSpan Min(this TimeSpan a, TimeSpan b)
```

### Behavior

Compares two `TimeSpan` values using the `<` operator and returns the lesser one.

### Usage

```csharp
var shorter = TimeSpan.FromMinutes(2).Min(TimeSpan.FromMinutes(5)); // returns 2 minutes
```

## Max

Returns the larger of two `TimeSpan` values.

### Signature

```csharp
public static TimeSpan Max(this TimeSpan a, TimeSpan b)
```

### Behavior

Compares two `TimeSpan` values using the `>` operator and returns the greater one.

### Usage

```csharp
var longer = TimeSpan.FromMinutes(2).Max(TimeSpan.FromMinutes(5)); // returns 5 minutes
```

## ToHumanString

Formats the `TimeSpan` as a concise human-readable string.

### Signature

```csharp
public static string ToHumanString(this TimeSpan ts)
```

### Behavior

- Builds a string by appending non-zero components in order: days (`d`), hours (`h`), minutes (`m`), seconds (`s`).
- If the total duration is less than one second, appends milliseconds (`ms`).
- If all components are zero, returns `"0s"`.
- Components are separated by a single space.

### Usage

```csharp
var ts = TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(2)).Add(TimeSpan.FromSeconds(3));
Console.WriteLine(ts.ToHumanString()); // Output: "1h 2m 3s"

var shortTs = TimeSpan.FromMilliseconds(150);
Console.WriteLine(shortTs.ToHumanString()); // Output: "150ms"

var zeroTs = TimeSpan.Zero;
Console.WriteLine(zeroTs.ToHumanString()); // Output: "0s"
```