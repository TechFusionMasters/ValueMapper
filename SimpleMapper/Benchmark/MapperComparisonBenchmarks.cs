using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SimpleMapperUtility;
using AutoMapper;
using Mapster;


namespace Benchmark
{
    [MemoryDiagnoser]
    public class MapperComparisonBenchmarks
    {
        private TestSource _testObject;
        private List<TestSource> _testCollection;
        private IMapper _autoMapper;
        private TestMapperImpl _mapperlyMapper;

        [GlobalSetup]
        public void Setup()
        {
            // Generate test data
            _testObject = new TestSource
            {
                Id = 1,
                Name = "Test Object",
                Description = "This is a test object for benchmarking",
                CreatedDate = DateTime.Now,
                IsActive = true,
                Value = 123.45m,
                Flags = new[] { "Flag1", "Flag2", "Flag3" }
            };

            _testCollection = Enumerable.Range(1, 1000)
                .Select(i => new TestSource
                {
                    Id = i,
                    Name = $"Test Object {i}",
                    Description = $"Description for object {i}",
                    CreatedDate = DateTime.Now.AddDays(-i),
                    IsActive = i % 2 == 0,
                    Value = i * 10.5m,
                    Flags = new[] { $"Flag{i}_1", $"Flag{i}_2" }
                })
                .ToList();

            // Configure AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TestSource, TestDestination>();
            });
            _autoMapper = config.CreateMapper();

            // Configure Mapperly-like mapper manually
            _mapperlyMapper = new TestMapperImpl();

            // Clear SimpleMapper cache
            SimpleMapperUtility.SimpleMapper.ClearCaches();
        }

        [Benchmark(Baseline = true)]
        public TestDestination Manual()
        {
            return new TestDestination
            {
                Id = _testObject.Id,
                Name = _testObject.Name,
                Description = _testObject.Description,
                CreatedDate = _testObject.CreatedDate,
                IsActive = _testObject.IsActive,
                Value = _testObject.Value,
                Flags = _testObject.Flags
            };
        }

        [Benchmark]
        public TestDestination SimpleMapper()
        {
            return SimpleMapperUtility.SimpleMapper.Map<TestSource, TestDestination>(_testObject);
        }

        [Benchmark]
        public TestDestination AutoMapper()
        {
            return _autoMapper.Map<TestDestination>(_testObject);
        }

        [Benchmark]
        public TestDestination Mapster()
        {
            return _testObject.Adapt<TestDestination>();
        }

        [Benchmark]
        public TestDestination ManuallyImplementedMapper()
        {
            return _mapperlyMapper.Map(_testObject);
        }

        [Benchmark]
        public List<TestDestination> SimpleMapperCollection()
        {
            return SimpleMapperUtility.SimpleMapper.MapList<TestSource, TestDestination>(_testCollection);
        }

        [Benchmark]
        public List<TestDestination> AutoMapperCollection()
        {
            return _autoMapper.Map<List<TestDestination>>(_testCollection);
        }

        [Benchmark]
        public List<TestDestination> MapsterCollection()
        {
            return _testCollection.Adapt<List<TestDestination>>();
        }

        [Benchmark]
        public List<TestDestination> ManuallyImplementedMapperCollection()
        {
            return _mapperlyMapper.MapList(_testCollection);
        }

        // Warmup vs. cached performance
        [Benchmark]
        public void SimpleMapperWarmup()
        {
            SimpleMapperUtility.SimpleMapper.ClearCaches();
            var result = SimpleMapperUtility.SimpleMapper.Map<TestSource, TestDestination>(_testObject);
        }

        [Benchmark]
        public void AutoMapperWarmup()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TestSource, TestDestination>();
            });
            var mapper = config.CreateMapper();
            var result = mapper.Map<TestDestination>(_testObject);
        }

        [Benchmark]
        public void MapsterWarmup()
        {
            // Reset Mapster config
            var config = new TypeAdapterConfig();
            config.ForType<TestSource, TestDestination>();
            var result = _testObject.Adapt<TestDestination>(config);
        }

        // Test classes
        public class TestSource
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool IsActive { get; set; }
            public decimal Value { get; set; }
            public string[] Flags { get; set; }
        }

        public class TestDestination
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool IsActive { get; set; }
            public decimal Value { get; set; }
            public string[] Flags { get; set; }
        }

        // Interface for mapper (would use Mapperly in real implementation)
        public interface ITestMapper
        {
            TestDestination Map(TestSource source);
            List<TestDestination> MapList(List<TestSource> source);
        }

        // Manual implementation (to simulate what Mapperly would generate)
        public class TestMapperImpl : ITestMapper
        {
            public TestDestination Map(TestSource source)
            {
                if (source == null)
                    return null;

                return new TestDestination
                {
                    Id = source.Id,
                    Name = source.Name,
                    Description = source.Description,
                    CreatedDate = source.CreatedDate,
                    IsActive = source.IsActive,
                    Value = source.Value,
                    Flags = source.Flags
                };
            }

            public List<TestDestination> MapList(List<TestSource> source)
            {
                if (source == null)
                    return null;

                var result = new List<TestDestination>(source.Count);
                foreach (var item in source)
                {
                    result.Add(Map(item));
                }
                return result;
            }
        }
    }
}
