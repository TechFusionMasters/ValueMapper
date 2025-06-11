using SimpleMapperUtility;

namespace UnitTestXUnitNet6
{
    public class SimpleMapperTests
    {
        // Test classes for mapping
        public enum SampleEnum
        {
            None,
            Value1,
            Value2
        }

        [Fact]
        public void Map_ShouldMapPropertiesCorrectly()
        {
            // Arrange
            var source = new Source
            {
                Name = "John",
                Age = 30,
                City = "New York",
                CustomMappedProperty = "CustomValue"
            };

            // Act
            var result = SimpleMapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(source.Name, result.Name);
            Assert.Equal(source.Age, result.Age);
            Assert.Equal(source.City, result.City);
            Assert.Equal(source.CustomMappedProperty, result.CustomName);
        }

        [Fact]
        public void Map_ShouldIgnoreSpecifiedProperties()
        {
            // Arrange
            var source = new Source
            {
                Name = "John",
                Age = 30,
                City = "New York"
            };
            var ignoredProperties = new HashSet<string> { "Age" };

            // Act
            var result = SimpleMapper.Map<Source, Destination>(source, ignoredProperties);

            // Assert
            Assert.Equal(source.Name, result.Name);
            Assert.Equal(default(int), result.Age); // Age should not be mapped
            Assert.Equal(source.City, result.City);
        }

        [Fact]
        public void Map_ShouldHandleEnumConversion_FromString()
        {
            // Arrange
            var source = new Source
            {
                EnumValue = "Value1"
            };

            // Act
            var result = SimpleMapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(SampleEnum.Value1, result.EnumValue);
        }

        [Fact]
        public void Map_ShouldThrowArgumentNullException_WhenSourceIsNull()
        {
            // Arrange
            Source source = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SimpleMapper.Map<Source, Destination>(source));
        }

        [Fact]
        public void MapList_ShouldMapListCorrectly()
        {
            // Arrange
            var sourceList = new List<Source>
            {
                new Source { Name = "John", Age = 30, City = "New York" },
                new Source { Name = "Jane", Age = 25, City = "London" }
            };

            // Act
            var resultList = SimpleMapper.MapList<Source, Destination>(sourceList);

            // Assert
            Assert.Equal(2, resultList.Count);
            Assert.Equal("John", resultList[0].Name);
            Assert.Equal(30, resultList[0].Age);
            Assert.Equal("New York", resultList[0].City);
            Assert.Equal("Jane", resultList[1].Name);
            Assert.Equal(25, resultList[1].Age);
            Assert.Equal("London", resultList[1].City);
        }

        [Fact]
        public void MapList_ShouldThrowArgumentNullException_WhenSourceListIsNull()
        {
            // Arrange
            List<Source> sourceList = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SimpleMapper.MapList<Source, Destination>(sourceList));
        }

        [Fact]
        public void Map_ShouldHandleDifferentPropertyTypes_WithTypeConversion()
        {
            // Arrange
            var source = new Source
            {
                Name = "John",
                Age = 30,
                City = "New York",
                EnumValue = "Value2"
            };

            // Act
            var result = SimpleMapper.Map<Source, Destination>(source);

            // Assert
            Assert.Equal(SampleEnum.Value2, result.EnumValue);
        }

        [Fact]
        public void Map_ShouldThrowInvalidOperationException_WhenMappingInvalidEnumValue()
        {
            // Arrange
            var source = new Source
            {
                EnumValue = "InvalidValue"
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SimpleMapper.Map<Source, Destination>(source));
        }

        [Fact]
        public void Map_ShouldSkipIgnoredProperties_UsingAttribute()
        {
            // Arrange
            var source = new SourceWithIgnoredProperties
            {
                Name = "John",
                IgnoredProperty = "IgnoreMe"
            };

            // Act
            var result = SimpleMapper.Map<SourceWithIgnoredProperties, DestinationWithIgnoredProperties>(source);

            // Assert
            Assert.Equal(source.Name, result.Name);
            Assert.Null(result.IgnoredProperty); // Should be null because it has the ignore attribute
        }
        
    }

}