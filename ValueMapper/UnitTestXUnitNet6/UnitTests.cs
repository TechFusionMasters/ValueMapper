using ValueMapperUtility;
using System;
using System.Collections.Generic;

namespace UnitTestXUnitNet6
{
    public class ValueMapperTests
    {
        [Fact]
        public void Map_BasicProperties_ShouldMapCorrectly()
        {
            // Arrange
            var source = new Source
            {
                Name = "John Doe",
                Age = 30,
                City = "New York"
            };

            // Act
            var destination = ValueMapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(source.Name, destination.Name);
            Assert.Equal(source.Age, destination.Age);
            Assert.Equal(source.City, destination.City);
        }

        [Fact]
        public void Map_NullSource_ShouldThrowArgumentNullException()
        {
            // Arrange
            Source source = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ValueMapper.Map<Source, Destination>(source));
        }

        [Fact]
        public void Map_WithIgnoredProperties_ShouldNotMapIgnoredProperties()
        {
            // Arrange
            var source = new Source
            {
                Name = "John Doe",
                Age = 30,
                City = "New York"
            };

            var ignoredProperties = new HashSet<string> { "Age" };

            // Act
            var destination = ValueMapper.Map<Source, Destination>(source, ignoredProperties);

            // Assert
            Assert.Equal(source.Name, destination.Name);
            Assert.Equal(0, destination.Age); // Default value for int
            Assert.Equal(source.City, destination.City);
        }

        [Fact]
        public void Map_EnumMapping_ShouldMapStringToEnum()
        {
            // Arrange
            var source = new Source
            {
                EnumValue = "Value1"
            };

            // Act
            var destination = ValueMapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(SampleEnum.Value1, destination.EnumValue);
        }

        [Fact]
        public void Map_EnumMapping_ShouldHandleInvalidEnumValue()
        {
            // Arrange
            var source = new Source
            {
                EnumValue = "InvalidValue"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ValueMapper.Map<Source, Destination>(source));
        }

        [Fact]
        public void Map_EnumMapping_ShouldBeCaseInsensitive()
        {
            // Arrange
            var source = new Source
            {
                EnumValue = "value1" // lowercase
            };

            // Act
            var destination = ValueMapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(SampleEnum.Value1, destination.EnumValue);
        }

        [Fact]
        public void Map_CustomMappedProperty_ShouldMapUsingAttribute()
        {
            // Arrange
            var source = new Source
            {
                CustomMappedProperty = "Custom Value"
            };

            // Act
            var destination = ValueMapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(source.CustomMappedProperty, destination.CustomName);
        }

        [Fact]
        public void MapList_BasicList_ShouldMapAllItems()
        {
            // Arrange
            var sourceList = new List<Source>
            {
                new Source { Name = "John", Age = 30, City = "New York" },
                new Source { Name = "Jane", Age = 25, City = "Boston" },
                new Source { Name = "Bob", Age = 40, City = "Chicago" }
            };

            // Act
            var destinationList = ValueMapper.MapList<Source, Destination>(sourceList);

            // Assert
            Assert.Equal(sourceList.Count, destinationList.Count);
            for (int i = 0; i < sourceList.Count; i++)
            {
                Assert.Equal(sourceList[i].Name, destinationList[i].Name);
                Assert.Equal(sourceList[i].Age, destinationList[i].Age);
                Assert.Equal(sourceList[i].City, destinationList[i].City);
            }
        }

