# FlashCards

![.NET](https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-console-239120?logo=csharp&logoColor=white)
![Spectre.Console](https://img.shields.io/badge/UI-Spectre.Console-00B8D4)

Aplicativo de linha de comando para **estudo ativo com repetição espaçada**. Cadastre flashcards por matéria, revise só o que está na hora de rever e acompanhe o progresso, tudo pelo terminal e com os dados salvos localmente em JSON.

## Funcionalidades

- **Menu interativo** navegável pelas setas do teclado: Estudar, Adicionar Card, Listar Cards, Estatísticas e Sair.
- **Repetição espaçada:** só aparecem para estudo os cards cuja próxima revisão já venceu.
- **Revelação em dois passos:** a pergunta aparece em um painel, e `ENTER` revela a resposta em outro.
- **Cadastro por tag/matéria** (ex.: C#, Pedagogia, História), com escolha entre tags já existentes ou criação de uma nova.
- **Listagem em tabela** com Tag, Pergunta, Nível e Próxima Revisão.
- **Estatísticas:** total de cards, pendências do dia e gráfico de barras com a quantidade de cards por tag.
- **Salvamento automático** a cada novo cadastro e a cada card revisado.

## Tecnologias

| Item | Uso |
| --- | --- |
| C# / .NET 8 ou superior | Linguagem e plataforma (Console Application) |
| [Spectre.Console](https://spectreconsole.net/) | Menus, painéis, tabelas e gráfico no terminal |
| `System.Text.Json` | Leitura e gravação do arquivo `flashcards.json` |

## Pré-requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download) ou superior (confira com `dotnet --version`)
- Um terminal com suporte a UTF-8, como o Windows Terminal, o terminal do VS Code ou o de Linux/macOS

## Como executar

```bash
git clone https://github.com/seu-usuario/flashcards.git
cd flashcards
dotnet restore
dotnet run
```

Para criar o projeto do zero a partir do `Program.cs`:

```bash
dotnet new console -n Flashcards
cd Flashcards
dotnet add package Spectre.Console
# substitua o Program.cs gerado pelo deste repositório
dotnet run
```

> O programa precisa rodar em um terminal interativo. Em ambientes sem terminal de verdade, como alguns compiladores online, os menus não funcionam.

## Como usar

| Opção | O que faz |
| --- | --- |
| **Estudar (Cards Pendentes)** | Mostra um card por vez. `ENTER` revela a resposta, e você avalia como *Errei / Difícil*, *Acertei / Fácil* ou *Encerrar sessão*. |
| **Adicionar Novo Card** | Pede tag, pergunta e resposta. Campos vazios são recusados. |
| **Listar Todos os Cards** | Exibe todos os cards em uma tabela, ordenados por tag. Cards vencidos aparecem como *Pendente*. |
| **Estatísticas** | Mostra o total, as pendências de hoje e o gráfico de cards por tag. |
| **Sair** | Salva e encerra o programa. |

## Como funciona a repetição espaçada

Todo card nasce no **nível 1** e já fica disponível para estudo.

- **Acertei / Fácil:** o nível sobe em 1 e a próxima revisão é agendada em `2^(nível - 2)` dias.
- **Errei / Difícil:** o nível cai em 1 (mínimo 1) e a revisão é reagendada para **daqui a 1 minuto**.
- O nível máximo é **10**, para os intervalos não crescerem sem limite.

| Nível após acertar | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Próxima revisão (dias) | 1 | 2 | 4 | 8 | 16 | 32 | 64 | 128 | 256 |

Um card errado volta a ficar pendente depois de 1 minuto, mas só reaparece na próxima vez que você escolher *Estudar*, e não na mesma sessão.

## Persistência dos dados

Os cards ficam no arquivo `flashcards.json`, criado automaticamente na pasta de onde o programa é executado. Exemplo:

```json
[
  {
    "Pergunta": "O que é uma interface em C#?",
    "Resposta": "Um contrato que define membros que uma classe deve implementar.",
    "Tag": "C#",
    "Nivel": 3,
    "ProximaRevisao": "2026-11-01T14:30:00"
  }
]
```

Detalhes de segurança dos dados:

- O salvamento é feito em um arquivo temporário e só depois substitui o original, o que evita corromper o JSON se algo falhar no meio da gravação.
- Se o `flashcards.json` estiver inválido ao abrir, o programa guarda uma cópia em `flashcards.json.bak` e começa com a lista vazia, em vez de fechar com erro.
- Tags que só diferem em maiúsculas e minúsculas (como `c#` e `C#`) são unificadas.
- Textos com colchetes, como `arr[0]`, são exibidos normalmente.

## Estrutura do projeto

```
Flashcards/
├── Program.cs          # menu, telas, modelo FlashCard e persistência
├── Flashcards.csproj   # configuração do projeto
└── flashcards.json     # gerado ao cadastrar o primeiro card
```

## Problemas comuns

- **`MSB3027` / arquivo em uso ao rodar `dotnet run`:** há uma instância anterior do programa aberta. Feche-a ou rode `Stop-Process -Name Flashcards -Force` no PowerShell. Se o projeto estiver numa pasta sincronizada (como o OneDrive), mova-o para uma pasta fora dela.
- **`The type or namespace name 'Spectre' could not be found`:** falta o pacote. Rode `dotnet add package Spectre.Console`.
- **Emojis aparecendo como `?`:** use o Windows Terminal ou o terminal do VS Code em vez do `cmd.exe` antigo.

## Ideias para evoluir

- Editar e excluir cards
- Refazer na mesma sessão os cards que foram errados
- Importar e exportar cards em CSV
- Separar a regra de agendamento em uma classe própria e cobrir com testes (xUnit)
- Gravar as datas em UTC para evitar diferenças com mudança de fuso ou horário de verão
