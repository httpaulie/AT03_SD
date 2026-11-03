import Pyro5.api
import ast

def avaliar_por_decomposicao(node, calc):
    """
    Função recursiva que interpreta a árvore sintática da expressão no cliente.
    Para cada operador encontrado, faz uma chamada remota separada ao servidor.
    """
    if isinstance(node, ast.Expression):
        return avaliar_por_decomposicao(node.body, calc)
    
    elif isinstance(node, ast.Constant):  # Python 3.8+ para números
        if isinstance(node.value, (int, float)):
            return float(node.value)
        raise ValueError("Apenas números são permitidos na expressão.")
    
    elif isinstance(node, ast.BinOp):
        # Avalia recursivamente os lados esquerdo e direito
        left_val = avaliar_por_decomposicao(node.left, calc)
        right_val = avaliar_por_decomposicao(node.right, calc)
        
        # Identifica o operador e delega a computação chamando o método remoto correspondente
        if isinstance(node.op, ast.Add):
            print(f"  [Cliente] Decompondo: invocando servidor.soma({left_val}, {right_val})")
            return calc.soma(left_val, right_val)
        
        elif isinstance(node.op, ast.Sub):
            print(f"  [Cliente] Decompondo: invocando servidor.subtracao({left_val}, {right_val})")
            return calc.subtracao(left_val, right_val)
        
        elif isinstance(node.op, ast.Mult):
            print(f"  [Cliente] Decompondo: invocando servidor.multiplicacao({left_val}, {right_val})")
            return calc.multiplicacao(left_val, right_val)
        
        elif isinstance(node.op, ast.Div):
            print(f"  [Cliente] Decompondo: invocando servidor.divisao({left_val}, {right_val})")
            return calc.divisao(left_val, right_val)
        
        else:
            raise ValueError(f"Operador não suportado: {type(node.op)}")
            
    elif isinstance(node, ast.UnaryOp): # Suporte a números negativos (ex: -5)
        operand = avaliar_por_decomposicao(node.operand, calc)
        if isinstance(node.op, ast.USub):
            return -operand
        elif isinstance(node.op, ast.UAdd):
            return +operand
        raise ValueError("Operador unário não suportado.")
    
    else:
        raise ValueError("Expressão matemática inválida.")

def main():
    # Obtém a referência proxy para o objeto remoto registrado no servidor
    calc = Pyro5.api.Proxy("PYRO:calculadora@localhost:9090")

    while True:
        print("\n========================================")
        print("=== CLIENTE RMI / PYRO (PYTHON) ===")
        print("========================================")
        print("1 - Soma (ex: 10 + 15)")
        print("2 - Subtração")
        print("3 - Multiplicação")
        print("4 - Divisão")
        print("5 - Expressão calculada INTEIRAMENTE NO SERVIDOR (ex: (10 + 15) * 4)")
        print("6 - Expressão DECOMPOSTA NO CLIENTE (ex: (10 + 15) * 4 com chamadas ao servidor)")
        print("0 - Sair")
        
        opcao = input("\nDigite o código da operação (0-6): ")
        
        if opcao == "0":
            print("Encerrando o cliente...")
            break

        try:
            if opcao == "5":
                expressao = input("Digite a expressão matemática (ex: (10 + 15) * 4): ")
                # Abordagem A: Envia a string inteira para o servidor calcular
                resultado = calc.calcular_expressao(expressao)
                print(f"\n--> Resultado calculado inteiramente no Servidor = {resultado}")

            elif opcao == "6":
                # Abordagem B: O cliente interpreta a expressão e faz múltiplas chamadas remotas
                expressao = input("Digite a expressão para decomposição (ex: (10 + 15) * 4): ")
                print("\n[CLIENTE - Interpretando e decompondo a expressão...]")
                
                # Faz o parse da string em uma árvore de sintaxe abstrata (AST)
                tree = ast.parse(expressao, mode='eval')
                
                # Executa a árvore avaliando e chamando o servidor remotamente a cada passo
                resultado_final = avaliar_por_decomposicao(tree, calc)
                
                print(f"\n--> Resultado final obtido por decomposição no cliente = {resultado_final}")

            elif opcao in ["1", "2", "3", "4"]:
                op1 = float(input("Digite o primeiro operando: "))
                op2 = float(input("Digite o segundo operando: "))

                if opcao == "1":
                    resultado = calc.soma(op1, op2)
                elif opcao == "2":
                    resultado = calc.subtracao(op1, op2)
                elif opcao == "3":
                    resultado = calc.multiplicacao(op1, op2)
                elif opcao == "4":
                    resultado = calc.divisao(op1, op2)

                print(f"\n--> Resultado da operação remota = {resultado}")
            else:
                print("Opção inválida!")

        except Exception as e:
            print(f"Erro na chamada remota / avaliação: {e}")

        input("\nPressione Enter para continuar...")

if __name__ == "__main__":
    main()