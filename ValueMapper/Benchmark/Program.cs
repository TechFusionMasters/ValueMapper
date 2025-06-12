using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ValueMapperUtility;
using AutoMapper;
using Mapster;
using Benchmark;

namespace Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Running ValueMapper Benchmarks...");

            // For quick testing without BenchmarkDotNet
            if (args.Length > 0 && args[0] == "quick")
            {
                Console.WriteLine("*** PERFORMANCE BENCHMARKS ***");
                QuickBenchmark();
                Console.WriteLine("\n*** COMPARISON BENCHMARKS ***");
                QuickComparisonBenchmark();
                return;
            }

            // Run full benchmarks with BenchmarkDotNet
            Console.WriteLine("Running ValueMapper Performance Benchmarks...");
            var performanceSummary = BenchmarkRunner.Run<ValueMapperBenchmarks>();

            Console.WriteLine("Running Mapper Comparison Benchmarks...");
            var comparisonSummary = BenchmarkRunner.Run<MapperComparisonBenchmarks>();

            Console.WriteLine("********* PERFORMANCE BENCHMARK ***********");
            Console.WriteLine(performanceSummary);
            Console.WriteLine("********* COMPARISON BENCHMARK ***********");
            Console.WriteLine(comparisonSummary);
        }

        private static void QuickBenchmark()
        {
            var benchmark = new ValueMapperBenchmarks();
            benchmark.Setup();

            Console.WriteLine("Quick benchmark mode (less accurate but faster)");
            Console.WriteLine("=============================================");

            // Run each benchmark manually with a stopwatch
            RunQuickBenchmark("Simple property mapping (1 object)", () => benchmark.MapSimpleObject());
            RunQuickBenchmark("Complex property mapping (1 object)", () => benchmark.MapComplexObject());
            RunQuickBenchmark("Collection mapping (100 objects)", () => benchmark.MapCollection(100));
            RunQuickBenchmark("Collection mapping (1,000 objects)", () => benchmark.MapCollection(1000));
            RunQuickBenchmark("Collection mapping (10,000 objects)", () => benchmark.MapCollection(10000));
            RunQuickBenchmark("Collection mapping parallel (10,000 objects)", () => benchmark.MapCollectionParallel(10000));
            RunQuickBenchmark("Type conversion benchmark", () => benchmark.MapWithTypeConversions());
            RunQuickBenchmark("Custom property mapping", () => benchmark.MapWithCustomPropertyNames());

            Console.WriteLine("\nCold vs Hot performance:");
            Console.WriteLine("======================");

            // Cold start (first time)
            var sw = Stopwatch.StartNew();
            benchmark.ColdStartBenchmark();
            sw.Stop();
            Console.WriteLine($"Cold start (first time): {sw.ElapsedMilliseconds}ms");

            // Warm start (cached)
            sw.Restart();
            benchmark.ColdStartBenchmark();
            sw.Stop();
            Console.WriteLine($"Warm start (cached): {sw.ElapsedMilliseconds}ms");

            Console.WriteLine("\nMemory usage:");
            Console.WriteLine("=============");
            Console.WriteLine("Note: For accurate memory benchmarks, use the full BenchmarkDotNet run");
        }

        private static void QuickComparisonBenchmark()
        {
            var benchmark = new MapperComparisonBenchmarks();
            benchmark.Setup();

            Console.WriteLine("Quick comparison benchmark mode (less accurate but faster)");
            Console.WriteLine("=======================================================");

            // Run each benchmark manually with a stopwatch
            RunQuickBenchmark("Manual mapping (baseline)", () => benchmark.Manual());
            RunQuickBenchmark("ValueMapper", () => benchmark.ValueMapper());
            RunQuickBenchmark("AutoMapper", () => benchmark.AutoMapper());
            RunQuickBenchmark("Mapster", () => benchmark.Mapster());
            RunQuickBenchmark("ManuallyImplementedMapper", () => benchmark.ManuallyImplementedMapper());

            Console.WriteLine("\nCollection mapping (1000 items):");
            Console.WriteLine("===============================");
            RunQuickBenchmark("ValueMapperCollection", () => benchmark.ValueMapperCollection());
            RunQuickBenchmark("AutoMapperCollection", () => benchmark.AutoMapperCollection());
            RunQuickBenchmark("MapsterCollection", () => benchmark.MapsterCollection());
            RunQuickBenchmark("ManuallyImplementedMapperCollection", () => benchmark.ManuallyImplementedMapperCollection());

            Console.WriteLine("\nWarmup performance (first-time use):");
            Console.WriteLine("===================================");

            // Warm-up tests
            var sw = Stopwatch.StartNew();
            benchmark.ValueMapperWarmup();
            sw.Stop();
            Console.WriteLine($"ValueMapper warmup: {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            benchmark.AutoMapperWarmup();
            sw.Stop();
            Console.WriteLine($"AutoMapper warmup: {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            benchmark.MapsterWarmup();
            sw.Stop();
            Console.WriteLine($"Mapster warmup: {sw.ElapsedMilliseconds}ms");

            // No warmup needed for manually implemented mapper
            Console.WriteLine("ManuallyImplementedMapper: No warmup needed (direct implementation)");

            Console.WriteLine("\nRelative Performance (vs Manual):");
            Console.WriteLine("=================================");

            // Run the baseline multiple times for more accurate timing
            double manualTime = 0;
            for (int i = 0; i < 1000; i++)
            {
                var manualSw = Stopwatch.StartNew();
                benchmark.Manual();
                manualSw.Stop();
                manualTime += manualSw.Elapsed.TotalMilliseconds;
            }
            manualTime /= 1000;

            MeasureRelativePerformance("ValueMapper", () => benchmark.ValueMapper(), manualTime);
            MeasureRelativePerformance("AutoMapper", () => benchmark.AutoMapper(), manualTime);
            MeasureRelativePerformance("Mapster", () => benchmark.Mapster(), manualTime);
            MeasureRelativePerformance("ManuallyImplementedMapper", () => benchmark.ManuallyImplementedMapper(), manualTime);

            Console.WriteLine("\nMemory usage:");
            Console.WriteLine("=============");
            Console.WriteLine("Note: For accurate memory benchmarks, use the full BenchmarkDotNet run");
        }


        private static void RunQuickBenchmark(string name, Action benchmarkAction)
        {
            // Warm up
            for (int i = 0; i < 3; i++)
            {
                benchmarkAction();
            }

            // Actual measurement
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                benchmarkAction();
            }
            stopwatch.Stop();

            Console.WriteLine($"{name}: {stopwatch.ElapsedMilliseconds / 100.0:F3}ms per operation");
        }

        private static void MeasureRelativePerformance(string name, Action benchmarkAction, double baselineTime)
        {
            // Actual measurement
            double totalTime = 0;
            for (int i = 0; i < 100; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                benchmarkAction();
                stopwatch.Stop();
                totalTime += stopwatch.Elapsed.TotalMilliseconds;
            }
            double avgTime = totalTime / 100;
            double relativeFactor = avgTime / baselineTime;

            Console.WriteLine($"{name}: {avgTime:F3}ms (x{relativeFactor:F2} slower than manual)");
        }
    }
}