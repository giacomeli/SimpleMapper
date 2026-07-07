using BenchmarkDotNet.Running;
using SimpleMapper.Net.Benchmarks;

BenchmarkSwitcher.FromTypes(new[] { typeof(MappingBenchmarks) }).Run(args);
