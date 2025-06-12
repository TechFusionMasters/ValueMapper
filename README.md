# ValueMapper

ValueMapper is a straightforward, easy-to-use object-to-object mapping utility for .NET. It provides a convenient way to map properties between different object types with minimal configuration.

## Installation

```shell
dotnet add package ValueMapper
```

## Quick Start

```csharp
using ValueMapperUtility;

// Define your classes
public class UserDto
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class UserEntity
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Perform mapping
var dto = new UserDto { Name = "John Doe", Age = 30 };
var entity = ValueMapper.Map<UserDto, UserEntity>(dto);
```

## Features

- Simple property matching by name
- Basic type conversion support
- Custom property mapping using attributes
- Property exclusion support
- Collection mapping
- Enum mapping support

## Documentation

For full documentation, visit our [GitHub repository](https://github.com/TechFusionMasters/ValueMapper).

### Collection Mapping

ValueMapper provides support for collections in two ways:

1. Mapping Lists of Objects using `MapList`:

```csharp
var dtoList = new List<UserDto>
{
    new UserDto { Name = "John Doe", Age = 30 },
    new UserDto { Name = "Jane Doe", Age = 28 }
};

// Maps each object in the list, with automatic parallelization for large lists
var entityList = ValueMapper.MapList<UserDto, UserEntity>(dtoList);
```

2. Mapping Collection Properties:

```csharp
public class UserDto
{
    public string Name { get; set; }
    public List<string> Roles { get; set; }
}

public class UserEntity
{
    public string Name { get; set; }
    public List<string> Roles { get; set; }
}

// Both Name and Roles will be mapped automatically
var dto = new UserDto
{
    Name = "John",
    Roles = new List<string> { "Admin", "User" }
};

var entity = ValueMapper.Map<UserDto, UserEntity>(dto);
```

### Custom Property Mapping

Use the `ValueMapperMappingAttribute` to map properties with different names:

```csharp
public class Source
{
    public string FullName { get; set; }
}

public class Destination
{
    [ValueMapperMapping("FullName")]
    public string Name { get; set; }
}
```

### Ignoring Properties

Use the `ValueMapperIgnoreAttribute` to exclude properties from mapping:

```csharp
public class UserEntity
{
    public string Name { get; set; }

    [ValueMapperIgnore]
    public string InternalId { get; set; }
}
```

### Type Conversion Support

ValueMapper handles various type conversions:

- Numeric type conversions (int, long, double, decimal, etc.)
- String to/from numeric types
- String to/from enum types (with case-insensitive parsing)
- Enum to/from numeric types
- Nullable type handling
- Custom type conversions through the Convert.ChangeType method

## Limitations

- Does not support deep object mapping (nested objects must be mapped separately)
- Only public properties are mapped
- Properties must have both getter and setter to be mapped
- Basic caching implementation
- Not optimized for high-performance scenarios

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
