using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(SimpleMapper.Net.Benchmarks.MappingBenchmarks).Assembly).Run(args);
