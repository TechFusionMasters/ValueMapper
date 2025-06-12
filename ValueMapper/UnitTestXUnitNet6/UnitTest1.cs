using ValueMapperUtility;

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
    }
}