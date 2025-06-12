# ValueMapper

A high-performance, lightweight object-to-object mapper for .NET with zero dependencies. ValueMapper provides fast mapping capabilities with a simple API, making it easy to map between different object types while maintaining good performance.

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

- Single object mapping
- Collection mapping (100,000 items)
- Parallel collection mapping
- Cold start vs. Cached performance

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
