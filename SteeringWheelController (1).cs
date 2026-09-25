/*
  SteeringWheelController.cs
  ----------------------------
  Pega o "Counter" (bruto, vindo do Arduino via SerialCounterReader) e
  transforma em um volante de verdade:

    1. Calibração de centro (o Arduino sempre liga no 0, que raramente
       é o centro físico do volante — então você recentraliza em runtime).
    2. Conversão counter -> graus de rotação do volante, usando a relação
       encoder/volante que você mediu (4,5 voltas do encoder por 1 volta
       do volante).
    3. Limite de curso (ex: volante trava em ±450°, ou seja, 900° ponta
       a ponta — ajustável).
    4. Normalização para -1..+1 (útil pra alimentar qualquer script de
       carro).
    5. (Opcional) Rotaciona visualmente um objeto 3D do volante.
    6. (Opcional) Aplica o steer direto em WheelColliders (se você estiver
       usando a física de veículo padrão da Unity).

  Como usar:
  1. Crie um GameObject (ex: "SteeringWheel") e adicione este script.
  2. Ele busca automaticamente o SerialCounterReader.Instance — não
     precisa arrastar nada, desde que o outro script já esteja na cena.
  3. (Opcional) Arraste o modelo 3D do volante em "wheelVisualTransform".
  4. (Opcional) Arraste os WheelColliders da frente em "frontWheels".
  5. Aperte a tecla de recentralização (padrão R) com o volante na
     posição física central antes de começar a dirigir.
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringWheelController : MonoBehaviour
{
    [Header("Calibração do encoder")]
    [Tooltip("Quantos 'counts' o encoder gera por volta completa dele mesmo (KY-040 comum = 20)")]
    [SerializeField] private float countsPerEncoderRevolution = 20f;

    [Tooltip("Quantas voltas do ENCODER equivalem a 1 volta do VOLANTE (você mediu ~4,5)")]
    [SerializeField] private float encoderRevolutionsPerWheelRevolution = 4.5f;

    [Header("Limites do volante")]
    [Tooltip("Ângulo máximo para cada lado, em graus (450 = 900° ponta a ponta, padrão de volante de sim racing)")]
    [SerializeField] private float maxSteeringAngle = 450f;

    [Header("Calibração de centro")]
    [Tooltip("Tecla de recentralização, usando o novo Input System (ex: r, space, enter)")]
    [SerializeField] private Key recenterKey = Key.R;
    private long centerOffsetCounts = 0;
    private bool hasCalibrated = false;

    [Header("Visual (opcional)")]
    [Tooltip("Transform do modelo 3D do volante, gira no eixo Z local")]
    [SerializeField] private Transform wheelVisualTransform;

    [Header("Física do carro (opcional)")]
    [Tooltip("WheelColliders da frente, se estiver usando a física de veículo padrão da Unity")]
    [SerializeField] private WheelCollider[] frontWheels;
    [Tooltip("Ângulo máximo de esterço nas rodas do carro (geralmente bem menor que o do volante, ex: 30-35°)")]
    [SerializeField] private float maxCarSteerAngle = 35f;

    [Tooltip("Suaviza a leitura para evitar tremulação/jitter do encoder")]
    [SerializeField] private float smoothing = 15f;

    // ---- Valores públicos, prontos pra qualquer outro script consumir ----

    /// Ângulo atual do volante em graus, já calibrado e limitado (-maxSteeringAngle a +maxSteeringAngle)
    public float SteeringAngleDegrees { get; private set; }

    /// Ângulo suavizado (use este para rotação visual e para o carro)
    public float SteeringAngleSmoothed { get; private set; }

    /// Valor normalizado entre -1 (esquerda total) e +1 (direita total)
    public float SteeringNormalized => SteeringAngleSmoothed / maxSteeringAngle;

    private float countsPerWheelRevolution =>
        countsPerEncoderRevolution * encoderRevolutionsPerWheelRevolution;

    private void Update()
    {
        if (SerialCounterReader.Instance == null)
            return;

        // Recentraliza: o counter atual passa a valer como "0 graus"
        if (Keyboard.current != null && Keyboard.current[recenterKey].wasPressedThisFrame)
        {
            Recenter();
        }

        long rawCounter = SerialCounterReader.Instance.Counter;

        if (!hasCalibrated)
        {
            // Primeira leitura: centraliza automaticamente nela pra não começar torto
            centerOffsetCounts = rawCounter;
            hasCalibrated = true;
        }

        long relativeCounts = rawCounter - centerOffsetCounts;

        // counts -> graus
        float rawDegrees = (relativeCounts / countsPerWheelRevolution) * 360f;

        // Limita ao curso físico do volante
        SteeringAngleDegrees = Mathf.Clamp(rawDegrees, -maxSteeringAngle, maxSteeringAngle);

        // Suaviza pra não tremer
        SteeringAngleSmoothed = Mathf.Lerp(
            SteeringAngleSmoothed,
            SteeringAngleDegrees,
            Time.deltaTime * smoothing
        );

        ApplyVisual();
        ApplyCarSteering();
    }

    /// Zera a referência de centro na posição atual do volante físico
    public void Recenter()
    {
        if (SerialCounterReader.Instance == null)
            return;

        centerOffsetCounts = SerialCounterReader.Instance.Counter;
        hasCalibrated = true;
        SteeringAngleDegrees = 0f;
        SteeringAngleSmoothed = 0f;

        Debug.Log("[SteeringWheelController] Volante recentralizado.");
    }

    private void ApplyVisual()
    {
        if (wheelVisualTransform == null)
            return;

        // Gira o modelo 3D no eixo Z local (ajuste o eixo se seu modelo estiver
        // orientado diferente — troca por localEulerAngles.x ou .y se precisar)
        wheelVisualTransform.localRotation = Quaternion.Euler(0f, 0f, -SteeringAngleSmoothed);
    }

    private void ApplyCarSteering()
    {
        if (frontWheels == null || frontWheels.Length == 0)
            return;

        float carSteerAngle = SteeringNormalized * maxCarSteerAngle;

        foreach (var wheel in frontWheels)
        {
            if (wheel != null)
                wheel.steerAngle = carSteerAngle;
        }
    }
}
