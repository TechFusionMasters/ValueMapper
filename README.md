# SimpleMapper

SimpleMapper is a straightforward, easy-to-use object-to-object mapping utility for .NET. It provides a convenient way to map properties between different object types with minimal configuration, making it suitable for simple mapping scenarios.

## Features

- 💡 Simple property matching by name
- 🔄 Basic type conversion for compatible types
- 🎯 Custom property mapping using attributes
- 🚫 Property exclusion support
- 📝 Support for basic type conversions including enums
- 🔄 List mapping with automatic parallelization

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

### Mapping Lists of Objects

SimpleMapper provides efficient list mapping using `MapList`:

```csharp
var dtoList = new List<UserDto>
{
    new UserDto { Name = "John Doe", Age = 30 },
    new UserDto { Name = "Jane Doe", Age = 28 }
};

// Maps each object in the list
var entityList = SimpleMapper.MapList<UserDto, UserEntity>(dtoList);
```

The `MapList` method automatically uses parallelization for lists with more than 100 items, with a maximum degree of parallelism limited to 8 or the number of processor cores, whichever is smaller.

### Custom Property Mapping

Use the `SimpleMapperMappingAttribute` to map properties with different names:

```csharp
// Destination property specifying the source property name
public class Source
{
    public string FullName { get; set; }
}

public class Destination
{
    [SimpleMapperMapping("FullName")]
    public string Name { get; set; }
}

// OR source property specifying the destination property name
public class Source
{
    [SimpleMapperMapping("Name")]
    public string FullName { get; set; }
}

public class Destination
{
    public string Name { get; set; }
}
```

The attribute can be applied to either the source or destination property.

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

SimpleMapper handles various type conversions:

- Numeric type conversions (int, long, double, decimal, etc.)
- String to/from numeric types
- String to/from enum types (with case-insensitive parsing)
- Enum to/from numeric types
- Nullable type handling
- Custom type conversions through the Convert.ChangeType method

If a conversion fails, SimpleMapper will use the default value for the destination type rather than throwing an exception.

Example of enum conversion:

```csharp
public enum UserType { Regular, Admin }

public class UserDto
{
    public string Type { get; set; }  // "Regular" or "Admin"
}

public class UserEntity
{
    public UserType Type { get; set; }  // Enum value
}

// String to enum conversion happens automatically
var dto = new UserDto { Type = "Admin" };
var entity = SimpleMapper.Map<UserDto, UserEntity>(dto);
// entity.Type will be UserType.Admin
```

Example of numeric conversion:

```csharp
public class Source
{
    public int Value { get; set; }
}

public class Destination
{
    public double Value { get; set; }
}

var source = new Source { Value = 42 };
var dest = SimpleMapper.Map<Source, Destination>(source);
// dest.Value will be 42.0
```

### Cache Management

SimpleMapper maintains a cache of mapping functions to improve performance:

- Mapping functions are cached for 1 hour after last access
- Old entries are automatically evicted
- The cache can be manually cleared if needed:

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

### Null Value Handling

- Null values are properly handled during mapping
- If the destination property is a non-nullable value type, null source values are skipped
- For nullable destination types, null source values are mapped as null

## Best Practices

1. **Property Naming**: Use consistent property names between source and destination classes when possible
2. **Type Compatibility**: Ensure property types are compatible for automatic conversion
3. **Simple Mappings**: Use SimpleMapper for straightforward property-to-property mapping scenarios
4. **Complex Scenarios**: Consider using more feature-rich mapping libraries for complex mapping requirements

## Limitations

- Does not support deep object mapping (nested complex objects must be mapped separately)
- Does not support mapping collection properties directly (e.g., mapping a List<string> to another List<string>)
- Only public properties are mapped
- Properties must have both getter and setter to be mapped
- Basic caching implementation with 1-hour TTL
- Not optimized for extremely high-performance scenarios

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
