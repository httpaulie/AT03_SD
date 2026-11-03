using Microsoft.AspNetCore.Mvc;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Configura o servidor para rodar na porta 5000
app.Urls.Add("http://localhost:5000");

// Mapeia uma rota POST "/api/calcular" para processar as operações via HTTP REST
app.MapPost("/api/calcular", ([FromBody] CalculadoraRequest req) =>
{
    Console.WriteLine($"\n[Servidor REST] Requisição POST recebida. Operação: {req.Operacao}");

    try
    {
        double resultado = 0;

        // Processa a lógica de negócio com base no código da operação enviado via POST
        switch (req.Operacao)
        {
            case 1: // Soma
                resultado = req.A + req.B;
                break;
            case 2: // Subtração
                resultado = req.A - req.B;
                break;
            case 3: // Multiplicação
                resultado = req.A * req.B;
                break;
            case 4: // Divisão
                if (req.B == 0) throw new DivideByZeroException("Erro: Divisão por zero!");
                resultado = req.A / req.B;
                break;
            case 5: // Expressão calculada inteiramente no servidor
                if (string.IsNullOrEmpty(req.Expressao)) throw new Exception("Expressão vazia.");
                var evalResult = new DataTable().Compute(req.Expressao, null);
                resultado = Convert.ToDouble(evalResult);
                break;
            default:
                // Retorna um status HTTP 400 se a operação for inválida
                return Results.BadRequest(new { erro = "Operação inválida." });
        }

        Console.WriteLine($"[Servidor REST] Cálculo realizado com sucesso. Resultado: {resultado}");

        // Retorna um status HTTP 200 contendo o objeto JSON com o resultado
        return Results.Ok(new { resultado = resultado });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Servidor REST] Erro: {ex.Message}");
        // Retorna um status HTTP 500 encapsulando a mensagem de erro
        return Results.StatusCode(500);
    }
});

app.Run();

// Modelo de dados que representa o corpo da requisição POST recebida pelo servidor REST
public class CalculadoraRequest
{
    public int Operacao { get; set; }
    public double A { get; set; }
    public double B { get; set; }
    public string? Expressao { get; set; }
}