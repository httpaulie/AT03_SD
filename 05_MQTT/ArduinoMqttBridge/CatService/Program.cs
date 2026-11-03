using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

class CatServiceApp
{
    // Lista para armazenar o histórico de leituras de temperatura recebidas
    private static readonly List<LeituraSensor> historicoLeituras = new List<LeituraSensor>();
    private static readonly object lockObj = new object();

    // Variável para armazenar a média anterior para o cálculo de variação repentina (> 5°C)
    private static double? mediaAnterior = null;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== SERVIÇO CAT (COMPUTE AVERAGE TEMPERATURE) ===");

        // Inicializa o cliente MQTT utilizando MQTTnet
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        // Configura as opções de conexão para o broker Mosquitto rodando no Docker na porta 1883
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithClientId("CatServiceClient")
            .Build();

        // Configura o evento disparado quando uma nova mensagem MQTT é recebida no tópico inscrito
        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            string topico = e.ApplicationMessage.Topic;
            string payloadJson = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            Console.WriteLine($"\n[CAT] Mensagem recebida no tópico '{topico}': {payloadJson}");

            try
            {
                // Desserializa o payload JSON recebido do sensor/bridge
                var dados = JsonSerializer.Deserialize<PayloadSensor>(payloadJson);

                if (dados != null)
                {
                    // Protege o acesso à lista compartilhada com bloqueio de thread
                    lock (lockObj)
                    {
                        // Adiciona a nova leitura ao histórico com o timestamp atual
                        historicoLeituras.Add(new LeituraSensor
                        {
                            SensorId = dados.sensorId,
                            Temperatura = dados.temperatura,
                            Momento = DateTime.Now
                        });

                        // Remove leituras mais antigas que 120 segundos
                        DateTime limiteTempo = DateTime.Now.AddSeconds(-120);
                        historicoLeituras.RemoveAll(l => l.Momento < limiteTempo);
                    }

                    // Calcula a média das temperaturas dos últimos 120 segundos
                    double mediaAtual = 0;
                    lock (lockObj)
                    {
                        if (historicoLeituras.Count > 0)
                        {
                            mediaAtual = historicoLeituras.Average(l => l.Temperatura);
                        }
                    }

                    Console.WriteLine($"[CAT] Média dos últimos 120s = {mediaAtual:F2} °C (Total de amostras na janela: {historicoLeituras.Count})");

                    // Verifica se a temperatura média está acima de 200°C
                    if (mediaAtual > 200.0)
                    {
                        string mensagemAlerta = $"ALERTA CRÍTICO: Temperatura alta detectada! Média atual: {mediaAtual:F2} °C";
                        Console.WriteLine($"[CAT ALARME] {mensagemAlerta}");

                        // Publica o evento de temperatura alta
                        var msgAlertaAlta = new MqttApplicationMessageBuilder()
                            .WithTopic("industrial/alarmes/alta")
                            .WithPayload(mensagemAlerta)
                            .Build();

                        await mqttClient.PublishAsync(msgAlertaAlta, CancellationToken.None);
                    }

                    //Verifica variação repentina (diferença > 5°C entre a média atual e a anterior)
                    if (mediaAnterior.HasValue)
                    {
                        double diferenca = Math.Abs(mediaAtual - mediaAnterior.Value);
                        if (diferenca > 5.0)
                        {
                            string mensagemRepentina = $"ALERTA DE VARIAÇÃO: Aumento repentino de temperatura! Variação de {diferenca:F2} °C (Anterior: {mediaAnterior.Value:F2} | Atual: {mediaAtual:F2})";
                            Console.WriteLine($"[CAT ALARME] {mensagemRepentina}");

                            // Publica o evento de variação repentina no tópico correspondente
                            var msgAlertaRepentino = new MqttApplicationMessageBuilder()
                                .WithTopic("industrial/alarmes/repentino")
                                .WithPayload(mensagemRepentina)
                                .Build();

                            await mqttClient.PublishAsync(msgAlertaRepentino, CancellationToken.None);
                        }
                    }

                    // Atualiza a média anterior para a próxima comparação
                    mediaAnterior = mediaAtual;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro CAT] Falha ao processar mensagem MQTT: {ex.Message}");
            }
        };

        try
        {
            Console.WriteLine("[MQTT] Conectando ao Broker Mosquitto...");
            await mqttClient.ConnectAsync(options);
            Console.WriteLine("[MQTT] Conectado com sucesso!");

            // Inscreve-se no tópico de publicação de temperatura da caldeira
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter("industrial/caldeira/temperatura")
                .Build();

            await mqttClient.SubscribeAsync(subscribeOptions);
            Console.WriteLine("[CAT] Inscrito com sucesso no tópico: 'industrial/caldeira/temperatura'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Erro MQTT] Falha ao conectar ou inscrever: {ex.Message}");
            return;
        }

        Console.WriteLine("\n[CAT] Serviço em execução. Pressione qualquer tecla para sair.\n");
        Console.ReadKey();
    }
}

// --- MODELOS DE DADOS ---

// Modelo que representa a estrutura do JSON recebido do sensor/bridge
public class PayloadSensor
{
    public string sensorId { get; set; } = "";
    public double temperatura { get; set; }
    public string timestamp { get; set; } = "";
}

// Modelo interno para armazenar o histórico com controle de tempo para a janela deslizante
public class LeituraSensor
{
    public string SensorId { get; set; } = "";
    public double Temperatura { get; set; }
    public DateTime Momento { get; set; }
}