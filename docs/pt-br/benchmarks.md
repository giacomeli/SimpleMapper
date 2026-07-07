# Benchmarks: SimpleMapper.Net vs AutoMapper

> Tradução em português brasileiro. O documento canônico é o [benchmarks.md](../benchmarks.md) em inglês.

Comparação direta entre SimpleMapper.Net e AutoMapper sobre o mesmo grafo de objetos, desenhada para ser **justa** e **reproduzível**.

## Metodologia

### Ferramentas

- [BenchmarkDotNet](https://benchmarkdotnet.org/) 0.15.8 com `[MemoryDiagnoser]` (rastreio de alocação) e o `[SimpleJob]` padrão.
- AutoMapper pinado em **14.0.0** — a última versão publicada sob licença MIT. A linha comercial 15+ está fora do escopo de uma comparação open source. Nota: toda versão MIT do AutoMapper é afetada pelo advisory de DoS de severidade alta [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x); a correção só saiu na linha comercial. A dependência é exclusiva do benchmark e nunca faz parte do pacote SimpleMapper.Net.

### Carga de trabalho

Um grafo sintético de plataforma de conteúdo (`benchmarks/SimpleMapper.Net.Benchmarks/Models/`) com complexidade equivalente a um objeto de conteúdo de produção:

- Root `Blog` com ~20 propriedades, 5 objetos aninhados, 1 coleção complexa e 2 listas de strings.
- `Author` com ~20 propriedades e 3 coleções; `Post` com ~48 propriedades e 3 coleções.
- 4-5 níveis de aninhamento (`Blog -> Featured (Section) -> Entries -> Post -> Tags`).
- Um `Dictionary<string, string>` (opções de publicação).
- Um item polimórfico: um `VideoPost` declarado como `Post` dentro da seção em destaque, exercitando a resolução de subtipo nos dois mappers.

Todos os dados são determinísticos e hard-coded (`TestData.BuildBlog`).

### Regras de justiça

- Os dois mappers rodam **no mesmo processo e no mesmo job do BenchmarkDotNet**, então limites de CPU/memória se aplicam igualmente por construção.
- Ambos recebem warm-up no `[GlobalSetup]`, de modo que caches/configuração lazy são construídos fora da medição.
- O AutoMapper recebe seu setup idiomático: `CreateMap` explícito para cada par nas duas direções, `Include<>` para o par polimórfico e `DisableConstructorMapping()` (equiparando à semântica por setters do SimpleMapper).
- O SimpleMapper.Net recebe seu setup idiomático: nada, exceto os dois registros de subtipo polimórfico.
- O grafo DTO reverso é produzido uma vez pelo AutoMapper no setup, então as duas direções mapeiam objetos equivalentes.

### Cenários

| Benchmark | O que mede |
| --- | --- |
| `*_EntityToDto` | Mapeamento único, entidade para DTO (forward) |
| `*_DtoToEntity` | Mapeamento único, DTO para entidade (reverso) |
| `*_Batch100` | 100 mapeamentos sequenciais, forward |

## Como rodar

### Containerizado (recomendado, reproduzível)

Roda a suite completa dentro de um container com **limites fixos de recursos** (2 CPUs, 2 GB), o que torna os resultados comparáveis entre máquinas:

```bash
docker compose -f docker-compose.benchmarks.yml up --build
```

Se a sua VM do Docker expõe menos recursos, sobrescreva os limites (o relatório registra o ambiente real):

```bash
BENCH_CPUS=1 BENCH_MEM=2g docker compose -f docker-compose.benchmarks.yml up
```

Os relatórios (markdown GitHub, CSV, HTML) são gravados em `benchmarks/results/`.

### Local (olhada rápida, dependente da máquina)

```bash
dotnet run -c Release --project benchmarks/SimpleMapper.Net.Benchmarks -- --filter "*"
```

Switches úteis: `--list flat` para enumerar os benchmarks, `--filter "*Batch*"` para rodar um subconjunto, `--job Dry` para um smoke run rápido (sem valor estatístico).

## Resultados

<!-- BENCHMARK-RESULTS:START -->
Execução containerizada de 07-07-2026. Ambiente: BenchmarkDotNet v0.15.8, container Ubuntu 24.04.4 no Docker (Arm64, host Apple M1 Pro), .NET 10.0.9, AutoMapper 14.0.0. Limites desta execução: **1 CPU / 2 GB** (`BENCH_CPUS=1`; a VM do Docker do host expõe uma única CPU — a config canônica é 2 CPUs). Relatórios brutos: `benchmarks/results/`.

| Cenário | AutoMapper 14.0.0 | SimpleMapper.Net | Delta de tempo |
| --- | --- | --- | --- |
| Blog -> BlogDto | 1.584 us / 5.14 KB | 2.085 us / 5.59 KB | +32% |
| BlogDto -> Blog | 1.521 us / 5.14 KB | 2.100 us / 5.59 KB | +38% |
| Blog -> BlogDto (x100) | 222.3 us / 514.9 KB | 241.6 us / 560.2 KB | +9% |
<!-- BENCHMARK-RESULTS:END -->

## Lendo os números

- O SimpleMapper.Net busca **paridade de ordem de grandeza** com o AutoMapper nesta carga, não vitória: o objetivo é manter a conveniência de zero configuração sem pagar um imposto de performance proibitivo. Neste grafo, mapeamentos únicos custam cerca de 0.5-0.6 us a mais por chamada (32-38% relativo em uma operação de ~2 us), o cenário batch fica dentro de ~9%, e as alocações rodam ~9% acima. O gap relativo depende fortemente do formato do grafo — um grafo com mais folhas escalares o reduz.
- Diferenças de poucos por cento em uma única execução estão dentro do ruído para microbenchmarks deste tamanho; olhe as colunas de erro/StdDev antes de tirar conclusões.
- O cenário batch amplifica o overhead por chamada; é o mais sensível a regressões do fast path e a principal guarda para mudanças no check `useFast` (ver [architecture.md](architecture.md)).
