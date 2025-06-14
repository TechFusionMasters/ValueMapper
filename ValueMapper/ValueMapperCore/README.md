# ValueMapper

A high-performance, lightweight object-to-object mapper for .NET with zero dependencies. ValueMapper provides fast mapping capabilities with a simple API, making it easy to map between different object types while maintaining good performance.

## Feature Comparison

| Feature                      | ValueMapper         | Mapster       | AutoMapper     |
| ---------------------------- | ------------------- | ------------- | -------------- |
| Zero Dependencies            | ✅                  | ✅            | ❌             |
| Basic Property Mapping       | ✅                  | ✅            | ✅             |
| Flattening                   | ❌                  | ✅            | ✅             |
| Deep Object Mapping          | ❌                  | ✅            | ✅             |
| Collection Mapping           | ✅                  | ✅            | ✅             |
| Enum Mapping                 | ✅                  | ✅            | ✅             |
| Custom Property Mapping      | ✅ (attr)           | ✅            | ✅             |
| Property Ignoring            | ✅                  | ✅            | ✅             |
| Type Conversion              | ✅                  | ✅            | ✅             |
| Nullable Handling            | ✅                  | ✅            | ✅             |
| Configuration API            | ❌                  | ✅            | ✅             |
| Custom Value Resolvers       | ❌                  | ✅            | ✅             |
| Conditional Mapping          | ❌                  | ✅            | ✅             |
| Circular Reference Handling  | ❌                  | ✅            | ✅             |
| Before/After Mapping Actions | ❌                  | ✅            | ✅             |
| Runtime Configuration        | ❌                  | ✅            | ✅             |
| Mapping Validation           | ❌                  | ✅            | ✅             |
| Collection Type Conversion   | ❌ (List→List only) | ✅            | ✅             |
| Parallel Collection Mapping  | ✅ (built-in)       | ❌+           | ❌+            |
| Compile-time Type Safety     | ✅                  | ❌‡           | ❌‡            |
| Mapping Cache                | ✅                  | ✅            | ✅             |
| Performance (vs Manual)\*    | ~11.95x slower      | ~8.11x slower | ~12.67x slower |

\* Based on benchmark results for single object mapping. For collection mapping (100,000 items), ValueMapper performs better: ValueMapper (39.84ms), Mapster (65.34ms), AutoMapper (70.80ms).

- Can be implemented manually

## Features

- ✨ Zero dependencies
- 🚀 High performance
- 💡 Simple API
- 🔄 Automatic type conversion
- 🏷️ Custom property mapping via attributes
- ⏭️ Property ignoring
- 📝 Collection mapping
- 🔄 Enum mapping (case-insensitive)
- 🧵 Parallel collection mapping for large datasets
- 🔒 Thread-safe operation
- 🔥 Mapping compilation caching

## Installation

```shell
dotnet add package ValueMapper
```

## Usage Examples

### Basic Property Mapping

```csharp
var source = new Source
{
    Name = "John Doe",
    Age = 30,
    City = "New York"
};

var destination = ValueMapper.Map<Source, Destination>(source);
```

### Custom Property Mapping Using Attributes

```csharp
public class Source
{
    [ValueMapperMapping("CustomName")]
    public string SourceProperty { get; set; }
}

public class Destination
{
    public string CustomName { get; set; }
}

var source = new Source { SourceProperty = "Custom Value" };
var destination = ValueMapper.Map<Source, Destination>(source);
// destination.CustomName will contain "Custom Value"
```

### Ignoring Properties

```csharp
public class Source
{
    public string Name { get; set; }
    [ValueMapperIgnore]
    public string IgnoredProperty { get; set; }
}

// Or ignore properties at runtime
var ignoredProperties = new HashSet<string> { "Age" };
var destination = ValueMapper.Map<Source, Destination>(source, ignoredProperties);
```

### Collection Mapping

```csharp
var sourceList = new List<Source>
{
    new Source { Name = "John", Age = 30 },
    new Source { Name = "Jane", Age = 25 }
};

var destinationList = ValueMapper.MapList<Source, Destination>(sourceList);
```

### Enum Mapping (Case-Insensitive)

```csharp
public enum SampleEnum
{
    None,
    Value1,
    Value2
}

public class Source
{
    public string EnumValue { get; set; }  // Can be "Value1" or "value1"
}

public class Destination
{
    public SampleEnum EnumValue { get; set; }
}
```

### Type Conversion Support

```csharp
public class Source
{
    public int IntValue { get; set; }
    public double DoubleValue { get; set; }
    public string StringNumber { get; set; }
}

public class Destination
{
    public long LongValue { get; set; }      // Converts from int
    public float FloatValue { get; set; }    // Converts from double
    public int IntFromString { get; set; }   // Converts from string
}
```

### Nullable Handling

```csharp
public class Source
{
    public string Value { get; set; }  // Can be null
}

public class Destination
{
    public int? NullableValue { get; set; }  // Will be null if source is null
}
```

## Performance

ValueMapper is designed for high performance. Here are some benchmark results comparing it with other popular mappers:

### Performance Comparison

#### Single Object Mapping (Relative to Manual Implementation)

| Mapper              | Performance | Relative Slowdown |
| ------------------- | ----------- | ----------------- |
| Manual (baseline)   | 0.000ms     | 1x                |
| ValueMapper         | 0.001ms     | 11.95x slower     |
| AutoMapper          | 0.002ms     | 12.67x slower     |
| Mapster             | 0.001ms     | 8.11x slower      |
| ManuallyImplemented | 0.001ms     | 7.29x slower      |

#### Collection Mapping (100,000 items)

| Mapper                        | Time per Operation |
| ----------------------------- | ------------------ |
| ValueMapperCollection         | 39.840ms           |
| AutoMapperCollection          | 70.800ms           |
| MapsterCollection             | 65.340ms           |
| ManuallyImplementedCollection | 45.310ms           |

#### Warmup Performance (First-time Use)

| Mapper              | Warmup Time      |
| ------------------- | ---------------- |
| ValueMapper         | 0ms              |
| AutoMapper          | 7ms              |
| Mapster             | 10ms             |
| ManuallyImplemented | No warmup needed |

### Key Performance Insights:

1. **Single Object Mapping**:

   - All mappers show minimal overhead for single object mapping
   - Mapster shows the best performance among the automated mappers
   - ValueMapper performs similarly to other popular mappers

2. **Collection Mapping**:

   - ValueMapper shows strong performance with large collections
   - ~43% faster than Mapster
   - ~77% faster than AutoMapper
   - Close to manually implemented mapping performance

3. **Warmup Time**:
   - ValueMapper has excellent cold start performance
   - No significant warmup overhead
   - Better startup time compared to both Mapster and AutoMapper

Run the benchmarks yourself:

```shell
cd ValueMapper/Benchmark
dotnet run         # Full benchmarks
dotnet run quick   # Quick benchmarks
```

## Cache Management

```csharp
// Clear all caches
ValueMapper.ClearCaches();

// Pre-warm mapping for specific types
ValueMapper.PreWarmMapping<Source, Destination>();

// Clear cache for specific types
ValueMapper.Clear<Source, Destination>();
```

## Known Limitations

1. Deep object mapping is not currently supported

   - The `Map_DeepObjectMapping_ShouldMapCorrectly` test demonstrates this limitation
   - Complex nested objects with multiple levels are not automatically mapped

2. No circular reference detection
3. No support for mapping private properties
4. Collection type conversion (e.g., List<T> to Array<T>) is not supported
5. No support for custom value converters
6. No support for conditional mapping

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

```

```
