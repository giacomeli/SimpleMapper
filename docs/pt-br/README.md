# SimpleMapper.Net

> Tradução em português brasileiro. O documento canônico é o [README.md](../../README.md) em inglês.

Um mapeador objeto-objeto simples e de configuração zero para .NET. Mapeamento por convenção de nomes com expression trees compiladas, builder fluente para ajustes por chamada e uma única dependência leve (`Microsoft.Extensions.DependencyInjection.Abstractions`).

O SimpleMapper.Net é um mapeador de objetos para .NET opinativo e sob licença MIT, que otimiza para uma coisa: tornar o caso comum de mapeamento sem esforço. É uma alternativa open source deliberadamente mais simples que o AutoMapper — ele não tenta resolver tudo que o AutoMapper resolve, e esse é justamente o ponto.

## Filosofia

O SimpleMapper.Net existe para melhorar a experiência do desenvolvedor no dia a dia de mapear objetos, não para ser um framework de mapeamento completo em features. O design é opinativo:

- **Simplicidade primeiro.** O caso comum — copiar um DTO, ignorar um campo, renomear outro, mapear um grafo aninhado — deve precisar de zero configuração e ler como código comum. Se uma feature complicaria esse caminho, ela fica de fora.
- **Feito para fronteiras de DTO.** O uso pretendido é DTO para DTO e entidade para DTO. Mapear *para dentro* de entidades ricas de domínio é possível, mas é uma troca consciente de encapsulamento por conveniência: o mapper escreve através de setters não-públicos, e criar targets sem construtor sem parâmetros exige opt-in explícito (ver [Construção de objetos](#construção-de-objetos)).
- **Performance na mesma ordem de grandeza.** O mapeamento roda por expression trees compiladas. Isso é mais lento que código escrito à mão ou um source generator, e próximo do AutoMapper — cerca de meio microssegundo a mais por chamada num grafo de 60 propriedades (ver [Benchmarks](benchmarks.md)). A simplicidade vence os empates, mas a performance nunca é uma reflexão tardia.
- **Pequeno de propósito.** Sem projeções, sem convenções de flattening, sem pipeline de resolvers/converters, sem configuração em runtime para validar. Menos conceitos para aprender, menos formas de errar. Quando você realmente precisa disso, um mapper mais pesado é a ferramenta certa — veja os [trade-offs](#onde-o-automapper-ou-outro-mapper-é-melhor-escolha) abaixo.
- **JIT-first por design.** O público-alvo são aplicações JIT tradicionais — APIs, monólitos, serviços de background — que valorizam produtividade com configuração zero. O código de mapeamento é construído em runtime, então o SimpleMapper.Net não é a ferramenta para deployments NativeAOT ou trimmed; use um source generator como o Mapperly nesses casos. A API pública carrega `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`, então projetos AOT/trimmed recebem um warning em tempo de compilação em vez de uma surpresa em runtime.

Se você quer um mapper que faz tudo, não é este. Se você quer que o mapeamento dos seus DTOs saia do seu caminho, é este.

## Recursos

- **Zero configuração** para o caso comum: propriedades e campos públicos com nomes iguais são copiados automaticamente, incluindo objetos aninhados, coleções e dicionários.
- **Totalmente dinâmico**: sem profiles, sem `CreateMap`, sem registro na inicialização. Os caches compilam de forma lazy no primeiro uso.
- **Bidirecional por natureza**: `User -> UserDto` e `UserDto -> User` funcionam sem nenhum setup.
- **Builder fluente** para ajustes por chamada: ignorar propriedades, renomear propriedades, navegar em paths profundos com type safety via lambdas e `Each()`.
- **Mapear sobre instância existente**: `dto.MapTo(entity)` aplica um DTO sobre um objeto que você já tem — mantenha o DTO plano para entidades rastreadas do EF Core (ver [Atualizando entidades rastreadas do EF Core](#atualizando-entidades-rastreadas-do-ef-core)).
- **Profundo por padrão**: o objeto mapeado nunca compartilha referências com o grafo de origem — objetos aninhados e itens de coleção são instâncias novas, mesmo quando os tipos de origem e destino são idênticos (dicionários são a exceção documentada).
- **Falha alto**: membros não mapeáveis lançam `MappingException` com o nome da propriedade e os dois tipos — sem skips silenciosos, sem structs zeradas, sem erros crus de expression tree.
- **Thread-safe**: todos os caches são lookups lock-free em `ConcurrentDictionary` após o primeiro uso.
- **Rápido, medido com honestidade**: expression trees compiladas mantêm mapeamentos únicos na mesma ordem de grandeza do AutoMapper — cerca de meio microssegundo a mais por chamada num grafo de 60 propriedades. Código escrito à mão e source generators são mais rápidos; todos os números estão publicados em [Benchmarks](benchmarks.md).
- **Debug logging**: imprime a árvore completa de mapeamento no console — ou em qualquer `TextWriter` — para diagnosticar um mapeamento.

## Instalação

```bash
dotnet add package SimpleMapper.Net
```

Requer .NET 8.0 ou superior. O pacote é multi-target `net8.0` e `net10.0` (um projeto .NET 9 resolve o ativo `net8.0` via NuGet).

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

// Mapeia sobre uma instância existente (membros sem correspondência mantêm seus valores)
dto.MapTo(existingUser);
```

Para updates de entidades rastreadas do EF Core, veja [Atualizando entidades rastreadas do EF Core](#atualizando-entidades-rastreadas-do-ef-core) — o DTO precisa ficar plano.

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

// Ou enviar a árvore para qualquer TextWriter (texto puro) — usável em testes e logs de servidor
var writer = new StringWriter();
var dto = user.Map()
    .WithDebugLogging(writer)
    .To<UserDto>();

// Aplicar o mapeamento configurado sobre uma instância existente
user.Map()
    .Ignore("InternalNotes")
    .To(existingDto);

// Permitir targets sem construtor sem parâmetros (apenas nesta chamada) — ver
// "Construção de objetos" abaixo para o que isso abre mão
var record = user.Map()
    .AllowUninitializedObjects()
    .To<UserRecordDto>();
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
- Propriedades públicas são escritas independentemente da visibilidade do setter: acessores `private set`, `protected set` e `init` são todos preenchidos. Isso é intencional — é o mesmo mecanismo que preenche membros init-only de records — mas também significa que mapear para dentro de entidades ricas de domínio contorna o encapsulamento delas (ver a [Filosofia](#filosofia)).

### Construção de objetos

Targets com construtor sem parâmetros — qualquer visibilidade: público, protected ou private — são criados por ele. Targets sem esse construtor (records posicionais, entidades com argumentos obrigatórios no construtor) são **recusados por default**: o mapeamento lança `MappingException` nomeando o tipo, porque criar uma instância sem executar o construtor pula a lógica do construtor, invariantes de domínio e inicializadores de campo.

Se você aceita esse trade-off — típico para records posicionais usados como DTOs — faça o opt-in explícito:

```csharp
// Por chamada: apenas este mapeamento, incluindo objetos aninhados e itens de coleção
var dto = user.Map()
    .AllowUninitializedObjects()
    .To<UserRecordDto>();

// Global: todos os mapeamentos do processo (configure na inicialização)
SimpleMapperOptions.ObjectConstruction = ObjectConstructionMode.AllowUninitializedObjects;
```

Sob o opt-in, targets sem construtor são criados com `RuntimeHelpers.GetUninitializedObject` e preenchidos membro a membro — nenhum construtor executa e inicializadores de campo são pulados. Leve isso em conta para tipos cujos construtores validam.

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

## Atualizando entidades rastreadas do EF Core

`dto.MapTo(entity)` é conveniente para aplicar um DTO de request sobre uma entidade rastreada, mas mapeamento profundo e change tracking interagem mal: objetos aninhados e coleções do destino são **substituídos por instâncias novas**, nunca mesclados. Num grafo de entidade rastreado isso significa filhos órfãos, linhas re-inseridas e fixup de navegação quebrado.

Mantenha o DTO **plano** (apenas escalares) ao atualizar entidades rastreadas, e ignore explicitamente membros de identidade, auditoria e concorrência:

```csharp
public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    // apenas escalares — sem propriedades de navegação, sem coleções
}

var entity = await db.Users.FindAsync(id);

dto.Map()
    .Ignore(nameof(User.Id))         // identidade
    .Ignore(nameof(User.CreatedAt))  // auditoria
    .Ignore(nameof(User.RowVersion)) // token de concorrência otimista
    .To(entity);

await db.SaveChangesAsync();
```

Se o update carrega objetos aninhados ou coleções, mapeie esses membros à mão — não faça deep-map sobre um grafo rastreado.

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
| Mapeamento via construtor com validação | Não suportado — targets sem construtor sem parâmetros são recusados por default; o opt-in explícito os cria sem executar o construtor |
| Mappers gerados em compile time (zero reflection em runtime) | Considere source generators no estilo Mapperly |
| Validação de configuração na inicialização (`AssertConfigurationIsValid`) | Não há configuração para validar — typos em strings de `Map`/`Ignore` aparecem em runtime |

### O que mapeia, o que lança

O SimpleMapper.Net prefere um erro alto e nomeado a dados silenciosamente errados. A matriz de suporte:

| Par de membros | Comportamento |
| --- | --- |
| Mesmo tipo simples (primitivos, string, decimal, enums, Guid, DateTime/DateOnly/TimeOnly, TimeSpan, Uri, Version) | Cópia direta |
| `T` <-> `T?` e alargamento numérico (`int -> long`, `float -> double`) | Convertido |
| Tipos simples incompatíveis (`string -> double`, `int -> string`, `string -> enum`) | Lança `MappingException` com o nome do membro e os dois tipos |
| Objeto aninhado, tipos diferentes ou idênticos | Mapeado profundo (instância nova; o DTO nunca compartilha referências com a origem) |
| Propriedade pública com setter `private`/`protected`/`init` | Escrita (intencional — ver [Null safety e instanciação](#null-safety-e-instanciação)) |
| Tipo target sem construtor sem parâmetros | Lança `MappingException` por default; criado uninitialized sob o [opt-in `AllowUninitializedObjects`](#construção-de-objetos) |
| Coleção para `T[]`, `List<T>` ou qualquer interface que um `List<T>` satisfaça (`IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, ...) | Mapeada profundo, item a item |
| Coleção para `HashSet<T>`, coleções imutáveis, coleções não genéricas (`ArrayList`) | Lança `MappingException` na construção do plano |
| Dicionário | Copiado **por referência** (exceção documentada; não clonado) |
| Membro tipado como `object`, delegate | Copiado por referência (a forma do destino é desconhecida / não instanciável) |
| **Tipo target** struct (`MapTo<AlgumaStruct>()`) | Lança `NotSupportedException` |
| **Propriedade** struct, tipos idênticos | Cópia por valor |
| Propriedade struct, tipos diferentes | Lança `MappingException` |

### Limitações conhecidas

- Grafos de objetos cíclicos não são seguidos — lançam `MappingDepthExceededException` (ver acima), não são resolvidos em grafos DTO cíclicos.
- Paths profundos de `Map`/`Ignore` exigem que source e target tenham a mesma profundidade.
- O path de debug (`WithDebugLogging`) é lento e aloca por design; nunca o deixe ligado em código de produção.
- **NativeAOT / trimming**: não suportado — o código de mapeamento é construído em runtime com reflection e expression trees compiladas (ver a nota JIT-first na [Filosofia](#filosofia)). A API pública é anotada com `[RequiresDynamicCode]` e `[RequiresUnreferencedCode]` (verificado pelo compilador via os analyzers de AOT/trim), então projetos AOT/trimmed recebem um warning em tempo de compilação; use um mapper com source generator como o Mapperly nesses cenários.

## Performance

Medido com BenchmarkDotNet contra uma baseline manual escrita à mão, Mapperly (source generator), AutoMapper 14.0.0 (última versão MIT) e Mapster, sobre um grafo sintético de plataforma de conteúdo (~60 propriedades mapeadas, 4-5 níveis de aninhamento, coleções, dicionário, um item polimórfico) mais cenários de DTO plano, map-into e cold start. O objetivo é paridade de ordem de grandeza com os mappers de runtime, não vitória — código manual e source generators são mais rápidos, e as tabelas dizem isso. Metodologia completa, ambiente e reprodução: [benchmarks.md](benchmarks.md).

<!-- BENCHMARK-SUMMARY:START -->
Execução containerizada (container Ubuntu Arm64, .NET 10.0.9, 1 CPU / 2 GB), v2.1.0:

| Grafo profundo: Blog -> BlogDto | Média | Alocado |
| --- | --- | --- |
| Manual (baseline) | 0.94 us | 4.95 KB |
| Mapperly 4.3.1 (source generator) | 0.90 us | 3.62 KB |
| Mapster 10.0.10 | 1.16 us | 4.93 KB |
| AutoMapper 14.0.0 | 1.62 us | 5.14 KB |
| SimpleMapper.Net | 2.23 us | 5.59 KB |

| Cenário | Manual | SimpleMapper.Net | AutoMapper 14.0.0 |
| --- | --- | --- | --- |
| DTO plano (8 escalares) | 9.8 ns | 55.8 ns | 51.2 ns |
| Map sobre instância existente (plano) | 4.7 ns | 29.0 ns | 57.1 ns |
| Cold start: primeiro mapeamento do grafo | sem etapa de build | 4.6 ms | 17.7 ms (config + map) |

O SimpleMapper.Net custa cerca de 0.6 us a mais por chamada que o AutoMapper neste grafo (2.4x um mapper escrito à mão), e é aproximadamente 2x mais rápido no map-into e 4x mais rápido no cold start. Se o seu projeto pode adotar um source generator, o Mapperly é mais rápido em todos os cenários — essa é a troca honesta: configuração zero em runtime versus codegen em tempo de compilação.
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

### Trusted Publishing (recomendado)

O nuget.org desencoraja API keys de longa duração. O repositório inclui um workflow do
GitHub Actions (`.github/workflows/publish.yml`) que usa **Trusted Publishing**: ele troca
um token OIDC de curta duração do GitHub por uma chave temporária do nuget.org (válida por
1 hora) no momento do push, de modo que nenhuma chave secreta fica armazenada.

Configuração única:

1. No nuget.org, abra **Account -> Trusted Publishing** e adicione uma política:
   - **Repository Owner:** `giacomeli`
   - **Repository:** `SimpleMapper`
   - **Workflow File:** `publish.yml` (apenas o nome do arquivo, sem caminho)
   - **Environment:** `release` (opcional; corresponde ao `environment:` do workflow)
2. Adicione um secret de repositório `NUGET_USER` com o **nome de perfil** do nuget.org
   (não o e-mail). É informação pública; o secret só mantém o workflow organizado.

Para publicar, crie uma Release no GitHub com uma tag como `v1.0.0` (o workflow deriva a
versão do pacote a partir da tag), ou dispare o workflow manualmente em **Actions -> Publish
to NuGet -> Run workflow**. O workflow roda a suíte de testes antes de empacotar e publicar,
e envia os símbolos `.snupkg` junto com o pacote.

### Manual (local, fallback)

Somente se você não puder usar CI. Requer uma API key criada manualmente:

```bash
dotnet pack src/SimpleMapper.Net/SimpleMapper.Net.csproj -c Release
dotnet nuget push src/SimpleMapper.Net/bin/Release/SimpleMapper.Net.1.0.0.nupkg \
    --source https://api.nuget.org/v3/index.json --api-key <API_KEY>
```

## Licença

[MIT](../../LICENSE) — Juliano Giacomeli
