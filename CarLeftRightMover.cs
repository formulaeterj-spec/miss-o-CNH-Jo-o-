/*
  CarLeftRightMover.cs
  ----------------------
  Move o carro para a ESQUERDA quando o contador do encoder aumenta,
  e para a DIREITA quando o contador diminui.

  Como usar:
  1. Adicione este script no GameObject do carro (o mesmo que tem o Rigidbody,
     se houver).
  2. Ele busca sozinho o SerialCounterReader.Instance — não precisa arrastar nada.
  3. Ajuste "Velocidade" no Inspector conforme a resposta que quiser.
*/

using UnityEngine;

public class CarLeftRightMover : MonoBehaviour
{
    [Tooltip("Velocidade de deslocamento lateral (unidades por segundo)")]
    [SerializeField] private float velocidade = 5f;

    private long ultimoCounter;
    private bool inicializado = false;

    private void Update()
    {
        if (SerialCounterReader.Instance == null)
            return;

        long atual = SerialCounterReader.Instance.Counter;

        // Primeira leitura: só guarda o valor, sem mover (evita um "pulo" inicial)
        if (!inicializado)
        {
            ultimoCounter = atual;
            inicializado = true;
            return;
        }

        long diferenca = atual - ultimoCounter;

        if (diferenca > 0)
        {
            // Contador aumentou -> move para a esquerda
            transform.Translate(Vector3.left * velocidade * Time.deltaTime, Space.World);
        }
        else if (diferenca < 0)
        {
            // Contador diminuiu -> move para a direita
            transform.Translate(Vector3.right * velocidade * Time.deltaTime, Space.World);
        }

        ultimoCounter = atual;
    }
}
