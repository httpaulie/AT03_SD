using Microsoft.AspNetCore.Mvc;
using System.Data;

// Cria o construtor da aplicação Web API do ASP.NET Core para gerenciar o servidor HTTP
var builder = WebApplication.CreateBuilder(args);
// Constrói a instância efetiva da aplicação web com base nas configurações definidas
var app = builder.Build();

// Adiciona explicitamente a URL e a porta 8080 na qual o servidor HTTP/JSON-RPC vai escutar as conexões de rede
app.Urls.Add("http://localhost:8080");

// Mapeia um endpoint HTTP POST no caminho "/rpc" para interceptar e processar as requisições JSON-RPC recebidas
app.MapPost("/rpc", ([FromBody] RpcRequest req) =>
{
    // Exibe no console do servidor o nome do método JSON-RPC solicitado remotamente pelo cliente
    Console.WriteLine($"\n[Servidor] Requisição JSON-RPC recebida para o método: '{req.Method}'");

    try
    {
        // Inicializa a variável numérica que armazenará o resultado calculado da operação
        double resultado = 0;

        // Estrutura de decisão para direcionar a execução de acordo com o método especificado no JSON-RPC
        switch (req.Method.ToLower())
        {
            case "somar":
                // Executa a operação aritmética básica de soma utilizando os parâmetros 'a' e 'b' da requisição
                resultado = req.A + req.B;
                break;
            case "subtrair":
                // Executa a operação aritmética básica de subtração utilizando os parâmetros 'a' e 'b'
                resultado = req.A - req.B;
                break;
            case "multiplicar":
                // Executa a operação aritmética básica de multiplicação utilizando os parâmetros 'a' e 'b'
                resultado = req.A * req.B;
                break;
            case "dividir":
                // Valida se o divisor 'b' é zero para prevenir exceções fatais de divisão por zero na CPU
                if (req.B == 0) throw new DivideByZeroException("Erro: Divisão por zero!");
                // Executa a operação aritmética básica de divisão
                resultado = req.A / req.B;
                break;
            case "calcular_expressao":
                // Abordagem A: Avalia e calcula uma expressão matemática inteira diretamente no servidor
                if (string.IsNullOrEmpty(req.Expression)) throw new Exception("Expressão vazia.");
                var evalResult = new DataTable().Compute(req.Expression, null);
                resultado = Convert.ToDouble(evalResult);
                break;
            default:
                // Retorna um objeto de resposta JSON-RPC contendo uma mensagem de erro caso o método seja desconhecido
                return Results.Ok(new RpcResponse { Error = $"Método '{req.Method}' desconhecido." });
        }

        // Exibe no console do servidor o sucesso e o valor numérico obtido pelo cálculo
        Console.WriteLine($"[Servidor] Sucesso! Resultado calculado: {resultado}");

        // Serializa e retorna um status HTTP 200 contendo o objeto RpcResponse com o resultado da chamada RPC
        return Results.Ok(new RpcResponse { Result = resultado });
    }
    catch (Exception ex)
    {
        // Captura qualquer exceção ocorrida durante o processamento do cálculo ou da requisição
        Console.WriteLine($"[Servidor] Erro: {ex.Message}");
        // Retorna um objeto RpcResponse encapsulando a mensagem de erro formatada para o cliente
        return Results.Ok(new RpcResponse { Error = ex.Message });
    }
});

// Inicia a execução do servidor web e bloqueia a thread principal para mantê-lo ouvindo a porta de rede
app.Run();

// --- MODELOS DE DADOS DO PROTOCOLO JSON-RPC ---

// Classe de modelo utilizada para o mapeamento e desserialização do JSON recebido na requisição RPC
public class RpcRequest
{
    public string Method { get; set; } = "";
    public double A { get; set; }
    public double B { get; set; }
    public string? Expression { get; set; }
}

// Classe de modelo utilizada para a serialização do JSON enviado de volta como resposta RPC
public class RpcResponse
{
    public double? Result { get; set; }
    public string? Error { get; set; }
}