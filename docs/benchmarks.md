# Benchmarks: SimpleMapper.Net vs AutoMapper

Head-to-head comparison between SimpleMapper.Net and AutoMapper over the same object graph, designed to be **fair** and **reproducible**.

## Methodology

### Tooling

- [BenchmarkDotNet](https://benchmarkdotnet.org/) 0.15.8 with `[MemoryDiagnoser]` (allocation tracking) and the default `[SimpleJob]`.
- AutoMapper pinned to **14.0.0** — the last version published under the MIT license. The commercial 15+ line is out of scope for an open-source comparison. Note: every MIT-licensed AutoMapper release is affected by the high-severity DoS advisory [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x); the fix shipped only in the commercial line. The dependency is benchmark-only and is never part of the SimpleMapper.Net package.

### Workload

A synthetic content-platform graph (`benchmarks/SimpleMapper.Net.Benchmarks/Models/`) with complexity equivalent to a production content object:

- Root `Blog` with ~20 properties, 5 nested objects, 1 complex collection and 2 string lists.
- `Author` with ~20 properties and 3 collections; `Post` with ~48 properties and 3 collections.
- 4-5 nesting levels (`Blog -> Featured (Section) -> Entries -> Post -> Tags`).
- A `Dictionary<string, string>` (publishing options).
- One polymorphic item: a `VideoPost` declared as `Post` inside the featured section, exercising subtype resolution on both mappers.

All data is deterministic and hard-coded (`TestData.BuildBlog`).

### Fairness rules

- Both mappers run **in the same process and the same BenchmarkDotNet job**, so CPU/memory limits apply equally by construction.
- Both are warmed up in `[GlobalSetup]` so lazy caches/configuration are built outside the measurement.
- AutoMapper gets its idiomatic setup: explicit `CreateMap` for every pair in both directions, `Include<>` for the polymorphic pair and `DisableConstructorMapping()` (matching SimpleMapper's setter-based semantics).
- SimpleMapper.Net gets its idiomatic setup: nothing, except the two polymorphic subtype registrations.
- The reverse DTO graph is produced once by AutoMapper in setup, so both directions map equivalent objects.

### Scenarios

| Benchmark | What it measures |
| --- | --- |
| `*_EntityToDto` | Single mapping, entity to DTO (forward) |
| `*_DtoToEntity` | Single mapping, DTO to entity (reverse) |
| `*_Batch100` | 100 sequential mappings, forward |

## Running

### Containerized (recommended, reproducible)

Runs the full suite inside a container with **fixed resource limits** (2 CPUs, 2 GB), which makes results comparable across machines:

```bash
docker compose -f docker-compose.benchmarks.yml up --build
```

If your Docker VM exposes fewer resources, override the limits (the report records the actual environment):

```bash
BENCH_CPUS=1 BENCH_MEM=2g docker compose -f docker-compose.benchmarks.yml up
```

Reports (GitHub markdown, CSV, HTML) are written to `benchmarks/results/`.

### Local (quick look, machine-dependent)

```bash
dotnet run -c Release --project benchmarks/SimpleMapper.Net.Benchmarks -- --filter "*"
```

Useful switches: `--list flat` to enumerate benchmarks, `--filter "*Batch*"` to run a subset, `--job Dry` for a fast smoke run (not statistically meaningful).

## Results

<!-- BENCHMARK-RESULTS:START -->
Containerized run of 2026-07-07. Environment: BenchmarkDotNet v0.15.8, Ubuntu 24.04.4 container on Docker (Arm64, Apple M1 Pro host), .NET 10.0.9, AutoMapper 14.0.0. Limits for this run: **1 CPU / 2 GB** (`BENCH_CPUS=1`; the host Docker VM exposes a single CPU — the canonical config is 2 CPUs). Raw reports: `benchmarks/results/`.

| Scenario | AutoMapper 14.0.0 | SimpleMapper.Net | Time delta |
| --- | --- | --- | --- |
| Blog -> BlogDto | 1.584 us / 5.14 KB | 2.085 us / 5.59 KB | +32% |
| BlogDto -> Blog | 1.521 us / 5.14 KB | 2.100 us / 5.59 KB | +38% |
| Blog -> BlogDto (x100) | 222.3 us / 514.9 KB | 241.6 us / 560.2 KB | +9% |
<!-- BENCHMARK-RESULTS:END -->

## Reading the numbers

- SimpleMapper.Net targets **parity of magnitude** with AutoMapper on this workload, not victory: the goal is to keep the convenience of zero configuration without paying a prohibitive performance tax. On this graph, single mappings cost roughly 0.5-0.6 us more per call (32-38% relative on a ~2 us operation), the batch scenario lands within ~9%, and allocations run ~9% higher. The relative gap depends heavily on graph shape — a graph with more scalar-heavy leaves narrows it.
- Single-run differences of a few percent are within noise for microbenchmarks of this size; look at the error/StdDev columns before drawing conclusions.
- The batch scenario amplifies per-call overhead; it is the most sensitive to fast-path regressions and the main guard for changes to the `useFast` check (see [architecture.md](architecture.md)).
