/*
  CarLeftRightMover.cs (agora funcionando como VOLANTE)
  --------------------------------------------------------
  Gira o carro para a ESQUERDA quando o contador do encoder aumenta,
  e para a DIREITA quando o contador diminui — ou seja, o contador
  passa a funcionar como o volante: gira o volante, o carro vira.

  Como usar:
  1. Adicione este script no GameObject do carro (o mesmo que tem o Rigidbody,
     se houver — para física de verdade, considere aplicar em um Rigidbody
     com AddTorque em vez de Rotate direto, mas isso aqui já dá o efeito
     básico de "virar o carro").
  2. Ele busca sozinho o SerialCounterReader.Instance — não precisa arrastar nada.
  3. Ajuste "velocidadeAngular" no Inspector: quanto maior, mais rápido o
     carro vira para cada "tick" do encoder.
*/

using UnityEngine;

public class CarLeftRightMover : MonoBehaviour
{
    [Tooltip("Velocidade de rotação (graus por segundo) enquanto o contador estiver mudando")]
    [SerializeField] private float velocidadeAngular = 45f;

    private long ultimoCounter;
    private bool inicializado = false;

    private void Update()
    {
        if (SerialCounterReader.Instance == null)
            return;

        long atual = SerialCounterReader.Instance.Counter;

        // Primeira leitura: só guarda o valor, sem girar (evita um "pulo" inicial)
        if (!inicializado)
        {
            ultimoCounter = atual;
            inicializado = true;
            return;
        }

        long diferenca = atual - ultimoCounter;

        if (diferenca > 0)
        {
            // Contador aumentou -> gira o carro para a esquerda
            transform.Rotate(Vector3.up * -velocidadeAngular * Time.deltaTime, Space.World);
        }
        else if (diferenca < 0)
        {
            // Contador diminuiu -> gira o carro para a direita
            transform.Rotate(Vector3.up * velocidadeAngular * Time.deltaTime, Space.World);
        }

        ultimoCounter = atual;
    }
}
