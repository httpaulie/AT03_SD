// Define o pino analógico onde o potenciômetro está conectado
const int potPin = A0;

void setup() {
  // Inicializa a comunicação serial com a mesma velocidade (baud rate) que o C# vai escutar
  Serial.begin(9600);
}

void loop() {
  // Lê o valor do potenciômetro
  int valorBruto = analogRead(potPin);

  // Mapeia o valor bruto para uma faixa de temperatura realista de 0 a 300 °C
  float temperatura = map(valorBruto, 0, 1023, 0, 300);

  // Envia o valor da temperatura pela porta USB Serial em formato de linha única
  Serial.println(temperatura);

  // Aguarda 60 segundos antes da próxima leitura 
  // (Nota: No enunciado original pede a cada 60s, mas para testes e gravação do vídeo, 
  // deixei a cada 2 segundos para deixar a dinâmica da aplicação mais ágil)
  delay(60000);
}