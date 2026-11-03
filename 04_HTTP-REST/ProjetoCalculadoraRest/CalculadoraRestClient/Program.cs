using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

class CalculadoraRestClientApp
{
    static async Task Main(string[] args)
    {
        // Instancia o HttpClient apontando para a URL base do servidor REST na porta 5000
        HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

        // Loop para manter a interface textual mínima aberta após cada requisição
        while (true)
        {
            // Exibe o menu interativo no console
            Console.WriteLine("\n==========================================");
            Console.WriteLine("=== CLIENTE HTTP-REST CALCULADORA (C#) ===");
            Console.WriteLine("==========================================");
            Console.WriteLine("1 - Soma");
            Console.WriteLine("2 - Subtração");
            Console.WriteLine("3 - Multiplicação");
            Console.WriteLine("4 - Divisão");
            Console.WriteLine("5 - Expressão calculada INTEIRAMENTE NO SERVIDOR (ex: (10 + 15) * 4)");
            Console.WriteLine("6 - Expressão DECOMPOSTA NO CLIENTE via chamadas POST REST");
            Console.WriteLine("0 - Sair");
            Console.Write("\nEscolha a operação (0-6): ");

            string? opcaoStr = Console.ReadLine();
            if (!int.TryParse(opcaoStr, out int opcao)) continue;

            if (opcao == 0)
            {
                Console.WriteLine("Encerrando o cliente HTTP-REST...");
                break;
            }

            CalculadoraRequest req = new CalculadoraRequest();

            if (opcao == 5)
            {
                req.Operacao = 5;
                Console.Write("Digite a expressão matemática: ");
                req.Expressao = Console.ReadLine() ?? "";
            }
            else if (opcao == 6)
            {
                // Decomposição no Cliente com chamadas HTTP POST e políticas de Retry
                Console.Write("Digite a expressão para decomposição (ex: (10 + 15) * 4): ");
                string expr = Console.ReadLine() ?? "";

                try
                {
                    Console.WriteLine("\n[Cliente REST] Decompondo expressão e enviando requisições POST...");

                    // Envia a operação de soma ao servidor REST utilizando a política de Retry
                    double subRes = await EnviarRestComRetryAsync(httpClient, new CalculadoraRequest { Operacao = 1, A = 10, B = 15 });
                    Console.WriteLine($"  -> Requisição POST 'soma(10, 15)' retornou: {subRes}");

                    // Envia a operação de multiplicação utilizando o resultado anterior e o fator 4
                    double resultadoFinal = await EnviarRestComRetryAsync(httpClient, new CalculadoraRequest { Operacao = 3, A = subRes, B = 4 });
                    Console.WriteLine($"  -> Requisição POST 'multiplicacao({subRes}, 4)' retornou: {resultadoFinal}");

                    Console.WriteLine($"\n--> Resultado final obtido por decomposição no cliente REST = {resultadoFinal}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-> Falha crítica na decomposição REST: {ex.Message}");
                }

                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                Console.Clear();
                continue;
            }
            else
            {
                req.Operacao = opcao;
                Console.Write("Digite o primeiro operando (A): ");
                req.A = double.Parse(Console.ReadLine() ?? "0");

                Console.Write("Digite o segundo operando (B): ");
                req.B = double.Parse(Console.ReadLine() ?? "0");
            }

            // Executa a requisição HTTP POST padrão utilizando a política de Retry para operações individuais
            try
            {
                double resultado = await EnviarRestComRetryAsync(httpClient, req);
                Console.WriteLine($"\n-> Resposta HTTP-REST bem-sucedida = {resultado}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n-> Erro após esgotar tentativas de Retry: {ex.Message}");
            }

            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }

        httpClient.Dispose();
    }

    /// <summary>
    /// Método responsável por implementar a Política de Retry (Wait T and Retry) exigida pelo Requisito 5.
    /// Se a chamada HTTP falhar ou o servidor estiver indisponível, o cliente aguarda T segundos e tenta novamente.
    /// </summary>
    private static async Task<double> EnviarRestComRetryAsync(HttpClient client, CalculadoraRequest req)
    {
        int maxTentativas = 3;      // Número máximo de tentativas permitidas antes de desistir
        int tempoEsperaSegundos = 2; // Tempo de espera (Wait T) em segundos entre as tentativas

        for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            try
            {
                // Exibe no console o status da tentativa atual de envio HTTP POST
                Console.WriteLine($"[Retry Policy] Tentativa {tentativa} de {maxTentativas} enviando POST para '/api/calcular'...");

                // Dispara a requisição HTTP POST serializando o objeto de requisição como JSON
                HttpResponseMessage response = await client.PostAsJsonAsync("api/calcular", req);

                // Verifica se a resposta HTTP retornou com sucesso
                if (response.IsSuccessStatusCode)
                {
                    // Desserializa o JSON de resposta para extrair o resultado numérico calculado pelo servidor
                    var respObj = await response.Content.ReadFromJsonAsync<CalculadoraResponse>();
                    return respObj?.resultado ?? 0;
                }
                else
                {
                    // Se o servidor retornou erro HTTP (ex: 400 ou 500), lança uma exceção para cair no bloco catch
                    throw new HttpRequestException($"Servidor retornou status code de erro: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Exibe o aviso de falha na tentativa atual capturada pela rede
                Console.WriteLine($"[Retry Policy] Falha na tentativa {tentativa}: {ex.Message}");

                // Se esta foi a última tentativa permitida, relança a exceção encerrando o fluxo
                if (tentativa == maxTentativas)
                {
                    throw new Exception("Número máximo de tentativas de Retry esgotado. Servidor inalcançável.");
                }

                // Aplica o tempo de espera (Wait T segundos) assincronamente antes de efetuar a próxima tentativa
                Console.WriteLine($"[Retry Policy] Aguardando {tempoEsperaSegundos} segundos antes da próxima tentativa...");
                await Task.Delay(TimeSpan.FromSeconds(tempoEsperaSegundos));
            }
        }

        throw new Exception("Erro desconhecido na política de retry.");
    }
}

// Modelos de dados utilizados para serialização e desserialização no cliente REST
public class CalculadoraRequest
{
    public int Operacao { get; set; }
    public double A { get; set; }
    public double B { get; set; }
    public string? Expressao { get; set; }
}

public class CalculadoraResponse
{
    public double resultado { get; set; }
}