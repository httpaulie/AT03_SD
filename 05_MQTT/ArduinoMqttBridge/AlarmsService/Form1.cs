using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client;

namespace AlarmsService
{
    public partial class Form1 : Form
    {
        private Label lblTitulo;

        // Indicador 1: Temperatura Alta (>200°C)
        private Panel pnlStatusAlta;
        private Label lblStatusAltaTexto;

        // Indicador 2: Variação Repentina (>5°C)
        private Panel pnlStatusRepentino;
        private Label lblStatusRepentinoTexto;

        private ListBox lstAlarmes;
        private Label lblRodape;

        private System.Windows.Forms.Timer timerCheckAlta;
        private System.Windows.Forms.Timer timerCheckRepentino;

        private DateTime ultimaVezAlertaAlta = DateTime.MinValue;
        private DateTime ultimaVezAlertaRepentino = DateTime.MinValue;

        // Cliente MQTT
        private IMqttClient? mqttClient;

        public Form1()
        {
            InicializarInterfaceGrafica();
            ConfigurarTimersDeReset();
            _ = InicializarMqttAsync();
        }

        private void InicializarInterfaceGrafica()
        {
            this.Text = "Central de Operação e Alarmes - Indústria";
            this.Size = new Size(720, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);

            lblTitulo = new Label();
            lblTitulo.Text = "PAINEL DE SUPERVISÃO E ALARMES - CALDEIRA";
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitulo.Location = new Point(20, 20);
            lblTitulo.AutoSize = true;
            this.Controls.Add(lblTitulo);

            pnlStatusAlta = new Panel();
            pnlStatusAlta.Size = new Size(25, 25);
            pnlStatusAlta.Location = new Point(20, 60);
            pnlStatusAlta.BackColor = Color.Green;
            this.Controls.Add(pnlStatusAlta);

            lblStatusAltaTexto = new Label();
            lblStatusAltaTexto.Text = "Temperatura Alta (> 200°C): NORMAL";
            lblStatusAltaTexto.ForeColor = Color.LightGreen;
            lblStatusAltaTexto.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatusAltaTexto.Location = new Point(55, 62);
            lblStatusAltaTexto.AutoSize = true;
            this.Controls.Add(lblStatusAltaTexto);

            pnlStatusRepentino = new Panel();
            pnlStatusRepentino.Size = new Size(25, 25);
            pnlStatusRepentino.Location = new Point(20, 100);
            pnlStatusRepentino.BackColor = Color.Green;
            this.Controls.Add(pnlStatusRepentino);

            lblStatusRepentinoTexto = new Label();
            lblStatusRepentinoTexto.Text = "Variação Repentina (> 5°C): NORMAL";
            lblStatusRepentinoTexto.ForeColor = Color.LightGreen;
            lblStatusRepentinoTexto.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatusRepentinoTexto.Location = new Point(55, 102);
            lblStatusRepentinoTexto.AutoSize = true;
            this.Controls.Add(lblStatusRepentinoTexto);

            // Lista de Histórico de Eventos
            lstAlarmes = new ListBox();
            lstAlarmes.Location = new Point(20, 150);
            lstAlarmes.Size = new Size(660, 310);
            lstAlarmes.BackColor = Color.FromArgb(45, 45, 45);
            lstAlarmes.ForeColor = Color.White;
            lstAlarmes.Font = new Font("Consolas", 9, FontStyle.Regular);
            this.Controls.Add(lstAlarmes);

            // Rodapé
            lblRodape = new Label();
            lblRodape.Text = "Monitorando tópicos MQTT: industrial/alarmes/#";
            lblRodape.ForeColor = Color.Gray;
            lblRodape.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblRodape.Location = new Point(20, 480);
            lblRodape.AutoSize = true;
            this.Controls.Add(lblRodape);
        }

        private void ConfigurarTimersDeReset()
        {
            timerCheckAlta = new System.Windows.Forms.Timer();
            timerCheckAlta.Interval = 1000; // Verifica a cada 1 segundo
            timerCheckAlta.Tick += (s, e) =>
            {
                if (ultimaVezAlertaAlta != DateTime.MinValue && (DateTime.Now - ultimaVezAlertaAlta).TotalSeconds > 6)
                {
                    pnlStatusAlta.BackColor = Color.Green;
                    lblStatusAltaTexto.Text = "Temperatura Alta (> 200°C): NORMAL";
                    lblStatusAltaTexto.ForeColor = Color.LightGreen;
                    ultimaVezAlertaAlta = DateTime.MinValue; // Reseta
                }
            };
            timerCheckAlta.Start();

            timerCheckRepentino = new System.Windows.Forms.Timer();
            timerCheckRepentino.Interval = 1000;
            timerCheckRepentino.Tick += (s, e) =>
            {
                if (ultimaVezAlertaRepentino != DateTime.MinValue && (DateTime.Now - ultimaVezAlertaRepentino).TotalSeconds > 6)
                {
                    pnlStatusRepentino.BackColor = Color.Green;
                    lblStatusRepentinoTexto.Text = "Variação Repentina (> 5°C): NORMAL";
                    lblStatusRepentinoTexto.ForeColor = Color.LightGreen;
                    ultimaVezAlertaRepentino = DateTime.MinValue;
                }
            };
            timerCheckRepentino.Start();
        }

        // Conecta ao Broker MQTT e separa o tratamento por subtópico
        private async Task InicializarMqttAsync()
        {
            try
            {
                var factory = new MqttFactory();
                mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", 1883)
                    .WithClientId("AlarmsServiceGUI")
                    .Build();

                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    string topico = e.ApplicationMessage.Topic;
                    string mensagemAlerta = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    string logFormatado = $"[{timestamp}] [{topico.ToUpper()}] -> {mensagemAlerta}";

                    this.Invoke(new Action(() =>
                    {
                        lstAlarmes.Items.Insert(0, logFormatado);

                        if (topico.Contains("alta"))
                        {
                            pnlStatusAlta.BackColor = Color.Red;
                            lblStatusAltaTexto.Text = "TEMPERATURA ALTA ATIVA!";
                            lblStatusAltaTexto.ForeColor = Color.Red;
                            ultimaVezAlertaAlta = DateTime.Now;
                        }
                        else if (topico.Contains("repentino"))
                        {
                            pnlStatusRepentino.BackColor = Color.Red;
                            lblStatusRepentinoTexto.Text = "VARIAÇÃO REPENTINA DETECTADA!";
                            lblStatusRepentinoTexto.ForeColor = Color.Red;
                            ultimaVezAlertaRepentino = DateTime.Now;
                        }
                    }));

                    return Task.CompletedTask;
                };

                await mqttClient.ConnectAsync(options);

                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("industrial/alarmes/#")
                    .Build();

                await mqttClient.SubscribeAsync(subscribeOptions);

                this.Invoke(new Action(() =>
                {
                    lstAlarmes.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [SISTEMA] Conectado com sucesso ao Broker MQTT!");
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show($"Erro ao conectar no Broker MQTT: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }
    }
}