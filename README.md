# ValueMapper

A high-performance, lightweight object-to-object mapper for .NET with zero dependencies. ValueMapper provides fast mapping capabilities with a simple API, making it easy to map between different object types while maintaining good performance.

## Feature Comparison

| Feature                      | ValueMapper         | Mapster       | AutoMapper     |
| ---------------------------- | ------------------- | ------------- | -------------- |
| Zero Dependencies            | ✅                  | ✅            | ❌             |
| Basic Property Mapping       | ✅                  | ✅            | ✅             |
| Flattening                   | ❌                  | ✅            | ✅             |
| Deep Object Mapping          | ✅                  | ✅            | ✅             |
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

### Deep Object Mapping

ValueMapper supports mapping nested objects with multiple levels of depth, including collections and dictionaries:

```csharp
public class DeepSourceObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DeepSourceChild Child { get; set; }
}

public class DeepSourceChild
{
    public int ChildId { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public List<string> Tags { get; set; }
    public DeepSourceGrandChild GrandChild { get; set; }
}

public class DeepSourceGrandChild
{
    public int GrandChildId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

// Destination classes with matching structure
public class DeepDestinationObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DeepDestinationChild Child { get; set; }
}

// ... similar structure for DeepDestinationChild and DeepDestinationGrandChild

var source = new DeepSourceObject
{
    Id = 1,
    Name = "Root Object",
    Child = new DeepSourceChild
    {
        ChildId = 100,
        Description = "Child Object",
        Metadata = new Dictionary<string, string> { { "key1", "value1" } },
        Tags = new List<string> { "tag1", "tag2" },
        GrandChild = new DeepSourceGrandChild
        {
            GrandChildId = 1000,
            IsActive = true,
            CreatedDate = new DateTime(2024, 1, 1)
        }
    }
};

var destination = ValueMapper.Map<DeepSourceObject, DeepDestinationObject>(source);
// All nested objects are automatically mapped
```

**Collection Mapping Behavior:**
- **Lists**: Creates new List instances (shallow copy - same objects, new container)
  - Null sources become empty lists `[]`
- **Dictionaries**: Copies references (same instance)
  - Null sources remain null
- **Complex Objects**: Deep mapping with new instances
- **Primitive Types**: Direct value copying

## Performance

ValueMapper is designed for high performance. Here are some benchmark results comparing it with other popular mappers:

### Performance Comparison

#### Single Object Mapping

| Mapper                | Mean Time (ns) | Allocated (B) | Relative Speed |
| --------------------- | -------------- | ------------- | -------------- |
| ManuallyImplemented   | **10.70 ns**   | 72 B          | 1x (baseline)  |
| Manual                | 11.35 ns       | 72 B          | 1.06x          |
| Mapster               | 49.65 ns       | 120 B         | 4.6x slower    |
| AutoMapper            | 92.11 ns       | 120 B         | 8.1x slower    |
| ValueMapper           | 108.08 ns      | 72 B          | 9.6x slower    |

#### Collection Mapping (100,000 items)

| Mapper Collection     | Mean Time (ms) | Allocated (MB) | Relative Speed |
| --------------------- | -------------- | -------------- | -------------- |
| ManuallyImplemented   | **18.86 ms**   | 8 MB           | 1x (baseline)  |
| ValueMapper           | 20.43 ms       | 9.6 MB         | 1.08x slower   |
| AutoMapper            | 28.49 ms       | 13.3 MB        | 1.51x slower   |
| Mapster               | 30.66 ms       | 12 MB          | 1.63x slower   |

#### Warmup Performance (First-time Use)

| Mapper Warmup | Mean Time     | Notes       |
| ------------- | ------------- | ----------- |
| ValueMapper   | **615.91 ns** | Fastest     |
| Mapster       | 720.70 µs     | Slower      |
| AutoMapper    | 1.398 ms      | Much slower |

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

1. No circular reference detection
2. No support for mapping private properties
3. Collection type conversion (e.g., List<T> to Array<T>) is not supported
4. No support for custom value converters
5. No support for conditional mapping

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🧑‍💻 Author

Created and maintained by **Jeevanandan J**

🔗 [LinkedIn](https://www.linkedin.com/in/jeevanandan-j-07b43b91/)  
📦 [GitHub](https://github.com/jeevasusej)  
📧 [jeevasusej@outlook.com](mailto:jeevasusej@outlook.com)


