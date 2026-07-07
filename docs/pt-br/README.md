# SimpleMapper.Net

> Tradução em português brasileiro. O documento canônico é o [README.md](../../README.md) em inglês.

Um mapeador objeto-objeto simples e de configuração zero para .NET. Mapeamento por convenção de nomes com expression trees compiladas, builder fluente para ajustes por chamada e uma única dependência leve (`Microsoft.Extensions.DependencyInjection.Abstractions`).

O SimpleMapper.Net é um mapeador de objetos para .NET opinativo e sob licença MIT, que otimiza para uma coisa: tornar o caso comum de mapeamento sem esforço. É uma alternativa open source deliberadamente mais simples que o AutoMapper — ele não tenta resolver tudo que o AutoMapper resolve, e esse é justamente o ponto.

## Filosofia

O SimpleMapper.Net existe para melhorar a experiência do desenvolvedor no dia a dia de mapear objetos, não para ser um framework de mapeamento completo em features. O design é opinativo:

- **Simplicidade primeiro.** O caso comum — copiar um DTO, ignorar um campo, renomear outro, mapear um grafo aninhado — deve precisar de zero configuração e ler como código comum. Se uma feature complicaria esse caminho, ela fica de fora.
- **Performance, num equilíbrio próximo.** O mapeamento roda por expression trees compiladas, então a conveniência custa pouco frente a código escrito à mão. A simplicidade vence os empates, mas a performance nunca é uma reflexão tardia.
- **Pequeno de propósito.** Sem projeções, sem convenções de flattening, sem pipeline de resolvers/converters, sem configuração em runtime para validar. Menos conceitos para aprender, menos formas de errar. Quando você realmente precisa disso, um mapper mais pesado é a ferramenta certa — veja os [trade-offs](#onde-o-automapper-ou-outro-mapper-é-melhor-escolha) abaixo.

Se você quer um mapper que faz tudo, não é este. Se você quer que o mapeamento dos seus DTOs saia do seu caminho, é este.

## Recursos

- **Zero configuração** para o caso comum: propriedades com nomes iguais são copiadas automaticamente, incluindo objetos aninhados, coleções e dicionários.
- **Totalmente dinâmico**: sem profiles, sem `CreateMap`, sem registro na inicialização. Os caches compilam de forma lazy no primeiro uso.
- **Bidirecional por natureza**: `User -> UserDto` e `UserDto -> User` funcionam sem nenhum setup.
- **Builder fluente** para ajustes por chamada: ignorar propriedades, renomear propriedades, navegar em paths profundos com type safety via lambdas e `Each()`.
- **Thread-safe**: todos os caches são lookups lock-free em `ConcurrentDictionary` após o primeiro uso.
- **Rápido**: expression trees compiladas entregam performance de código escrito à mão no hot path — em paridade com o AutoMapper (ver [Benchmarks](benchmarks.md)).
- **Debug logging**: imprime a árvore completa de mapeamento no console para diagnosticar um mapeamento.

## Instalação

```bash
dotnet add package SimpleMapper.Net
```

Requer .NET 10.0 (multi-targeting para .NET 8+ está planejado).

## Domínio de exemplo

Os exemplos abaixo usam um pequeno domínio de blog. As entidades e os DTOs
compartilham os mesmos nomes de propriedade, então o mapeamento não precisa de
configuração:

```csharp
class User                     class UserDto
{                              {
    string Id;                     string Id;
    string Name;                   string Name;
    string Handle;                 string DisplayName;   // renomeado sob demanda
    string InternalNotes;          // descartado de propósito sob demanda
    Account Account;               AccountDto Account;
    List<Article> Articles;        List<ArticleDto> Articles;
}                              }

class Account { string Username; string Password; string TaxId; string Document; }
class Article { string Title; Media Media; }
class Media   { string CoverUrl; List<string> Thumbnails; }
```

## Início rápido

```csharp
using SimpleMapper.Net;

// Mapeamento por convenção: propriedades com nomes iguais são copiadas
var dto = user.MapTo<UserDto>();

// A direção inversa funciona sem nenhuma configuração
var model = dto.MapTo<User>();

// Tipo target resolvido em runtime
var obj = user.MapTo(typeof(UserDto));

// Mapeia cada item de uma coleção
List<UserDto> dtos = users.MapListTo<UserDto>();
```

## Builder fluente

```csharp
// Ignorar uma propriedade
var dto = user.Map()
    .Ignore("InternalNotes")
    .To<UserDto>();

// Renomear uma propriedade (source -> target)
var dto = user.Map()
    .Map("Handle", "DisplayName")
    .To<UserDto>();

// Imprimir a árvore de mapeamento no console (path lento, diagnóstico)
var dto = user.Map()
    .WithDebugLogging()
    .To<UserDto>();
```

### Navegação profunda de propriedades

Lambdas dão segurança em tempo de compilação para paths aninhados. `Each()` navega para dentro dos itens de uma coleção:

```csharp
// Ignorar uma propriedade aninhada
var dto = user.Map()
    .Ignore(x => x.Account.Password)
    .To<UserDto>();

// Ignorar uma propriedade dentro de cada item de uma coleção
var dto = user.Map()
    .Ignore(x => x.Articles.Each().Media.Thumbnails)
    .To<UserDto>();

// Ignorar a coleção inteira (sem Each)
var dto = user.Map()
    .Ignore(x => x.Articles)
    .To<UserDto>();

// Renomear propriedade aninhada (paths com a mesma profundidade; só o leaf difere)
var dto = user.Map()
    .Map(x => x.Account.TaxId, x => x.Account.Document)
    .To<UserDto>();

// Combinar flat (string) e deep (lambda) livremente
var dto = user.Map()
    .Ignore("InternalNotes")
    .Ignore(x => x.Account.Password)
    .Ignore(x => x.Articles.Each().Media.Thumbnails)
    .Map(x => x.Account.TaxId, x => x.Account.Document)
    .To<UserDto>();
```

`Each()` existe apenas para o parsing da expression tree — chamá-lo em runtime lança `InvalidOperationException`.

## Null safety e instanciação

```csharp
// Source null retorna default (null para reference types)
UserDto? dto = ((User?)null).MapTo<UserDto>();  // dto == null
```

- Uma propriedade source `null` só é escrita no target quando a propriedade target é nullable; propriedades target não-nullable mantêm o valor default (semântica skip-if-null, guiada pelas anotações de nullable reference types).
- Targets com construtor sem parâmetros acessível (público ou protected) são criados por ele. Targets sem esse construtor — records posicionais, entidades com argumentos obrigatórios no construtor — são criados **sem executar nenhum construtor** (`RuntimeHelpers.GetUninitializedObject`) e preenchidos propriedade a propriedade. Invariantes garantidas pelo construtor são puladas nesse caso; leve isso em conta para tipos cujos construtores validam.

### Grafos cíclicos e profundidade de recursão

O SimpleMapper.Net não segue ciclos de referência. Para se proteger contra recursão descontrolada (CWE-674) — um grafo cíclico como referências bidirecionais ou de navegação de ORM, ou um grafo extremamente profundo — o mapeamento é limitado por uma profundidade máxima. Excedê-la lança um `MappingDepthExceededException` capturável em vez de derrubar o processo com um `StackOverflowException`:

```csharp
try
{
    var dto = userWithCyclicReferences.MapTo<UserDto>();
}
catch (MappingDepthExceededException)
{
    // Quebre o ciclo antes de mapear (ex.: .Ignore na referência de volta).
}
```

O limite tem default 100 e é configurável na inicialização:

```csharp
SimpleMapperOptions.MaxDepth = 250; // só se o seu grafo for legitimamente profundo
```

É a mesma classe de problema do [CVE-2026-32933](https://github.com/advisories/ghsa-rvv3-g6hj-g44x) no AutoMapper; o SimpleMapper.Net já vem com o guard de profundidade por padrão.

## Injeção de dependência

O registro é opcional — o mapeamento funciona com zero setup. `AddSimpleMapper` existe por discoverability e para registrar regras de subtipo polimórfico na inicialização:

```csharp
using Microsoft.Extensions.DependencyInjection;

// Registro no-op (discoverability)
services.AddSimpleMapper();

// Com regras de subtipo polimórfico (experimental, ver abaixo)
services.AddSimpleMapper(config => config
    .MapSubtype<Article>(
        src => src is VideoArticle,
        typeof(VideoArticleDto)));
```

## Mapeamento polimórfico (`MapSubtype` / `RegisterSubtype`) — WIP

> **Status: em desenvolvimento / experimental.** Essas APIs carregam
> `[Experimental("SMEXP001")]` — consumi-las produz um diagnóstico de compilação que
> você precisa suprimir explicitamente, como reconhecimento de que a API pode mudar.

### O que faz

Quando um objeto source está *declarado* como um tipo base mas a instância em runtime é um tipo derivado, um mapper por convenção puro produziria o DTO base e descartaria silenciosamente os dados do derivado. O `MapSubtype` registra um **discriminador**: um predicado que inspeciona a instância source e, quando bate, redireciona o mapeamento para um tipo target mais específico.

```csharp
SimpleMapperExtensions.RegisterSubtype<Article>(
    src => src is VideoArticle,   // discriminador
    typeof(VideoArticleDto));     // target criado quando o discriminador bate

// videoArticle.MapTo<ArticleDto>() -> cria VideoArticleDto (dados do derivado preservados)
// article.MapTo<ArticleDto>()      -> cria ArticleDto (base, como declarado)
```

### O problema que resolve

Grafos de objetos com herança perdem a fidelidade de subtipo em mapeamentos ingênuos. O caso clássico: uma `List<Article>` que contém um elemento `VideoArticle`. Sem uma regra de subtipo, o item de vídeo é mapeado como um `ArticleDto` puro — as propriedades específicas de vídeo (`VideoUrl`, `DurationSeconds`, etc.) desaparecem silenciosamente, e o round-trip do DTO de volta para a entidade produz o tipo concreto errado. O discriminador preserva o tipo concreto nas duas direções do mapeamento.

### Por que ainda é WIP

O mecanismo funciona (está coberto pela suite de testes), mas o design atual tem arestas que você precisa respeitar:

1. **Registro global estático.** As regras vivem em um dicionário estático do processo, não por instância de mapper nem por container de DI. Todos os consumidores no processo as compartilham.
2. **Registre antes do primeiro mapeamento.** O engine cacheia por tipo um short-circuit de "não tem regras de subtipo" após o primeiro mapeamento daquele tipo. Uma regra registrada *depois* de o tipo já ter sido mapeado pode ser ignorada. Registre todas as regras na inicialização (ex.: via `AddSimpleMapper`), antes de qualquer mapeamento.
3. **Estado vaza entre testes.** Como o registro é estático, suites de teste que registram regras de subtipo as compartilham entre classes de teste e execuções no mesmo processo.

Uma revisão futura moverá o registro para uma configuração por instância/DI. Até lá a API permanece atrás do diagnóstico experimental `SMEXP001`.

## Vantagens e trade-offs vs AutoMapper

### Onde o SimpleMapper.Net ganha

| Aspecto | SimpleMapper.Net | AutoMapper |
| --- | --- | --- |
| Licença | MIT, gratuito para sempre | Licença comercial a partir da v15+ |
| Setup | Zero — sem profiles, sem `CreateMap`, sem `IMapper` para injetar | Cada par exige `CreateMap`; a configuração precisa ser registrada e validada |
| Mapeamento bidirecional | Automático | Exige `ReverseMap()` ou um segundo `CreateMap` |
| Superfície de aprendizado | Meia dúzia de extension methods e um builder | API grande: profiles, resolvers, converters, projections |
| Peso de dependência | Um pacote de abstractions | Pacote AutoMapper + infraestrutura de configuração |
| Ajustes por chamada | Builder fluente no ponto de uso | Configuração é global; ajustes por chamada são desajeitados |

### Onde o AutoMapper (ou outro mapper) é melhor escolha

| Necessidade | Por que o SimpleMapper.Net não é a ferramenta |
| --- | --- |
| Projeção `IQueryable` (`ProjectTo`) para SQL | Não suportado — o SimpleMapper.Net mapeia apenas objetos em memória |
| Convenções de flattening (`Account.Username -> AccountUsername`) | Não suportado — nomes precisam bater, ou ser renomeados explicitamente por chamada |
| Value resolvers / type converters customizados | Não suportado — lógica de transformação pertence ao seu código antes/depois do mapeamento |
| Mapeamento via construtor com validação | O SimpleMapper.Net pula construtores em tipos sem construtor sem parâmetros |
| Mappers gerados em compile time (zero reflection em runtime) | Considere source generators no estilo Mapperly |
| Validação de configuração na inicialização (`AssertConfigurationIsValid`) | Não há configuração para validar — typos em strings de `Map`/`Ignore` aparecem em runtime |

### Limitações conhecidas

- Grafos de objetos cíclicos não são seguidos — lançam `MappingDepthExceededException` (ver acima), não são resolvidos em grafos DTO cíclicos.
- Dicionários são copiados **por referência**, não clonados.
- Paths profundos de `Map`/`Ignore` exigem que source e target tenham a mesma profundidade.
- Sem suporte a mapear sobre uma instância target existente (`Map(source, destination)`).
- Structs como source/target não são cenário de primeira classe (o foco é classe-para-classe).
- O path de debug (`WithDebugLogging`) é lento e aloca por design; nunca o deixe ligado em código de produção.

## Performance

Medido com BenchmarkDotNet contra o AutoMapper 14.0.0 (última versão MIT) sobre um grafo sintético de plataforma de conteúdo (~60 propriedades mapeadas, 4-5 níveis de aninhamento, coleções, dicionário, um item polimórfico). Metodologia completa, ambiente e reprodução: [benchmarks.md](benchmarks.md).

<!-- BENCHMARK-SUMMARY:START -->
Execução containerizada (container Ubuntu Arm64, .NET 10.0.9, 1 CPU / 2 GB):

| Cenário | AutoMapper 14.0.0 | SimpleMapper.Net |
| --- | --- | --- |
| Mapeamento único (forward) | 1.584 us / 5.14 KB | 2.085 us / 5.59 KB |
| Mapeamento único (reverso) | 1.521 us / 5.14 KB | 2.100 us / 5.59 KB |
| Batch x100 (forward) | 222.3 us / 514.9 KB | 241.6 us / 560.2 KB |

Mesma ordem de grandeza em todos os cenários: throughput de batch dentro de ~9%, mapeamentos únicos cerca de meio microssegundo a mais por chamada neste grafo.
<!-- BENCHMARK-SUMMARY:END -->

Rode você mesmo, com recursos fixados, em um comando:

```bash
docker compose -f docker-compose.benchmarks.yml up --build
```

## Documentação

- [Arquitetura e internals](architecture.md)
- [Benchmarks: metodologia e resultados](benchmarks.md)
- Original em inglês: [README.md](../../README.md), [docs/](../)

## Contribuindo

Contribuições são bem-vindas, e você não precisa ser especialista em mapeamento para ajudar. Relatos de bug, reproduções com teste que falha, correções de documentação, novos exemplos e features — tudo faz o projeto avançar. Se não tiver certeza se uma ideia se encaixa na [filosofia](#filosofia), abra uma issue primeiro e vamos conversar.

**Contribuições com auxílio de IA são bem-vindas** — todo mundo usa essas ferramentas hoje, então use o que te ajudar. Só aplique o bom senso: você é dono do que envia, e isso precisa passar pela mesma régua de qualquer outra mudança (testes, estilo, escopo, e um entendimento real do que o código faz).

### Primeiros passos

```bash
git clone https://github.com/giacomeli/SimpleMapper
cd SimpleMapper
dotnet build SimpleMapper.Net.slnx -c Release   # precisa estar sem warnings
dotnet test SimpleMapper.Net.slnx               # precisa estar verde
```

### Requisitos para um pull request

- **Testes passam.** `dotnet test` verde e o build em Release com zero warnings.
- **Teste primeiro.** Novas features e correções vêm com testes, escritos antes da implementação. Uma correção de bug deve incluir um teste que falha sem a sua mudança e passa com ela.
- **API pública documentada.** Todo tipo e membro público carrega um comentário XML doc (o build exige isso).
- **Inglês, com espelhos pt-BR.** Código, comentários, mensagens de exceção e a documentação principal são em inglês. Se você mudar comportamento ou a API pública, atualize `README.md` / `docs/` e espelhe em `docs/pt-br/`.
- **Mudanças de performance vêm com benchmarks.** Qualquer PR que alegue melhoria de tempo ou alocação — ou que toque no engine ou no fast path — deve incluir números antes/depois da execução containerizada (`docker compose -f docker-compose.benchmarks.yml up --build`). "Parece mais rápido" não é um benchmark.
- **Tamanho razoável.** Mantenha os pull requests pequenos e revisáveis. PRs muito grandes ou espalhados não serão aceitos — divida em pedaços focados.
- **Commits.** Conventional Commits em inglês (`feat:`, `fix:`, `refactor:`, `docs:`, `chore:`, `perf:`, `test:`), modo imperativo. Usar IA para ajudar a escrever a mudança é ok; a mensagem de commit em si fica sem atribuição de IA ou trailers `Co-Authored-By`.

### Boas práticas

- **Mantenha o caso comum no fast path.** O caminho de mapeamento sem configuração é onde vive a performance. Se você tocar em `MapperEngine` ou nos caches, rode os benchmarks (`docker compose -f docker-compose.benchmarks.yml up --build`) e confirme que não há regressão. Veja [architecture.md](architecture.md) para entender os paths de execução e o check `useFast`.
- **Fique no escopo.** Não reformate código não relacionado e siga o estilo ao redor.
- **Proteja a filosofia.** Uma feature que complica o caso comum, ou puxa a lib para "fazer tudo", provavelmente será recusada — isso é intencional. Na dúvida, proponha numa issue antes de escrever código.
- **Discuta mudanças maiores antes.** Mudanças na API pública, novas dependências e qualquer coisa que toque no registro de subtipos (`SMEXP001`) são melhor combinadas numa issue.

## Publicação (mantenedores)

```bash
dotnet pack src/SimpleMapper.Net/SimpleMapper.Net.csproj -c Release
dotnet nuget push src/SimpleMapper.Net/bin/Release/SimpleMapper.Net.1.0.0.nupkg \
    --source https://api.nuget.org/v3/index.json --api-key <API_KEY>
```

## Licença

[MIT](../../LICENSE) — Juliano Giacomeli
