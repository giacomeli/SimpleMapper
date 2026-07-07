# Changelog

All notable changes to SimpleMapper.Net are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the
project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed (behavior)

- **Unified mapping semantics between the fast path and the plan-based path.**
  Nested properties and collection items whose source and target types are
  identical are now deep-mapped on both paths. Previously the zero-config fast
  path copied them by reference while any `Ignore`/rename config deep-mapped them,
  so adding an unrelated `.Ignore(...)` silently changed the aliasing of other
  properties. A mapped DTO no longer aliases the source graph. Exceptions that
  keep the reference: dictionaries (documented), `object`-typed members and
  delegates.
- **Public fields are now mapped on both paths.** Previously they were mapped
  only when an `Ignore`/rename config was present.
- **Constructor resolution is identical on both paths** (public or non-public
  parameterless constructor, otherwise `GetUninitializedObject`), and plan
  building no longer test-invokes the target constructor as a side effect.
- **Runtime type coercion uses the invariant culture.** `Convert.ChangeType`
  coercion on the dynamic path no longer depends on the process culture
  (`"1.5"` cannot become `15` under pt-BR).

### Changed (diagnostics — fail loud instead of silent)

- Mapping to a **struct target** throws `NotSupportedException` instead of
  returning a silently zeroed struct. Struct *properties* are value-copied when
  source and target types are identical and throw `MappingException` otherwise.
- **Incompatible member types** (e.g. `string -> double`, `int -> string`) throw
  `MappingException` naming the member and both types, instead of a raw
  expression-tree error with no context.
- **Unsupported collection targets** (`HashSet<T>`, immutable collections,
  non-generic collections such as `ArrayList`) throw `MappingException` at plan
  build with the member name, instead of a runtime `InvalidCastException` or a
  silent skip. Supported targets: `T[]`, `List<T>` and interfaces a `List<T>`
  satisfies.

### Added

- `MapTo(destination)` and `Map().To(destination)`: map onto an existing
  instance (e.g. apply a DTO onto a tracked EF entity). Unmatched target
  members keep their values.
- `WithDebugLogging(TextWriter)`: send the debug mapping tree to any writer
  (plain text) instead of the console — usable in tests and server logs.
- `MappingException`: the new diagnostic exception for unmappable member pairs.
- Array target properties (`T[]`) are now mapped on the plan-based path too.
- Renames now work when the target member name does not exist on the source
  type (previously the property was silently dropped from the plan).
- NativeAOT/trimming annotations (`[RequiresDynamicCode]`,
  `[RequiresUnreferencedCode]`) on the public mapping API: AOT/trimmed consumers
  get a compile-time warning instead of a runtime surprise.
- `[return: NotNullIfNotNull(nameof(source))]` annotations: the null-in/null-out
  contract of `MapTo` is now visible to the compiler.
- CI workflow (`ci.yml`): warning-free Release build and tests on net8.0 and
  net10.0 for every push and pull request.
- Deterministic release builds (`ContinuousIntegrationBuild`) in the publish
  workflow.

## [1.1.0] - 2026-07-07

- Multi-target `net8.0` and `net10.0`.
- NuGet Trusted Publishing workflow.

## [1.0.0] - 2026-07-07

- First release: convention-based mapping, fluent builder (`Ignore`, `Map`,
  deep paths with `Each()`), polymorphic subtype rules (experimental), recursion
  depth guard, debug tree logging.
