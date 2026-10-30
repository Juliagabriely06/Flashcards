using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Spectre.Console;

class Program
{
    private const string OpEstudar = "Estudar (Cards Pendentes)";
    private const string OpAdicionar = "Adicionar Novo Card";
    private const string OpListar = "Listar Todos os Cards";
    private const string OpEstatisticas = "Estatísticas";
    private const string OpSair = "Sair";

    private const int NivelMaximo = 10;

    private static readonly string arquivoDados = "flashcards.json";
    private static List<FlashCard> flashcards = new List<FlashCard>();

    private static readonly JsonSerializerOptions opcoesJson = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Color[] paleta =
    {
        Color.Yellow, Color.Cyan1, Color.Green, Color.Fuchsia,
        Color.Orange1, Color.DodgerBlue1, Color.Red, Color.SpringGreen1
    };

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        CarregarDados();

        bool sair = false;
        while (!sair)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("FlashCards+").Color(Color.Cyan1));
            AnsiConsole.MarkupLine("[grey]Sistema de Estudo Ativo & Repetição Espaçada[/]\n");

            var escolha = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("O que você deseja fazer?")
                    .AddChoices(OpEstudar, OpAdicionar, OpListar, OpEstatisticas, OpSair));

            switch (escolha)
            {
                case OpEstudar:
                    EstudarCards();
                    break;
                case OpAdicionar:
                    AdicionarCard();
                    break;
                case OpListar:
                    ListarCards();
                    break;
                case OpEstatisticas:
                    MostrarEstatisticas();
                    break;
                case OpSair:
                    SalvarDados();
                    sair = true;
                    AnsiConsole.MarkupLine("[yellow]Até logo! Bons estudos![/]");
                    break;
            }
        }
    }

    public class FlashCard
    {
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public int Nivel { get; set; } = 1;
        public DateTime ProximaRevisao { get; set; } = DateTime.Now;

        [JsonIgnore]
        public bool Pendente => ProximaRevisao <= DateTime.Now;
    }

    private static void AdicionarCard()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]=== Novo Flashcard ===[/]\n");

        string tag = LerTexto("Digite a [green]Matéria / Tag[/] (ex: C#, Pedagogia):");
        string pergunta = LerTexto("Digite a [yellow]Pergunta[/]:");
        string resposta = LerTexto("Digite a [green]Resposta[/]:");

        tag = flashcards
            .Select(f => f.Tag)
            .FirstOrDefault(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) ?? tag;

        flashcards.Add(new FlashCard
        {
            Tag = tag,
            Pergunta = pergunta,
            Resposta = resposta,
            Nivel = 1,
            ProximaRevisao = DateTime.Now
        });

        SalvarDados();
        AnsiConsole.MarkupLine("\n[green]✓ Card cadastrado com sucesso![/]");
        Pausa();
    }

    private static void EstudarCards()
    {
        AnsiConsole.Clear();

        var pendentes = flashcards
            .Where(f => f.Pendente)
            .OrderBy(f => f.ProximaRevisao)
            .ToList();

        if (pendentes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]🎉 Nenhum card pendente para revisão hoje! Volte mais tarde ou adicione novos.[/]");
            Pausa();
            return;
        }

        const string Acertei = "Acertei / Fácil";
        const string Errei = "Errei / Difícil";
        const string Encerrar = "Encerrar sessão";

        int acertos = 0, erros = 0;

        for (int i = 0; i < pendentes.Count; i++)
        {
            var card = pendentes[i];

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[grey]Card {i + 1} de {pendentes.Count}[/]\n");
            AnsiConsole.Write(new Panel(new Markup($"[bold yellow]{Markup.Escape(card.Pergunta)}[/]"))
            {
                Header = new PanelHeader($" Matéria: {Markup.Escape(card.Tag)} | Nível: {card.Nivel} "),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 2)
            });

            AnsiConsole.MarkupLine("\n[grey]Pressione [bold]ENTER[/] para ver a resposta...[/]");
            AguardarEnter();

            AnsiConsole.Write(new Panel(new Markup($"[bold green]{Markup.Escape(card.Resposta)}[/]"))
            {
                Header = new PanelHeader(" Resposta "),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 2)
            });

            var avaliacao = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Como foi sua lembrança?")
                    .AddChoices(Errei, Acertei, Encerrar));

            if (avaliacao == Encerrar) break;

            if (avaliacao == Acertei)
            {
                card.Nivel = Math.Min(card.Nivel + 1, NivelMaximo);
                int diasAdd = (int)Math.Pow(2, card.Nivel - 2);
                card.ProximaRevisao = DateTime.Now.AddDays(diasAdd);
                acertos++;
                AnsiConsole.MarkupLine($"[green]✔ Muito bem! Próxima revisão em {diasAdd} dia(s).[/]");
            }
            else
            {
                card.Nivel = Math.Max(1, card.Nivel - 1);
                card.ProximaRevisao = DateTime.Now.AddMinutes(1);
                erros++;
                AnsiConsole.MarkupLine("[red]✖ Sem problemas! Vamos revisar isso em breve.[/]");
            }

            SalvarDados();
            Thread.Sleep(1200);
        }

        AnsiConsole.MarkupLine($"\n[cyan]Sessão encerrada![/] Acertos: {acertos} | Erros: {erros}");
        Pausa();
    }

    private static void ListarCards()
    {
        AnsiConsole.Clear();

        if (flashcards.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum card cadastrado ainda.[/]");
        }
        else
        {
            var tabela = new Table().Border(TableBorder.Rounded);
            tabela.AddColumn("Tag");
            tabela.AddColumn("Pergunta");
            tabela.AddColumn("Nível");
            tabela.AddColumn("Próxima Revisão");

            foreach (var card in flashcards.OrderBy(f => f.Tag).ThenBy(f => f.ProximaRevisao))
            {
                string pergunta = card.Pergunta.Length > 40
                    ? card.Pergunta.Substring(0, 37) + "..."
                    : card.Pergunta;

                string revisao = card.Pendente
                    ? "[green]Pendente[/]"
                    : card.ProximaRevisao.ToString("dd/MM/yyyy HH:mm");

                tabela.AddRow(
                    $"[cyan]{Markup.Escape(card.Tag)}[/]",
                    Markup.Escape(pergunta),
                    card.Nivel.ToString(),
                    revisao
                );
            }

            AnsiConsole.Write(tabela);
        }

        Pausa();
    }

    private static void MostrarEstatisticas()
    {
        AnsiConsole.Clear();

        int total = flashcards.Count;
        int pendentes = flashcards.Count(f => f.Pendente);

        AnsiConsole.Write(new FigletText("Stats").Color(Color.Green));
        AnsiConsole.MarkupLine($"[bold]Total de Cards:[/] {total}");
        AnsiConsole.MarkupLine($"[bold]Pendentes para hoje:[/] {pendentes}\n");

        if (total > 0)
        {
            var grupos = flashcards
                .GroupBy(f => f.Tag, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ToList();

            var chart = new BreakdownChart().Width(60).ShowTagValues();

            for (int i = 0; i < grupos.Count; i++)
            {
                string rotulo = grupos[i].Key.Replace('[', '(').Replace(']', ')');
                chart.AddItem(rotulo, grupos[i].Count(), paleta[i % paleta.Length]);
            }

            AnsiConsole.MarkupLine("[bold]Distribuição por Matéria/Tag:[/]");
            AnsiConsole.Write(chart);
        }

        Pausa();
    }

    private static string LerTexto(string titulo)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(titulo)
                .Validate(t => string.IsNullOrWhiteSpace(t)
                    ? ValidationResult.Error("[red]O campo não pode ficar vazio.[/]")
                    : ValidationResult.Success()))
            .Trim();
    }

    private static void AguardarEnter()
    {
        while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
    }

    private static void Pausa()
    {
        AnsiConsole.MarkupLine("\n[grey]Pressione qualquer tecla para voltar...[/]");
        Console.ReadKey(true);
    }

    private static void SalvarDados()
    {
        string json = JsonSerializer.Serialize(flashcards, opcoesJson);
        string temporario = arquivoDados + ".tmp";

        File.WriteAllText(temporario, json);
        File.Move(temporario, arquivoDados, true);
    }

    private static void CarregarDados()
    {
        if (!File.Exists(arquivoDados)) return;

        try
        {
            string json = File.ReadAllText(arquivoDados);
            flashcards = JsonSerializer.Deserialize<List<FlashCard>>(json, opcoesJson) ?? new List<FlashCard>();
        }
        catch (JsonException)
        {
            File.Copy(arquivoDados, arquivoDados + ".bak", true);
            AnsiConsole.MarkupLine("[red]⚠ O arquivo flashcards.json estava inválido. Uma cópia foi salva em flashcards.json.bak.[/]");
            Thread.Sleep(2500);
            flashcards = new List<FlashCard>();
        }
    }
}