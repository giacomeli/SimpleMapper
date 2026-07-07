# Reproducible benchmark environment for SimpleMapper.Net vs AutoMapper.
# The SDK image is required because BenchmarkDotNet compiles generated benchmark
# hosts at runtime.
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY . .

RUN dotnet build -c Release benchmarks/SimpleMapper.Net.Benchmarks/SimpleMapper.Net.Benchmarks.csproj

# Full suite by default; results land in /results (mount it as a volume).
ENTRYPOINT ["dotnet", "run", "-c", "Release", "--no-build", \
    "--project", "benchmarks/SimpleMapper.Net.Benchmarks", "--", \
    "--filter", "*", "--artifacts", "/results"]
