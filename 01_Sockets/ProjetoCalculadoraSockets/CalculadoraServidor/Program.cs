using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Data;

class CalculadoraServerSocket
{
    static void Main(string[] args)
    {
        // Inicializa o TcpListener escutando conexões TCP no IP local (localhost) na porta 9090
        TcpListener servidor = new TcpListener(IPAddress.Loopback, 9090);
        servidor.Start();
        Console.WriteLine("Servidor Socket C# no ar e aguardando conexões na porta 9090...");

        while (true)
        {
            // Fica bloqueado aguardando e aceitando a conexão de um cliente da rede
            TcpClient cliente = servidor.AcceptTcpClient();
            Console.WriteLine("\nNova conexão aceita de cliente.");

            // Abre os fluxos de rede para leitura e escrita de dados com o cliente
            using NetworkStream stream = cliente.GetStream();
            using StreamReader leitor = new StreamReader(stream);
            using StreamWriter escritor = new StreamWriter(stream) { AutoFlush = true };

            // Lê a primeira linha enviada pelo cliente
            string? operacaoStr = leitor.ReadLine();
            if (operacaoStr == null) continue;

            int operacao = int.Parse(operacaoStr);
            string resultado = "";

            try
            {
                if (operacao == 5)
                {
                    // Recebe a expressão inteira para calcular no servidor
                    string? expressao = leitor.ReadLine();
                    if (expressao != null)
                    {
                        // Utiliza o DataTable para calcular o valor da expressão matemática em string
                        var evalResult = new DataTable().Compute(expressao, null);
                        resultado = evalResult?.ToString() ?? "Erro ao calcular expressão";
                    }
                }
                else
                {
                    // Operações individuais normais (1 a 4)
                    string? op1Str = leitor.ReadLine();
                    string? op2Str = leitor.ReadLine();

                    if (op1Str != null && op2Str != null)
                    {
                        double op1 = double.Parse(op1Str);
                        double op2 = double.Parse(op2Str);

                        switch (operacao)
                        {
                            case 1: resultado = (op1 + op2).ToString(); break;
                            case 2: resultado = (op1 - op2).ToString(); break;
                            case 3: resultado = (op1 * op2).ToString(); break;
                            case 4:
                                if (op2 == 0) throw new DivideByZeroException("Erro: Divisão por zero!");
                                resultado = (op1 / op2).ToString();
                                break;
                            default: resultado = "Erro: Operação inválida."; break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultado = "Erro: " + ex.Message;
            }

            escritor.WriteLine(resultado);
            Console.WriteLine($"Resultado calculado e enviado: {resultado}");
        }
    }
}