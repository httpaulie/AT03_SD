using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

class CalculadoraJsonRpcClient
{
    static async Task Main(string[] args)
    {
        // Instancia o HttpClient configurando o endereço base para apontar para o servidor JSON-RPC local na porta 8080
        HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080/") };

        // Loop para manter a interface textual interativa aberta após cada requisição
        while (true)
        {
            // Exibe o menu interativo no console
            Console.WriteLine("\n==========================================");
            Console.WriteLine("=== CLIENTE JSON-RPC CALCULADORA (C#) ===");
            Console.WriteLine("==========================================");
            Console.WriteLine("1 - Somar");
            Console.WriteLine("2 - Subtrair");
            Console.WriteLine("3 - Multiplicar");
            Console.WriteLine("4 - Dividir");
            Console.WriteLine("5 - Expressão calculada INTEIRAMENTE NO SERVIDOR (ex: (10 + 15) * 4)");
            Console.WriteLine("6 - Expressão DECOMPOSTA NO CLIENTE via chamadas JSON-RPC");
            Console.WriteLine("0 - Sair");
            Console.Write("\nEscolha a operação (0-6): ");

            string? opcao = Console.ReadLine();
            if (opcao == "0")
            {
                Console.WriteLine("Encerrando o cliente JSON-RPC...");
                break;
            }

            // Instancia o objeto de requisição JSON-RPC que será enviado via HTTP
            RpcRequest req = new RpcRequest();

            if (opcao == "5")
            {
                // Configura o método para avaliação completa no servidor
                req.Method = "calcular_expressao";
                Console.Write("Digite a expressão matemática: ");
                req.Expression = Console.ReadLine() ?? "";
            }
            else if (opcao == "6")
            {
                // Decomposição no Cliente com chamadas básicas ao servidor JSON-RPC
                Console.Write("Digite a expressão para decomposição (ex: (10 + 15) * 4): ");
                string expr = Console.ReadLine() ?? "";

                try
                {
                    Console.WriteLine("\n[Cliente] Decompondo expressão e invocando métodos remotos básicos...");

                    // Invoca remotamente o método JSON-RPC "somar" para o bloco entre parênteses
                    double subRes = await InvocarRpcAsync(httpClient, "somar", 10, 15);
                    Console.WriteLine($"  -> Chamada RPC 'somar(10, 15)' retornou: {subRes}");

                    // Invoca remotamente o método JSON-RPC "multiplicar" usando o resultado intermediário e o fator 4
                    double resultadoFinal = await InvocarRpcAsync(httpClient, "multiplicar", subRes, 4);
                    Console.WriteLine($"  -> Chamada RPC 'multiplicar({subRes}, 4)' retornou: {resultadoFinal}");

                    Console.WriteLine($"\n--> Resultado final obtido por decomposição no cliente = {resultadoFinal}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-> Erro na decomposição RPC: {ex.Message}");
                }

                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
                Console.Clear();
                continue;
            }
            else
            {
                // Mapeia a opção numérica digitada para o nome correspondente do método JSON-RPC
                req.Method = opcao switch
                {
                    "1" => "somar",
                    "2" => "subtrair",
                    "3" => "multiplicar",
                    "4" => "dividir",
                    _ => ""
                };

                if (string.IsNullOrEmpty(req.Method))
                {
                    Console.WriteLine("Opção inválida!");
                    continue;
                }

                // Captura os valores dos operandos informados pelo usuário na interface textual
                Console.Write("Digite o primeiro operando (A): ");
                req.A = double.Parse(Console.ReadLine() ?? "0");

                Console.Write("Digite o segundo operando (B): ");
                req.B = double.Parse(Console.ReadLine() ?? "0");
            }

            try
            {
                Console.WriteLine("\n[Cliente] Enviando requisição HTTP POST JSON-RPC...");

                // Dispara a requisição HTTP POST serializando o objeto RpcRequest como JSON para o endpoint "/rpc" do servidor
                HttpResponseMessage httpResponse = await httpClient.PostAsJsonAsync("rpc", req);

                // Desserializa o corpo da resposta HTTP contendo o JSON retornado pelo servidor para a classe RpcResponse
                RpcResponse? rpcResp = await httpResponse.Content.ReadFromJsonAsync<RpcResponse>();

                // Verifica se o servidor retornou uma mensagem de erro encapsulada no protocolo JSON-RPC
                if (rpcResp != null && !string.IsNullOrEmpty(rpcResp.Error))
                {
                    Console.WriteLine($"-> Falha RPC Retornada pelo Servidor: {rpcResp.Error}");
                }
                else if (rpcResp != null)
                {
                    // Exibe o resultado numérico bem-sucedido devolvido pelo servidor remoto
                    Console.WriteLine($"-> Resposta remota bem-sucedida = {rpcResp.Result}");
                }
            }
            catch (Exception ex)
            {
                // Captura e exibe falhas de rede, queda de conexão ou indisponibilidade do servidor HTTP
                Console.WriteLine($"-> Erro de conexão com o servidor JSON-RPC: {ex.Message}");
            }

            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }

        // Libera os recursos alocados pelo HttpClient ao fechar o aplicativo
        httpClient.Dispose();
    }

    // Método auxiliar encapsulado para realizar chamadas JSON-RPC individuais (utilizado na Opção 6 de decomposição)
    private static async Task<double> InvocarRpcAsync(HttpClient client, string method, double a, double b)
    {
        // Cria um pacote de requisição JSON-RPC específico para a chamada remota
        var req = new RpcRequest { Method = method, A = a, B = b };

        // Envia o pacote serializado via HTTP POST para o endpoint "/rpc" do servidor
        HttpResponseMessage resp = await client.PostAsJsonAsync("rpc", req);

        // Desserializa a resposta HTTP recebida para o objeto RpcResponse
        RpcResponse? rpcResp = await resp.Content.ReadFromJsonAsync<RpcResponse>();

        // Se a resposta contiver um erro retornado pelo servidor, dispara uma exceção
        if (rpcResp?.Error != null) throw new Exception(rpcResp.Error);

        // Retorna o valor numérico do resultado obtido na chamada remota
        return rpcResp?.Result ?? 0;
    }
}

// Modelos de dados espelhados para serialização e desserialização no cliente
public class RpcRequest
{
    public string Method { get; set; } = "";
    public double A { get; set; }
    public double B { get; set; }
    public string? Expression { get; set; }
}

public class RpcResponse
{
    public double? Result { get; set; }
    public string? Error { get; set; }
}