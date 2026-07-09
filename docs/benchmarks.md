# Benchmarks: SimpleMapper.Net vs the field

Comparison between SimpleMapper.Net, a hand-written manual baseline, Mapperly (source generator), AutoMapper and Mapster over the same workloads, designed to be **fair** and **reproducible**. The goal is not victory: it is to show exactly what the zero-configuration convenience costs, against both the runtime competitors and the compile-time floor.

## Methodology

### Tooling

- [BenchmarkDotNet](https://benchmarkdotnet.org/) 0.15.8 with `[MemoryDiagnoser]` (allocation tracking) and the default `[SimpleJob]`; the cold-start suite uses `RunStrategy.Monitoring` with one invocation per iteration (see its caveat below).
- **Manual baseline**: hand-written mapping code (`ManualMapper.cs`) mirroring SimpleMapper.Net's published semantics — deep copies, dictionaries by reference, runtime type check for the polymorphic pair. Marked `Baseline = true` in every table.
- **Mapperly** pinned to **4.3.1** — a source generator: all mapping code is generated at compile time, zero runtime reflection. Included as the reference point for what a source generator buys (the README recommends Mapperly for NativeAOT). One semantic difference: Mapperly clones dictionaries, while SimpleMapper and the manual baseline copy them by reference.
- **AutoMapper** pinned to **14.0.0** — the last version published under the MIT license. The commercial 15+ line is out of scope for an open-source comparison. Note: every MIT-licensed AutoMapper release is affected by the high-severity DoS advisory [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x); the fix shipped only in the commercial line. The dependency is benchmark-only and is never part of the SimpleMapper.Net package.
- **Mapster** pinned to **10.0.10** — the closest competitor in API style (runtime, convention-based `Adapt<T>()`).

### Workloads

**Deep graph** — a synthetic content-platform graph (`benchmarks/SimpleMapper.Net.Benchmarks/Models/`) with complexity equivalent to a production content object:

- Root `Blog` with ~20 properties, 5 nested objects, 1 complex collection and 2 string lists.
- `Author` with ~20 properties and 3 collections; `Post` with ~48 properties and 3 collections.
- 4-5 nesting levels (`Blog -> Featured (Section) -> Entries -> Post -> Tags`).
- A `Dictionary<string, string>` (publishing options).
- One polymorphic item: a `VideoPost` declared as `Post` inside the featured section, exercising subtype resolution on every mapper.

**Flat DTO** — a `Customer` pair with eight scalar members and no nesting. Fixed per-call overhead dominates here, so this is where the relative gap between runtime mappers and compile-time code is at its widest; it is published on purpose.

**Map into existing instance** — the flat pair applied onto a preallocated destination (`dto.MapTo(entity)` and each competitor's equivalent). This is the shape the README recommends for updating tracked EF Core entities.

**Cold start** — the first deep-graph mapping including whatever each mapper builds lazily: SimpleMapper's plan build and expression compile (internal caches are reset before every iteration), AutoMapper's configuration plus first map, Mapster's fresh config plus first map. Manual code and source generators have no runtime construction step and are out of scope here.

All data is deterministic and hard-coded (`TestData.BuildBlog`).

### Fairness rules

- Every mapper runs **in the same process and the same BenchmarkDotNet job**, so CPU/memory limits apply equally by construction.
- Every mapper is warmed up in `[GlobalSetup]` so lazy caches/configuration are built outside the measurement (except in the cold-start suite, where that construction *is* the measurement).
- AutoMapper gets its idiomatic setup: explicit `CreateMap` for every pair, `Include<>` for the polymorphic pair and `DisableConstructorMapping()` (matching SimpleMapper's setter-based semantics).
- Mapster gets an explicit `TypeAdapterConfig` with `Include<>` for the polymorphic pair; Mapperly gets `[MapDerivedType]`-equivalent dispatch for the same pair.
- SimpleMapper.Net gets its idiomatic setup: nothing, except the two polymorphic subtype registrations.
- The reverse DTO graph is produced once by AutoMapper in setup, so both directions map equivalent objects.

### Scenarios

| Suite | What it measures |
| --- | --- |
| `MappingBenchmarks` | Deep graph, single mapping forward/reverse + 100-mapping batch |
| `SimpleDtoBenchmarks` | Flat DTO, single mapping |
| `MapIntoBenchmarks` | Flat DTO applied onto an existing instance |
| `ColdStartBenchmarks` | First deep-graph mapping, including lazy construction |

**Cold-start caveat**: single-invocation measurements (`RunStrategy.Monitoring`, one op per iteration) are inherently less precise than steady-state microbenchmarks. Read those numbers as orders of magnitude, not exact costs.

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

Useful switches: `--list flat` to enumerate benchmarks, `--filter "*SimpleDto*"` to run a subset, `--job Dry` for a fast smoke run (not statistically meaningful).

## Results

<!-- BENCHMARK-RESULTS:START -->
Containerized run of 2026-07-08, v2.1.0. Environment: BenchmarkDotNet v0.15.8, Ubuntu 24.04.4 container on Docker (Arm64, Apple M1 Pro host), .NET 10.0.9, AutoMapper 14.0.0, Mapster 10.0.10, Mapperly 4.3.1. Limits for this run: **1 CPU / 2 GB** (`BENCH_CPUS=1`; the host Docker VM exposes a single CPU — the canonical config is 2 CPUs). Raw reports: `benchmarks/results/`.

### Deep graph (~60 properties, 4-5 levels, polymorphic item)

| Mapper | Blog -> BlogDto | BlogDto -> Blog | Allocated |
| --- | --- | --- | --- |
| Manual (baseline) | 0.943 us | 0.966 us | 4.95 KB |
| Mapperly 4.3.1 | 0.904 us | 0.566 us | 3.62 KB |
| Mapster 10.0.10 | 1.164 us | 1.169 us | 4.93 KB |
| AutoMapper 14.0.0 | 1.620 us | 1.637 us | 5.14 KB |
| SimpleMapper.Net | 2.228 us | 2.259 us | 5.59 KB |

Batch x100 (forward): AutoMapper 190.2 us / 514.9 KB, SimpleMapper.Net 256.4 us / 560.2 KB.

### Flat DTO (8 scalar members)

| Mapper | Customer -> CustomerDto | Allocated |
| --- | --- | --- |
| Manual (baseline) | 9.8 ns | 88 B |
| Mapperly 4.3.1 | 9.9 ns | 88 B |
| Mapster 10.0.10 | 16.0 ns | 88 B |
| AutoMapper 14.0.0 | 51.2 ns | 88 B |
| SimpleMapper.Net | 55.8 ns | 88 B |

### Map onto an existing instance (flat pair)

| Mapper | CustomerDto -> existing Customer | Allocated |
| --- | --- | --- |
| Manual (baseline) | 4.7 ns | 0 |
| Mapperly 4.3.1 | 4.7 ns | 0 |
| Mapster 10.0.10 | 10.2 ns | 0 |
| SimpleMapper.Net | 29.0 ns | 0 |
| AutoMapper 14.0.0 | 57.1 ns | 0 |

### Cold start (first deep-graph mapping, order of magnitude)

| Mapper | First mapping including lazy construction |
| --- | --- |
| SimpleMapper.Net (plan build + expression compile + map) | 4.6 ms |
| AutoMapper 14.0.0 (configuration + first map) | 17.7 ms |
| Mapster 10.0.10 (fresh config + first map) | 44.6 ms |
<!-- BENCHMARK-RESULTS:END -->

## Reading the numbers

- **Against AutoMapper** (the mapper people are actually migrating from): single deep-graph mappings cost roughly 0.6 us more per call (~37% relative on a ~2 us operation), the batch scenario lands within ~35%, allocations run ~9% higher, and the flat-DTO gap is ~9%. SimpleMapper.Net is faster where it matters for its own recommended workflows: **map-into is ~2x faster** and **cold start is ~4x faster**.
- **Against the manual baseline**: SimpleMapper.Net costs 2.4x on the deep graph and ~5.7x on the flat DTO. That multiple *is* the price of zero configuration — it is published so you can decide whether it matters for your workload. For request-scoped mapping in I/O-bound services, 1-2 extra microseconds per request rarely does.
- **Against Mapperly**: the source generator is at or below manual cost everywhere. If your project can adopt a source generator (and you want NativeAOT), Mapperly is the right tool — the README says the same. SimpleMapper.Net exists for the cases where you want zero per-pair declarations and runtime flexibility.
- Single-run differences of a few percent are within noise for microbenchmarks of this size; look at the error/StdDev columns in the raw reports before drawing conclusions.
- The batch scenario amplifies per-call overhead; it is the most sensitive to fast-path regressions and the main guard for changes to the `useFast` check (see [architecture.md](architecture.md)).