        [Fact]
        public void MapList_NullList_ShouldThrowArgumentNullException()
        {
            // Arrange
            List<Source> sourceList = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ValueMapper.MapList<Source, Destination>(sourceList));
        }

        [Fact]
        public void MapList_EmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var sourceList = new List<Source>();

            // Act
            var destinationList = ValueMapper.MapList<Source, Destination>(sourceList);

            // Assert
            Assert.Empty(destinationList);
        }

        [Fact]
        public void MapList_WithIgnoredProperties_ShouldRespectIgnoredProperties()
        {
            // Arrange
            var sourceList = new List<Source>
            {
                new Source { Name = "John", Age = 30, City = "New York" },
                new Source { Name = "Jane", Age = 25, City = "Boston" }
            };

            var ignoredProperties = new HashSet<string> { "City" };

            // Act
            var destinationList = ValueMapper.MapList<Source, Destination>(sourceList, ignoredProperties);

            // Assert
            Assert.Equal(sourceList.Count, destinationList.Count);
            for (int i = 0; i < sourceList.Count; i++)
            {
                Assert.Equal(sourceList[i].Name, destinationList[i].Name);
                Assert.Equal(sourceList[i].Age, destinationList[i].Age);
                Assert.Null(destinationList[i].City); // Default value for string
            }
        }

        [Fact]
        public void ClearCaches_ShouldClearAllCaches()
        {
            // Arrange - Populate caches
            var source = new Source { Name = "Test" };
            var destination = ValueMapper.Map<Source, Destination>(source);

            // Act
            ValueMapper.ClearCaches();

            // Assert - No direct way to verify caches are cleared, but we can ensure mapping still works
            var newDestination = ValueMapper.Map<Source, Destination>(source);
            Assert.Equal(source.Name, newDestination.Name);
        }

        [Fact]
        public void Map_LargeNumberOfObjects_ShouldUseParallelism()
        {
            // Arrange
            var sourceList = new List<Source>();
            for (int i = 0; i < 1000; i++) // Create enough objects to trigger parallelism
            {
                sourceList.Add(new Source { Name = $"Name{i}", Age = i, City = $"City{i}" });
            }

            // Act
            var startTime = DateTime.UtcNow;
            var destinationList = ValueMapper.MapList<Source, Destination>(sourceList);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.Equal(sourceList.Count, destinationList.Count);
            // Note: We can't directly test parallelism, but we can check it completed successfully
            for (int i = 0; i < sourceList.Count; i++)
            {
                Assert.Equal(sourceList[i].Name, destinationList[i].Name);
                Assert.Equal(sourceList[i].Age, destinationList[i].Age);
                Assert.Equal(sourceList[i].City, destinationList[i].City);
            }
        }

        [Fact]
        public void Map_DifferentTypes_ShouldConvertBetweenTypes()
        {
            // Arrange
            var source = new Models
            {
                IntValue = 42,
                DoubleValue = 3.14,
                StringNumber = "123"
            };

            // Act
            var destination = ValueMapper.Map<Models, NumericDestination>(source);

            // Assert
            Assert.Equal((long)source.IntValue, destination.LongValue);
            Assert.Equal((float)source.DoubleValue, destination.FloatValue);
            Assert.Equal(123, destination.IntFromString);
        }

        [Fact]
        public void Map_WithNullableDestination_ShouldHandleNulls()
        {
            // Arrange
            var source = new BasicSource
            {
                Value = null
            };

            // Act
            var destination = ValueMapper.Map<BasicSource, NullableDestination>(source);

            // Assert
            Assert.Null(destination.NullableValue);
        }

        [Fact]
        public void Map_WithNonNullableDestination_ShouldSkipNulls()
        {
            // Arrange
            var source = new BasicSource
            {
                Value = null
            };

            // Act
            var destination = ValueMapper.Map<BasicSource, NonNullableDestination>(source);

            // Assert
            Assert.Equal(0, destination.IntValue); // Default value should remain
        }

        [Fact]
        public void Map_CacheShouldWork_WhenCalledMultipleTimes()
        {
            // Arrange
            var source1 = new Source { Name = "First" };
            var source2 = new Source { Name = "Second" };

            // Act - This should use cache for the second call
            var dest1 = ValueMapper.Map<Source, Destination>(source1);
            var dest2 = ValueMapper.Map<Source, Destination>(source2);

            // Assert
            Assert.Equal(source1.Name, dest1.Name);
            Assert.Equal(source2.Name, dest2.Name);
        }

        [Fact]
        public void Map_ListStringProperties_ShouldMapCorrectly()
        {
            // Arrange
            var source = new UserWithRoles
            {
                Name = "John",
                Roles = new List<string> { "Admin", "User" }
            };

            // Act
            var destination = ValueMapper.Map<UserWithRoles, UserWithRolesDestination>(source);

            // Assert
            Assert.Equal(source.Name, destination.Name);
            Assert.Equal(source.Roles.Count, destination.Roles.Count);
            Assert.Equal(source.Roles[0], destination.Roles[0]);
            Assert.Equal(source.Roles[1], destination.Roles[1]);
        }

        [Fact]
        public void Map_DeepObjectMapping_ShouldMapCorrectly()
        {
            // Arrange
            var source = new DeepSourceObject
            {
                Id = 1,
                Name = "Root Object",
                Child = new DeepSourceChild
                {
                    ChildId = 100,
                    Description = "Child Object",
                    Metadata = new Dictionary<string, string>
                    {
                        { "key1", "value1" },
                        { "key2", "value2" }
                    },
                    Tags = new List<string> { "tag1", "tag2" },
                    GrandChild = new DeepSourceGrandChild
                    {
                        GrandChildId = 1000,
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 1, 1)
                    }
                }
            };

            // Act
            var destination = ValueMapper.Map<DeepSourceObject, DeepDestinationObject>(source);

            // Assert
            // Root level assertions
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.Name, destination.Name);
            
            // Child level assertions
            Assert.NotNull(destination.Child);
            Assert.Equal(source.Child.ChildId, destination.Child.ChildId);
            Assert.Equal(source.Child.Description, destination.Child.Description);
            
            // Child collections assertions
            Assert.NotNull(destination.Child.Metadata);
            Assert.Equal(source.Child.Metadata.Count, destination.Child.Metadata.Count);
            Assert.Equal(source.Child.Metadata["key1"], destination.Child.Metadata["key1"]);
            Assert.Equal(source.Child.Metadata["key2"], destination.Child.Metadata["key2"]);
            
            Assert.NotNull(destination.Child.Tags);
            Assert.Equal(source.Child.Tags.Count, destination.Child.Tags.Count);
            Assert.Equal(source.Child.Tags[0], destination.Child.Tags[0]);
            Assert.Equal(source.Child.Tags[1], destination.Child.Tags[1]);
            
            // GrandChild level assertions
            Assert.NotNull(destination.Child.GrandChild);
            Assert.Equal(source.Child.GrandChild.GrandChildId, destination.Child.GrandChild.GrandChildId);
            Assert.Equal(source.Child.GrandChild.IsActive, destination.Child.GrandChild.IsActive);
            Assert.Equal(source.Child.GrandChild.CreatedDate, destination.Child.GrandChild.CreatedDate);
        }
    }
}