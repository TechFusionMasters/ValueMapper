using BenchmarkDotNet.Attributes;
using ValueMapperUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class ValueMapperBenchmarks
    {
        private List<SourceSimple> _simpleObjects;
        private List<SourceComplex> _complexObjects;
        private SourceWithDifferentTypes _sourceWithDifferentTypes;
        private SourceWithCustomNames _sourceWithCustomNames;

        [GlobalSetup]
        public void Setup()
        {
            // Generate test data
            _simpleObjects = GenerateSimpleObjects(10000);
            _complexObjects = GenerateComplexObjects(10000);
            _sourceWithDifferentTypes = new SourceWithDifferentTypes
            {
                IntProperty = 42,
                DoubleProperty = 3.14,
                DecimalProperty = 99.99m,
                StringProperty = "Hello",
                DateTimeProperty = DateTime.Now,
                EnumProperty = TestEnum.Value2
            };
            _sourceWithCustomNames = new SourceWithCustomNames
            {
                SourceProperty = "Test value",
                AnotherProperty = 123
            };

            // Clear caches to ensure fresh start
            ValueMapper.ClearCaches();
        }

        [Benchmark]
        public void ColdStartBenchmark()
        {
            // This measures the first-time mapping cost including compilation
            ValueMapper.ClearCaches();
            var result = ValueMapper.Map<SourceSimple, DestSimple>(_simpleObjects[0]);
        }

        [Benchmark]
        public DestSimple MapSimpleObject()
        {
            return ValueMapper.Map<SourceSimple, DestSimple>(_simpleObjects[0]);
        }

        [Benchmark]
        public DestComplex MapComplexObject()
        {
            return ValueMapper.Map<SourceComplex, DestComplex>(_complexObjects[0]);
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        public List<DestSimple> MapCollection(int count)
        {
            return ValueMapper.MapList<SourceSimple, DestSimple>(_simpleObjects.Take(count));
        }

        [Benchmark]
        [Arguments(10000)]
        public List<DestSimple> MapCollectionParallel(int count)
        {
            return ValueMapper.MapList<SourceSimple, DestSimple>(_simpleObjects.Take(count));
        }

        [Benchmark]
        public DestWithDifferentTypes MapWithTypeConversions()
        {
            return ValueMapper.Map<SourceWithDifferentTypes, DestWithDifferentTypes>(_sourceWithDifferentTypes);
        }

        [Benchmark]
        public DestWithCustomNames MapWithCustomPropertyNames()
        {
            return ValueMapper.Map<SourceWithCustomNames, DestWithCustomNames>(_sourceWithCustomNames);
        }

       
        #region Helper Methods

        private List<SourceSimple> GenerateSimpleObjects(int count)
        {
            var result = new List<SourceSimple>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(new SourceSimple
                {
                    Id = i,
                    Name = $"Name {i}",
                    Description = $"This is description for item {i}"
                });
            }
            return result;
        }

        private List<SourceComplex> GenerateComplexObjects(int count)
        {
            var result = new List<SourceComplex>(count);
            var random = new Random(42); // Fixed seed for reproducibility

            for (int i = 0; i < count; i++)
            {
                var tags = new List<string>();
                var counts = new Dictionary<string, int>();

                int tagCount = random.Next(1, 6);
                for (int t = 0; t < tagCount; t++)
                {
                    tags.Add($"Tag{t}");
                    counts[$"Tag{t}"] = random.Next(1, 100);
                }

                result.Add(new SourceComplex
                {
                    Id = i,
                    Name = $"Complex {i}",
                    Description = $"This is a complex object with ID {i}",
                    Child = new SourceSimple
                    {
                        Id = i * 100,
                        Name = $"Child {i}",
                        Description = $"Child description {i}"
                    },
                    Tags = tags,
                    Counts = counts,
                    CreatedDate = DateTime.Now.AddDays(-random.Next(1, 365)),
                    Duration = TimeSpan.FromMinutes(random.Next(1, 120)),
                    Price = (decimal)(random.NextDouble() * 1000),
                    IsActive = random.Next(0, 2) == 1
                });
            }
            return result;
        }

        #endregion
    }
}
