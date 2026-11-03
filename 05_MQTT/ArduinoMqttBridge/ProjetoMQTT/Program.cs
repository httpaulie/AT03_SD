using System;
using System.IO.Ports;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

class ArduinoMqttBridgeApp
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== BRIDGE SERIAL -> MQTT (ARDUINO PARA BROKER) ===");

        // Solicita ao usuário a porta COM onde o Arduino Uno está conectado (ex: COM3)
        Console.Write("Digite a porta COM do Arduino (ex: COM3): ");
        string portaCom = Console.ReadLine() ?? "COM3";

        // Configura a porta serial física com a taxa de 9600 baud rate do Arduino
        SerialPort serialPort = new SerialPort(portaCom, 9600);
        try
        {
            serialPort.Open();
            Console.WriteLine($"[Serial] Conexão estabelecida com sucesso na porta {portaCom}!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Erro Serial] Falha ao abrir a porta {portaCom}: {ex.Message}");
            return;
        }

        // Inicializa a fábrica e o cliente MQTT utilizando a biblioteca MQTTnet
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        // Configura as opções de conexão para o broker Mosquitto rodando no Docker na porta 1883
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithClientId("ArduinoBridgeCsharp")
            .Build();

        try
        {
            Console.WriteLine("[MQTT] Conectando ao Broker Mosquitto em localhost:1883...");
            await mqttClient.ConnectAsync(options);
            Console.WriteLine("[MQTT] Conectado com sucesso ao Broker MQTT!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Erro MQTT] Não foi possível conectar ao broker: {ex.Message}");
            serialPort.Close();
            return;
        }

        Console.WriteLine("\n[Bridge] Aguardando dados do sensor físico do Arduino para publicar no tópico MQTT...\n");

        //Loop para ler a serial do Arduino e republicar no broker
        while (true)
        {
            try
            {
                // Lê uma linha de texto enviada pelo Arduino via USB
                string? linhaSerial = serialPort.ReadLine();

                if (!string.IsNullOrEmpty(linhaSerial))
                {
                    string valorLimpo = linhaSerial.Trim();

                    if (double.TryParse(valorLimpo, out double temperatura))
                    {
                        double temperaturaReal = temperatura / 100.0;

                        // Cria o payload em formato JSON
                        var payloadObj = new
                        {
                            sensorId = "Arduino_Uno_R3_Fisico",
                            temperatura = temperaturaReal,
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };

                        string jsonPayload = JsonSerializer.Serialize(payloadObj);

                        // Constrói a mensagem MQTT destinada ao tópico da caldeira
                        var applicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("industrial/caldeira/temperatura")
                            .WithPayload(jsonPayload)
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                            .Build();

                        // Dispara a publicação da mensagem de forma assíncrona no broker MQTT
                        await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                        Console.WriteLine($"[Bridge] Lido e Publicado -> Tópico: 'industrial/caldeira/temperatura' | Payload: {jsonPayload}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Trata intermitências de leitura sem interromper o loop da aplicação
                Console.WriteLine($"[Aviso] Erro na leitura serial: {ex.Message}");
            }
        }
    }
}