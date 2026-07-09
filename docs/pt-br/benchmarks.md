# Benchmarks: SimpleMapper.Net vs o campo

> Tradução em português brasileiro. O documento canônico é o [benchmarks.md](../benchmarks.md) em inglês.

Comparação entre SimpleMapper.Net, uma baseline manual escrita à mão, Mapperly (source generator), AutoMapper e Mapster sobre as mesmas cargas de trabalho, desenhada para ser **justa** e **reproduzível**. O objetivo não é vitória: é mostrar exatamente o que a conveniência de configuração zero custa, tanto contra os concorrentes de runtime quanto contra o piso de tempo de compilação.

## Metodologia

### Ferramentas

- [BenchmarkDotNet](https://benchmarkdotnet.org/) 0.15.8 com `[MemoryDiagnoser]` (rastreamento de alocação) e o `[SimpleJob]` padrão; a suíte de cold start usa `RunStrategy.Monitoring` com uma invocação por iteração (veja a ressalva abaixo).
- **Baseline manual**: código de mapeamento escrito à mão (`ManualMapper.cs`) espelhando a semântica publicada do SimpleMapper.Net — cópias profundas, dicionários por referência, checagem de tipo em runtime para o par polimórfico. Marcada como `Baseline = true` em todas as tabelas.
- **Mapperly** pinado em **4.3.1** — source generator: todo o código de mapeamento é gerado em tempo de compilação, zero reflection em runtime. Incluído como referência do que um source generator compra (o README recomenda Mapperly para NativeAOT). Uma diferença semântica: o Mapperly clona dicionários, enquanto o SimpleMapper e a baseline manual os copiam por referência.
- **AutoMapper** pinado em **14.0.0** — a última versão publicada sob licença MIT. A linha comercial 15+ está fora de escopo para uma comparação open source. Nota: toda release MIT do AutoMapper é afetada pelo advisory de DoS de severidade alta [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x); a correção saiu apenas na linha comercial. A dependência é exclusiva dos benchmarks e nunca faz parte do pacote SimpleMapper.Net.
- **Mapster** pinado em **10.0.10** — o concorrente mais próximo em estilo de API (runtime, `Adapt<T>()` por convenção).

### Cargas de trabalho

**Grafo profundo** — um grafo sintético de plataforma de conteúdo (`benchmarks/SimpleMapper.Net.Benchmarks/Models/`) com complexidade equivalente a um objeto de conteúdo de produção:

- `Blog` raiz com ~20 propriedades, 5 objetos aninhados, 1 coleção complexa e 2 listas de strings.
- `Author` com ~20 propriedades e 3 coleções; `Post` com ~48 propriedades e 3 coleções.
- 4-5 níveis de aninhamento (`Blog -> Featured (Section) -> Entries -> Post -> Tags`).
- Um `Dictionary<string, string>` (opções de publicação).
- Um item polimórfico: um `VideoPost` declarado como `Post` dentro da seção em destaque, exercitando a resolução de subtipo em todos os mappers.

**DTO plano** — um par `Customer` com oito membros escalares e nenhum aninhamento. O overhead fixo por chamada domina aqui, então é onde a distância relativa entre mappers de runtime e código de tempo de compilação é maior; publicado de propósito.

**Map para instância existente** — o par plano aplicado sobre um destino pré-alocado (`dto.MapTo(entity)` e o equivalente de cada concorrente). É o formato que o README recomenda para atualizar entidades rastreadas pelo EF Core.

**Cold start** — o primeiro mapeamento do grafo profundo incluindo o que cada mapper constrói de forma lazy: build de plano e compilação de expressions do SimpleMapper (os caches internos são resetados antes de cada iteração), configuração + primeiro map do AutoMapper, config nova + primeiro map do Mapster. Código manual e source generators não têm etapa de construção em runtime e ficam fora deste cenário.

Todos os dados são determinísticos e hard-coded (`TestData.BuildBlog`).

### Regras de justiça

- Todos os mappers rodam **no mesmo processo e no mesmo job do BenchmarkDotNet**, então limites de CPU/memória se aplicam igualmente por construção.
- Todos os mappers são aquecidos no `[GlobalSetup]` para que caches/configuração lazy sejam construídos fora da medição (exceto na suíte de cold start, onde essa construção *é* a medição).
- O AutoMapper recebe seu setup idiomático: `CreateMap` explícito para cada par, `Include<>` para o par polimórfico e `DisableConstructorMapping()` (casando com a semântica setter-based do SimpleMapper).
- O Mapster recebe um `TypeAdapterConfig` explícito com `Include<>` para o par polimórfico; o Mapperly recebe despacho equivalente a `[MapDerivedType]` para o mesmo par.
- O SimpleMapper.Net recebe seu setup idiomático: nada, exceto os dois registros de subtipo polimórfico.
- O grafo DTO reverso é produzido uma vez pelo AutoMapper no setup, então ambas as direções mapeiam objetos equivalentes.

### Cenários

| Suíte | O que mede |
| --- | --- |
| `MappingBenchmarks` | Grafo profundo, mapeamento único ida/volta + batch de 100 |
| `SimpleDtoBenchmarks` | DTO plano, mapeamento único |
| `MapIntoBenchmarks` | DTO plano aplicado sobre instância existente |
| `ColdStartBenchmarks` | Primeiro mapeamento do grafo profundo, incluindo construção lazy |

**Ressalva do cold start**: medições de invocação única (`RunStrategy.Monitoring`, um op por iteração) são inerentemente menos precisas que microbenchmarks de regime estável. Leia esses números como ordens de magnitude, não custos exatos.

## Executando

### Containerizado (recomendado, reproduzível)

Roda a suíte completa dentro de um container com **limites fixos de recursos** (2 CPUs, 2 GB), o que torna os resultados comparáveis entre máquinas:

```bash
docker compose -f docker-compose.benchmarks.yml up --build
```

Se sua VM do Docker expõe menos recursos, sobrescreva os limites (o relatório registra o ambiente real):

```bash
BENCH_CPUS=1 BENCH_MEM=2g docker compose -f docker-compose.benchmarks.yml up
```

Relatórios (markdown GitHub, CSV, HTML) são gravados em `benchmarks/results/`.

### Local (olhada rápida, dependente da máquina)

```bash
dotnet run -c Release --project benchmarks/SimpleMapper.Net.Benchmarks -- --filter "*"
```

Switches úteis: `--list flat` para enumerar os benchmarks, `--filter "*SimpleDto*"` para rodar um subconjunto, `--job Dry` para um smoke run rápido (sem valor estatístico).

## Resultados

<!-- BENCHMARK-RESULTS:START -->
Rodada containerizada de 08-07-2026, v2.1.0. Ambiente: BenchmarkDotNet v0.15.8, container Ubuntu 24.04.4 no Docker (Arm64, host Apple M1 Pro), .NET 10.0.9, AutoMapper 14.0.0, Mapster 10.0.10, Mapperly 4.3.1. Limites desta rodada: **1 CPU / 2 GB** (`BENCH_CPUS=1`; a VM do Docker do host expõe uma única CPU — a configuração canônica é 2 CPUs). Relatórios brutos: `benchmarks/results/`.

### Grafo profundo (~60 propriedades, 4-5 níveis, item polimórfico)

| Mapper | Blog -> BlogDto | BlogDto -> Blog | Alocado |
| --- | --- | --- | --- |
| Manual (baseline) | 0.943 us | 0.966 us | 4.95 KB |
| Mapperly 4.3.1 | 0.904 us | 0.566 us | 3.62 KB |
| Mapster 10.0.10 | 1.164 us | 1.169 us | 4.93 KB |
| AutoMapper 14.0.0 | 1.620 us | 1.637 us | 5.14 KB |
| SimpleMapper.Net | 2.228 us | 2.259 us | 5.59 KB |

Batch x100 (ida): AutoMapper 190.2 us / 514.9 KB, SimpleMapper.Net 256.4 us / 560.2 KB.

### DTO plano (8 membros escalares)

| Mapper | Customer -> CustomerDto | Alocado |
| --- | --- | --- |
| Manual (baseline) | 9.8 ns | 88 B |
| Mapperly 4.3.1 | 9.9 ns | 88 B |
| Mapster 10.0.10 | 16.0 ns | 88 B |
| AutoMapper 14.0.0 | 51.2 ns | 88 B |
| SimpleMapper.Net | 55.8 ns | 88 B |

### Map sobre instância existente (par plano)

| Mapper | CustomerDto -> Customer existente | Alocado |
| --- | --- | --- |
| Manual (baseline) | 4.7 ns | 0 |
| Mapperly 4.3.1 | 4.7 ns | 0 |
| Mapster 10.0.10 | 10.2 ns | 0 |
| SimpleMapper.Net | 29.0 ns | 0 |
| AutoMapper 14.0.0 | 57.1 ns | 0 |

### Cold start (primeiro mapeamento do grafo profundo, ordem de magnitude)

| Mapper | Primeiro mapeamento incluindo construção lazy |
| --- | --- |
| SimpleMapper.Net (build de plano + compile de expressions + map) | 4.6 ms |
| AutoMapper 14.0.0 (configuração + primeiro map) | 17.7 ms |
| Mapster 10.0.10 (config nova + primeiro map) | 44.6 ms |
<!-- BENCHMARK-RESULTS:END -->

## Lendo os números

- **Contra o AutoMapper** (o mapper de onde as pessoas de fato estão migrando): mapeamentos únicos do grafo profundo custam cerca de 0.6 us a mais por chamada (~37% relativo numa operação de ~2 us), o cenário batch fica em ~35%, alocações rodam ~9% acima, e a distância no DTO plano é ~9%. O SimpleMapper.Net é mais rápido onde importa para seus próprios fluxos recomendados: **map-into é ~2x mais rápido** e **cold start é ~4x mais rápido**.
- **Contra a baseline manual**: o SimpleMapper.Net custa 2.4x no grafo profundo e ~5.7x no DTO plano. Esse múltiplo *é* o preço da configuração zero — está publicado para você decidir se importa para a sua carga. Para mapeamento por request em serviços I/O-bound, 1-2 microssegundos extras por request raramente importam.
- **Contra o Mapperly**: o source generator fica no custo do manual ou abaixo em todos os cenários. Se o seu projeto pode adotar um source generator (e você quer NativeAOT), o Mapperly é a ferramenta certa — o README diz o mesmo. O SimpleMapper.Net existe para os casos em que você quer zero declarações por par e flexibilidade em runtime.
- Diferenças de poucos por cento numa única rodada estão dentro do ruído para microbenchmarks deste tamanho; olhe as colunas de erro/StdDev nos relatórios brutos antes de tirar conclusões.
- O cenário batch amplifica o overhead por chamada; é o mais sensível a regressões do fast path e a principal guarda para mudanças no check `useFast` (veja [architecture.md](architecture.md)).
