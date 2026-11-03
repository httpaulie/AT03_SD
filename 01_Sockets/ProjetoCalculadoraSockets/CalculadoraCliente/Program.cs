using System;
using System.IO;
using System.Net.Sockets;

class CalculadoraClientSocket
{
    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("=== CLIENTE CALCULADORA SOCKET (C#) ===");
            Console.WriteLine("========================================");
            Console.WriteLine("1 - Soma (ex: 10 + 15)");
            Console.WriteLine("2 - Subtração");
            Console.WriteLine("3 - Multiplicação");
            Console.WriteLine("4 - Divisão");
            Console.WriteLine("5 - Expressão calculada no servidor (ex: (10 + 15) * 4)");
            Console.WriteLine("6 - Expressão decomposta no cliente (ex: (10 + 15) * 4 chamando o servidor)");
            Console.WriteLine("0 - Sair da aplicação");
            Console.Write("\nDigite o código da operação (0-6): ");

            string? inputOp = Console.ReadLine();
            if (!int.TryParse(inputOp, out int operacao))
            {
                Console.WriteLine("Opção inválida. Digite um número entre 0 e 6.");
                continue;
            }

            if (operacao == 0)
            {
                Console.WriteLine("Encerrando o cliente... Até mais!");
                break;
            }

            string expressaoCliente = "";
            double oper1 = 0, oper2 = 0;

            if (operacao == 5)
            {
                Console.Write("Digite a expressão matemática (ex: (10 + 15) * 4): ");
                expressaoCliente = Console.ReadLine() ?? "";
            }
            else if (operacao == 6)
            {
                Console.Write("Digite a expressão simples (ex: (10 + 15) * 4): ");
                expressaoCliente = Console.ReadLine() ?? "";

                // Decomposição no Cliente
                try
                {
                    // Calcula a parte interna chamando o servidor com a operação de soma
                    double subRes1 = EnviarParaServidor(1, 10, 15);
                    //  Multiplica o resultado pelo fator externo 4 chamando o servidor com multiplicação
                    double resultadoFinal = EnviarParaServidor(3, subRes1, 4);

                    Console.WriteLine($"\n[CLIENTE - Decomposição] A expressão foi decomposta no cliente.");
                    Console.WriteLine($"Resultado final obtido via chamadas parciais ao servidor = {resultadoFinal}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro na decomposição: {ex.Message}");
                }

                PausarTela();
                continue; // Volta para o início do loop do menu
            }
            else
            {
                Console.Write("Digite o primeiro operando: ");
                oper1 = double.Parse(Console.ReadLine() ?? "0");

                Console.Write("Digite o segundo operando: ");
                oper2 = double.Parse(Console.ReadLine() ?? "0");
            }

            try
            {
                // Cria um socket TCP conectando o cliente ao IP local na porta 9090 do servidor
                TcpClient clientSocket = new TcpClient("127.0.0.1", 9090);

                // Obtém os fluxos de rede para comunicação
                using NetworkStream stream = clientSocket.GetStream();
                using StreamReader leitor = new StreamReader(stream);
                using StreamWriter escritor = new StreamWriter(stream) { AutoFlush = true };

                // Envia os dados conforme a operação escolhida
                escritor.WriteLine(operacao);
                if (operacao == 5)
                {
                    escritor.WriteLine(expressaoCliente);
                }
                else
                {
                    escritor.WriteLine(oper1);
                    escritor.WriteLine(oper2);
                }

                // Lê a linha de texto enviada de volta pelo servidor contendo o resultado
                string? resultado = leitor.ReadLine();

                // Exibe o resultado final no console
                Console.WriteLine($"\n--> Resultado recebido do servidor = {resultado}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro na conexão com o servidor: {e.Message}");
            }

            PausarTela();
        }
    }

    // Método auxiliar para fazer chamadas individuais reutilizáveis na Abordagem B
    private static double EnviarParaServidor(int operacao, double op1, double op2)
    {
        TcpClient clientSocket = new TcpClient("127.0.0.1", 9090);
        using NetworkStream stream = clientSocket.GetStream();
        using StreamReader leitor = new StreamReader(stream);
        using StreamWriter escritor = new StreamWriter(stream) { AutoFlush = true };

        escritor.WriteLine(operacao);
        escritor.WriteLine(op1);
        escritor.WriteLine(op2);

        string? resposta = leitor.ReadLine();
        return double.Parse(resposta ?? "0");
    }

    private static void PausarTela()
    {
        Console.WriteLine("\nPressione qualquer tecla para realizar outra operação...");
        Console.ReadKey();
        Console.Clear();
    }
}