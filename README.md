# SimpleMapper.Net

A simple, zero-configuration object-to-object mapper for .NET. Convention-based mapping powered by compiled expression trees, with a fluent builder for per-call overrides and a single lightweight dependency (`Microsoft.Extensions.DependencyInjection.Abstractions`).

SimpleMapper.Net is an opinionated, MIT-licensed object mapper for .NET that optimizes for one thing: making the common mapping case effortless. It is a deliberately simpler, open-source alternative to AutoMapper — it does not try to solve everything AutoMapper solves, and that is the point.

## Philosophy

SimpleMapper.Net exists to improve the day-to-day developer experience of mapping objects, not to be a feature-complete mapping framework. The design is opinionated:

- **Simplicity first.** The common case — copy a DTO, ignore a field, rename another, map a nested graph — should need zero configuration and read like plain code. If a feature would complicate that path, it stays out.
- **Performance, closely balanced.** Mapping runs through compiled expression trees, so the convenience costs little over hand-written code. Simplicity wins ties, but performance is never an afterthought.
- **Small on purpose.** No projections, no flattening conventions, no resolver/converter pipeline, no runtime configuration to validate. Fewer concepts to learn, fewer ways to get it wrong. When you genuinely need those, a heavier mapper is the right tool — see the [trade-offs](#where-automapper-or-another-mapper-is-a-better-fit) below.

If you want a mapper that does everything, this isn't it. If you want your DTO mapping to get out of your way, it is.

## Features

- **Zero configuration** for the common case: properties and public fields with matching names are copied automatically, including nested objects, collections and dictionaries.
- **Fully dynamic**: no profiles, no `CreateMap`, no startup registration. Caches compile lazily on first use.
- **Bidirectional by nature**: `User -> UserDto` and `UserDto -> User` both work without any setup.
- **Fluent builder** for per-call overrides: ignore properties, rename properties, navigate deep paths type-safely with lambdas and `Each()`.
- **Map onto an existing instance**: `dto.MapTo(entity)` applies a DTO onto an object you already have (e.g. a tracked EF entity).
- **Deep by default**: the mapped object never aliases the source graph — nested objects and collection items are new instances, even when source and target types are identical (dictionaries are the documented exception).
- **Fail loud**: unmappable members throw `MappingException` naming the property and both types — no silent skips, no zeroed structs, no raw expression-tree errors.
- **Thread-safe**: all caches are lock-free `ConcurrentDictionary` lookups after first use.
- **Fast**: compiled expression trees give hand-written-code performance on the hot path — on par with AutoMapper (see [Benchmarks](docs/benchmarks.md)).
- **Debug logging**: print the whole mapping tree to the console — or any `TextWriter` — to diagnose a mapping.

## Installation

```bash
dotnet add package SimpleMapper.Net
```

Requires .NET 8.0 or later. The package multi-targets `net8.0` and `net10.0` (a .NET 9 project resolves the `net8.0` asset via NuGet).

## Example domain

The examples below use a small blog domain. The entities and DTOs share the same
property names, so mapping needs no configuration:

```csharp
class User                     class UserDto
{                              {
    string Id;                     string Id;
    string Name;                   string Name;
    string Handle;                 string DisplayName;   // renamed on demand
    string InternalNotes;          // intentionally dropped on demand
    Account Account;               AccountDto Account;
    List<Article> Articles;        List<ArticleDto> Articles;
}                              }

class Account { string Username; string Password; string TaxId; string Document; }
class Article { string Title; Media Media; }
class Media   { string CoverUrl; List<string> Thumbnails; }
```

## Quick start

```csharp
using SimpleMapper.Net;

// Convention-based mapping: properties with matching names are copied
var dto = user.MapTo<UserDto>();

// Reverse direction works without any configuration
var model = dto.MapTo<User>();

// Runtime-resolved target type
var obj = user.MapTo(typeof(UserDto));

// Map every item of a collection
List<UserDto> dtos = users.MapListTo<UserDto>();

// Map onto an existing instance (unmatched target members keep their values)
var entity = await db.Users.FindAsync(id);
dto.MapTo(entity);
```

## Fluent builder

```csharp
// Ignore a property
var dto = user.Map()
    .Ignore("InternalNotes")
    .To<UserDto>();

// Rename a property (source -> target)
var dto = user.Map()
    .Map("Handle", "DisplayName")
    .To<UserDto>();

// Print the mapping tree to the console (diagnostic slow path)
var dto = user.Map()
    .WithDebugLogging()
    .To<UserDto>();

// Or send the tree to any TextWriter (plain text) — usable in tests and server logs
var writer = new StringWriter();
var dto = user.Map()
    .WithDebugLogging(writer)
    .To<UserDto>();

// Apply the configured mapping onto an existing instance
user.Map()
    .Ignore("InternalNotes")
    .To(existingDto);
```

### Deep property navigation

Lambdas give you compile-time safety for nested paths. `Each()` navigates into the items of a collection:

```csharp
// Ignore a nested property
var dto = user.Map()
    .Ignore(x => x.Account.Password)
    .To<UserDto>();

// Ignore a property inside every item of a collection
var dto = user.Map()
    .Ignore(x => x.Articles.Each().Media.Thumbnails)
    .To<UserDto>();

// Ignore the whole collection (no Each)
var dto = user.Map()
    .Ignore(x => x.Articles)
    .To<UserDto>();

// Rename a nested property (paths must have the same depth; only the leaf differs)
var dto = user.Map()
    .Map(x => x.Account.TaxId, x => x.Account.Document)
    .To<UserDto>();

// Combine flat (string) and deep (lambda) freely
var dto = user.Map()
    .Ignore("InternalNotes")
    .Ignore(x => x.Account.Password)
    .Ignore(x => x.Articles.Each().Media.Thumbnails)
    .Map(x => x.Account.TaxId, x => x.Account.Document)
    .To<UserDto>();
```

`Each()` exists only for expression-tree parsing — calling it at runtime throws `InvalidOperationException`.

## Null safety and instantiation

```csharp
// A null source returns default (null for reference types)
UserDto? dto = ((User?)null).MapTo<UserDto>();  // dto == null
```

- A `null` source property is written to the target only when the target property is nullable; non-nullable target properties keep their default value (skip-if-null semantics, driven by nullable reference type annotations).
- Targets with an accessible parameterless constructor (public or protected) are created through it. Targets without one — positional records, entities with required constructor arguments — are created **without running any constructor** (`RuntimeHelpers.GetUninitializedObject`) and populated property by property. Constructor-enforced invariants are bypassed in that case; keep that in mind for types whose constructors validate.

### Cyclic graphs and recursion depth

SimpleMapper.Net does not follow reference cycles. To stay safe against uncontrolled recursion (CWE-674) — a cyclic graph such as bidirectional or ORM navigation references, or an extremely deep one — mapping is bounded by a maximum depth. Exceeding it throws a catchable `MappingDepthExceededException` instead of terminating the process with a `StackOverflowException`:

```csharp
try
{
    var dto = userWithCyclicReferences.MapTo<UserDto>();
}
catch (MappingDepthExceededException)
{
    // Break the cycle before mapping (e.g. .Ignore the back-reference).
}
```

The limit defaults to 100 and is configurable at startup:

```csharp
SimpleMapperOptions.MaxDepth = 250; // only if your graph is legitimately deep
```

This is the same class of issue as [CVE-2026-32933](https://github.com/advisories/ghsa-rvv3-g6hj-g44x) in AutoMapper; SimpleMapper.Net ships the depth guard by default.

## Dependency injection

Registration is optional — mapping works with zero setup. `AddSimpleMapper` exists for discoverability and for registering polymorphic subtype rules at startup:

```csharp
using Microsoft.Extensions.DependencyInjection;

// No-op registration (discoverability)
services.AddSimpleMapper();

// With polymorphic subtype rules (experimental, see below)
services.AddSimpleMapper(config => config
    .MapSubtype<Article>(
        src => src is VideoArticle,
        typeof(VideoArticleDto)));
```

## Polymorphic mapping (`MapSubtype` / `RegisterSubtype`) — WIP

> **Status: work in progress / experimental.** These APIs are marked with
> `[Experimental("SMEXP001")]` — consuming them produces a compiler diagnostic that
> you must suppress explicitly, as an acknowledgment that the API may change.

### What it does

When a source object is *declared* as a base type but the runtime instance is a derived type, a plain convention mapper would produce the base DTO and silently drop the derived data. `MapSubtype` registers a **discriminator**: a predicate that inspects the source instance and, when it matches, redirects the mapping to a more specific target type.

```csharp
SimpleMapperExtensions.RegisterSubtype<Article>(
    src => src is VideoArticle,   // discriminator
    typeof(VideoArticleDto));     // target created when it matches

// videoArticle.MapTo<ArticleDto>() -> creates VideoArticleDto (derived data preserved)
// article.MapTo<ArticleDto>()      -> creates ArticleDto (base, as declared)
```

### The problem it solves

Object graphs with inheritance lose subtype fidelity during naive mapping. The classic case: a `List<Article>` that contains a `VideoArticle` element. Without a subtype rule, the video item is mapped as a plain `ArticleDto` — its video-specific properties (`VideoUrl`, `DurationSeconds`, etc.) silently disappear, and round-tripping the DTO back to the entity produces the wrong concrete type. The discriminator preserves the concrete type on both directions of the mapping.

### Why it is still WIP

The mechanism works (it is covered by the test suite), but its current design has sharp edges you must respect:

1. **Global static registry.** Rules are stored in a process-wide static dictionary, not per mapper instance or per DI container. Every consumer in the process shares them.
2. **Register before the first mapping.** The engine caches a per-type "has no subtype rules" short-circuit after the first mapping of a type. A rule registered *after* that type has already been mapped may be ignored. Register all rules at startup (e.g. via `AddSimpleMapper`) before any mapping runs.
3. **State leaks across tests.** Because the registry is static, test suites that register subtype rules share them between test classes and runs within the same process.

A future revision will move the registry into an instance/DI-scoped configuration. Until then the API stays behind the `SMEXP001` experimental diagnostic.

## Advantages and trade-offs vs AutoMapper

### Where SimpleMapper.Net wins

| Aspect | SimpleMapper.Net | AutoMapper |
| --- | --- | --- |
| License | MIT, free forever | Commercial license from v15+ |
| Setup | Zero — no profiles, no `CreateMap`, no `IMapper` to inject | Every pair needs `CreateMap`; configuration must be registered and validated |
| Bidirectional mapping | Automatic | Requires `ReverseMap()` or a second `CreateMap` |
| Learning surface | A handful of extension methods and one builder | Large API: profiles, resolvers, converters, projections |
| Dependency weight | One abstractions package | AutoMapper package + configuration infrastructure |
| Per-call overrides | Fluent builder at the call site | Configuration is global; per-call tweaks are awkward |

### Where AutoMapper (or another mapper) is a better fit

| Need | Why SimpleMapper.Net is not the tool |
| --- | --- |
| `IQueryable` projection (`ProjectTo`) to SQL | Not supported — SimpleMapper.Net maps in-memory objects only |
| Flattening conventions (`Account.Username -> AccountUsername`) | Not supported — names must match, or be renamed explicitly per call |
| Custom value resolvers / type converters | Not supported — transformation logic belongs in your code before/after mapping |
| Constructor-based mapping with validation | SimpleMapper.Net bypasses constructors for types without a parameterless one |
| Compile-time generated mappers (zero reflection at runtime) | Consider Mapperly-style source generators |
| Configuration validation at startup (`AssertConfigurationIsValid`) | There is no configuration to validate — typos in `Map`/`Ignore` strings surface at runtime |

### What maps, what throws

SimpleMapper.Net prefers a loud, named error over silently wrong data. The support matrix:

| Member pair | Behavior |
| --- | --- |
| Same simple type (primitives, string, decimal, enums, Guid, DateTime/DateOnly/TimeOnly, TimeSpan, Uri, Version) | Direct copy |
| `T` <-> `T?` and numeric widening (`int -> long`, `float -> double`) | Converted |
| Incompatible simple types (`string -> double`, `int -> string`, `string -> enum`) | Throws `MappingException` naming the member and both types |
| Nested object, different or identical types | Deep-mapped (a new instance; the DTO never aliases the source) |
| Collection to `T[]`, `List<T>` or any interface a `List<T>` satisfies (`IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, ...) | Deep-mapped item by item |
| Collection to `HashSet<T>`, immutable collections, non-generic collections (`ArrayList`) | Throws `MappingException` at plan build |
| Dictionary | Copied **by reference** (documented exception; not cloned) |
| `object`-typed member, delegate | Copied by reference (the target shape is unknowable / not instantiable) |
| Struct **target type** (`MapTo<SomeStruct>()`) | Throws `NotSupportedException` |
| Struct **property**, identical types | Value copy |
| Struct property, different types | Throws `MappingException` |

### Known limitations

- Cyclic object graphs are not followed — they throw `MappingDepthExceededException` (see above), they are not resolved into cyclic DTO graphs.
- Deep `Map`/`Ignore` paths require the source and target paths to have the same depth.
- The debug path (`WithDebugLogging`) is intentionally slow and allocates; never leave it on in production code.
- **NativeAOT / trimming**: mapping code is built at runtime with reflection and compiled expression trees. The public API is annotated with `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]`, so AOT/trimmed projects get a compile-time warning: SimpleMapper.Net is not the right tool there — consider a source-generated mapper (e.g. Mapperly).

## Performance

Benchmarked with BenchmarkDotNet against AutoMapper 14.0.0 (the last MIT release) over a synthetic content-platform graph (~60 mapped properties, 4-5 nesting levels, collections, dictionary, one polymorphic item). Full methodology, environment and reproduction steps: [docs/benchmarks.md](docs/benchmarks.md).

<!-- BENCHMARK-SUMMARY:START -->
Containerized run (Ubuntu Arm64 container, .NET 10.0.9, 1 CPU / 2 GB), v2.0.0:

| Scenario | AutoMapper 14.0.0 | SimpleMapper.Net |
| --- | --- | --- |
| Single mapping (forward) | 1.601 us / 5.14 KB | 2.238 us / 5.59 KB |
| Single mapping (reverse) | 1.539 us / 5.14 KB | 2.517 us / 5.59 KB |
| Batch x100 (forward) | 177.3 us / 514.9 KB | 247.3 us / 560.2 KB |

Same order of magnitude across the board; single mappings land roughly half a microsecond over AutoMapper per call on this graph, and allocations are unchanged from v1.x — the unified deep-map semantics added no work to the hot path.
<!-- BENCHMARK-SUMMARY:END -->

Run it yourself, with pinned resources, in one command:

```bash
docker compose -f docker-compose.benchmarks.yml up --build
```

## Documentation

- [Architecture and internals](docs/architecture.md)
- [Benchmarks: methodology and results](docs/benchmarks.md)
- [Changelog](CHANGELOG.md)
- Portuguese (pt-BR) translations: [docs/pt-br/](docs/pt-br/)

## Contributing

Contributions are welcome, and you do not need to be a mapping expert to help. Bug reports, failing-test reproductions, documentation fixes, new examples and features all move the project forward. If you are unsure whether an idea fits the [philosophy](#philosophy), open an issue first and let's talk it through.

**AI-assisted contributions are welcome** — everyone uses these tools now, so use whatever helps you. Just apply good sense: you own what you submit, and it must clear the same bar as any other change (tests, style, scope, and a real understanding of what the code does).

### Getting started

```bash
git clone https://github.com/giacomeli/SimpleMapper
cd SimpleMapper
dotnet build SimpleMapper.Net.slnx -c Release   # must be warning-free
dotnet test SimpleMapper.Net.slnx               # must be green
```

### Requirements for a pull request

- **Tests pass.** `dotnet test` is green and the Release build has zero warnings.
- **Test-first.** New features and bug fixes ship with tests, written before the implementation. A bug fix must include a test that fails without your change and passes with it.
- **Public API is documented.** Every public type and member carries an XML doc comment (the build enforces this).
- **English, with pt-BR mirrors.** Code, comments, exception messages and the primary docs are in English. If you change behavior or the public API, update `README.md` / `docs/` and mirror it in `docs/pt-br/`.
- **Performance changes include benchmarks.** Any PR that claims a speed or allocation improvement — or that touches the engine or the fast path — must include before/after numbers from the containerized run (`docker compose -f docker-compose.benchmarks.yml up --build`). "It feels faster" is not a benchmark.
- **Reasonable size.** Keep pull requests small and reviewable. Very large or sprawling PRs will not be accepted — split them into focused pieces.
- **Commits.** Conventional Commits in English (`feat:`, `fix:`, `refactor:`, `docs:`, `chore:`, `perf:`, `test:`), imperative mood. Using AI to help write the change is fine; the commit message itself stays free of AI attribution or `Co-Authored-By` trailers.

### Good practices

- **Keep the common case on the fast path.** The zero-config mapping path is where the performance lives. If you touch `MapperEngine` or the caches, run the benchmarks (`docker compose -f docker-compose.benchmarks.yml up --build`) and confirm there is no regression. See [docs/architecture.md](docs/architecture.md) for how the execution paths and the `useFast` check work.
- **Stay in scope.** Prefer small, focused pull requests. Don't reformat unrelated code, and match the surrounding style.
- **Guard the philosophy.** A feature that complicates the common path, or pulls the library toward "do everything," is likely to be declined — that is by design. When in doubt, propose it in an issue before writing code.
- **Discuss larger changes first.** Public API changes, new dependencies and anything touching the subtype registry (`SMEXP001`) are best agreed on in an issue.

## Publishing (maintainers)

### Trusted Publishing (recommended)

nuget.org discourages long-lived API keys. The repository ships a GitHub Actions
workflow (`.github/workflows/publish.yml`) that uses **Trusted Publishing**: it
exchanges a short-lived GitHub OIDC token for a temporary nuget.org key (valid for
1 hour) at push time, so no secret key is ever stored.

One-time setup:

1. On nuget.org, open **Account -> Trusted Publishing** and add a policy:
   - **Repository Owner:** `giacomeli`
   - **Repository:** `SimpleMapper`
   - **Workflow File:** `publish.yml` (file name only, no path)
   - **Environment:** `release` (optional; matches the `environment:` in the workflow)
2. Add a repository secret `NUGET_USER` with your nuget.org **profile name** (not your
   email). It is public information; the secret only keeps the workflow tidy.

To publish, cut a GitHub Release with a tag like `v1.0.0` (the workflow derives the
package version from the tag), or trigger the workflow manually via **Actions -> Publish
to NuGet -> Run workflow**. The workflow runs the test suite before it packs and pushes,
and uploads the `.snupkg` symbols alongside the package.

### Manual (local, fallback)

Only if you cannot use CI. Requires a manually created API key:

```bash
dotnet pack src/SimpleMapper.Net/SimpleMapper.Net.csproj -c Release
dotnet nuget push src/SimpleMapper.Net/bin/Release/SimpleMapper.Net.1.0.0.nupkg \
    --source https://api.nuget.org/v3/index.json --api-key <API_KEY>
```

## License

[MIT](LICENSE) — Juliano Giacomeli
