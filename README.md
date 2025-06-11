# SimpleMapper

SimpleMapper is a straightforward, easy-to-use object-to-object mapping utility for .NET. It provides a convenient way to map properties between different object types with minimal configuration, making it suitable for simple mapping scenarios.

## Features

- 💡 Simple property matching by name
- 🔄 Basic type conversion for compatible types
- 🎯 Custom property mapping using attributes
- 🚫 Property exclusion support
- 📝 Support for basic type conversions including enums
- 🔄 Collection mapping support

## When to Use SimpleMapper

SimpleMapper is ideal for:

- Quick property-to-property mapping between DTOs and entities
- Projects that need basic object mapping without complex configurations
- Learning and understanding how object mapping works
- Simple applications where mapping performance is not critical

For complex mapping scenarios or high-performance requirements, consider using more feature-rich libraries like AutoMapper, Mapster.

## Installation

Add the SimpleMapper project to your solution or install via NuGet:

```shell
dotnet add package SimpleMapper
```

## Usage

### Basic Mapping

```csharp
using SimpleMapperUtility;

// Define your source and destination classes
public class UserDto
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

public class UserEntity
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

// Perform the mapping
var dto = new UserDto { Name = "John Doe", Age = 30, Email = "john@example.com" };
var entity = SimpleMapper.Map<UserDto, UserEntity>(dto);
```

### Mapping Collections

```csharp
var dtoList = new List<UserDto>
{
    new UserDto { Name = "John Doe", Age = 30 },
    new UserDto { Name = "Jane Doe", Age = 28 }
};

// Map a list of objects
var entityList = SimpleMapper.MapList<UserDto, UserEntity>(dtoList);
```

### Custom Property Mapping

Use the `SimpleMapperMappingAttribute` to map properties with different names:

```csharp
public class Source
{
    public string FullName { get; set; }
}

public class Destination
{
    [SimpleMapperMapping("FullName")]
    public string Name { get; set; }
}
```

### Ignoring Properties

Use the `SimpleMapperIgnoreAttribute` to exclude properties from mapping:

```csharp
public class UserEntity
{
    public string Name { get; set; }

    [SimpleMapperIgnore]
    public string InternalId { get; set; }
}
```

### Ignoring Properties at Runtime

You can also ignore properties at runtime by passing a set of property names:

```csharp
var ignoredProperties = new HashSet<string> { "Email", "PhoneNumber" };
var entity = SimpleMapper.Map<UserDto, UserEntity>(dto, ignoredProperties);
```

### Type Conversion Support

SimpleMapper handles basic conversions between compatible types:

- Numeric type conversions (int, long, double, decimal, etc.)
- String to/from numeric types
- String to/from enum types
- Enum to/from numeric types
- Nullable type handling

### Cache Management

SimpleMapper maintains a simple cache of mapping functions. If needed, you can manually clear the cache:

```csharp
SimpleMapper.ClearCaches();
```

## Features in Detail

### Automatic Type Conversion

SimpleMapper supports basic type conversions:

- Between numeric types (int, long, float, double, decimal)
- String to/from numeric types
- String to/from enum types
- Enum to/from numeric types
- Handling of nullable value types

### Collection Mapping

For collections, SimpleMapper provides a simple `MapList` method that processes items sequentially or in parallel based on collection size.

## Best Practices

1. **Property Naming**: Use consistent property names between source and destination classes when possible
2. **Type Compatibility**: Ensure property types are compatible for automatic conversion
3. **Simple Mappings**: Use SimpleMapper for straightforward property-to-property mapping scenarios
4. **Complex Scenarios**: Consider using more feature-rich mapping libraries for complex mapping requirements

## Limitations

- Does not support deep object mapping (nested objects must be mapped separately)
- Collection properties must be mapped manually
- Only public properties are mapped
- Properties must have both getter and setter to be mapped
- Custom type conversions are not supported
- Basic caching implementation
- Not optimized for high-performance scenarios

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
