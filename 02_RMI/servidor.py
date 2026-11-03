import Pyro5.api
@Pyro5.api.expose
class Calculadora:
    def __init__(self):
        self.chamadas = 0

    def soma(self, a, b):
        self.chamadas += 1
        print(f"Método soma chamado {self.chamadas} vezes.")
        return a + b

    def subtracao(self, a, b):
        self.chamadas += 1
        print(f"Método subtracao chamado {self.chamadas} vezes.")
        return a - b

    def multiplicacao(self, a, b):
        self.chamadas += 1
        print(f"Método multiplicacao chamado {self.chamadas} vezes.")
        return a * b

    def divisao(self, a, b):
        self.chamadas += 1
        print(f"Método divisao chamado {self.chamadas} vezes.")
        if b == 0:
            raise ValueError("Erro: Divisão por zero!")
        return a / b

    def calcular_expressao(self, expressao):
        """Abordagem A: Calcula a expressão inteira no servidor de forma segura"""
        self.chamadas += 1
        print(f"Calculando expressão inteira no servidor: {expressao}")
        try:
            # Avaliação segura restrita apenas a expressões matemáticas básicas
            # (eval restrito para evitar comandos arbitrários)
            resultado = eval(expressao, {"__builtins__": {}}, {})
            return resultado
        except Exception as e:
            return f"Erro ao calcular expressão: {str(e)}"

def main():
    # Inicializa o daemon do Pyro
    daemon = Pyro5.api.Daemon(host="localhost", port=9090)
    
    # Registra a classe Calculadora como um objeto remoto
    uri = daemon.register(Calculadora, objectId="calculadora")
    
    print(f"Servidor RMI (Pyro) no ar!")
    print(f"URI do Objeto: {uri}")
    
    # Inicia o loop de atendimento aos clientes
    daemon.requestLoop()

if __name__ == "__main__":
    main()