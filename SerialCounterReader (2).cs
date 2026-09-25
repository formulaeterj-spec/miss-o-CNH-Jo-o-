/*
  SerialCounterReader.cs
  -----------------------
  Lê, em uma thread separada (para não travar o jogo), o contador
  enviado pelo Arduino via Serial e expõe o valor atual como "Counter"
  (long, sem limite de -infinito a +infinito na prática).

  Como usar:
  1. Crie um GameObject vazio na cena (ex: "SerialManager").
  2. Arraste este script para ele.
  3. No Inspector, ajuste "Port Name" para a porta correta:
       Windows: "COM3", "COM4", etc.
       Mac/Linux: "/dev/tty.usbmodemXXXX" ou "/dev/ttyACM0"
  4. Ajuste "Baud Rate" para 115200 (igual ao definido no Arduino).
  5. Outros scripts podem ler o valor via:
       SerialCounterReader.Instance.Counter

  Requer que o projeto NÃO esteja usando IL2CPP com restrição de threads
  incomuns — funciona normalmente em builds padrão (Windows/Mac/Linux/Editor).
*/

using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialCounterReader : MonoBehaviour
{
    public static SerialCounterReader Instance { get; private set; }

    [Header("Configuração da porta serial")]
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private int readTimeoutMs = 50;

    // Valor público, atualizado a cada frame a partir do valor lido na thread
    public long Counter { get; private set; } = 0;

    private SerialPort serialPort;
    private Thread readThread;
    private volatile bool keepReading = false;

    // Comunicação thread-safe entre a thread de leitura e a Main Thread
    private readonly object lockObj = new object();
    private long latestCounterFromThread = 0;
    private bool hasNewValue = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        OpenSerialPort();
    }

    private void OpenSerialPort()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = readTimeoutMs,
                NewLine = "\n"
            };
            serialPort.Open();

            keepReading = true;
            readThread = new Thread(ReadSerialLoop)
            {
                IsBackground = true
            };
            readThread.Start();

            Debug.Log($"[SerialCounterReader] Porta {portName} aberta com sucesso.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SerialCounterReader] Falha ao abrir a porta {portName}: {e.Message}");
        }
    }

    // Executa em thread separada — nunca chame métodos do Unity aqui além de leitura de dados simples
    private void ReadSerialLoop()
    {
        while (keepReading && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string line = serialPort.ReadLine(); // bloqueia até timeout ou '\n'

                if (long.TryParse(line.Trim(), out long parsedValue))
                {
                    lock (lockObj)
                    {
                        latestCounterFromThread = parsedValue;
                        hasNewValue = true;
                    }
                }
            }
            catch (TimeoutException)
            {
                // Normal — apenas não chegou nada dentro do timeout, continua o loop
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SerialCounterReader] Erro na leitura serial: {e.Message}");
            }
        }
    }

    private void Update()
    {
        // Passa o valor lido na thread para a Main Thread de forma segura
        if (hasNewValue)
        {
            lock (lockObj)
            {
                Counter = latestCounterFromThread;
                hasNewValue = false;
            }
        }
    }

    private void OnDisable()
    {
        CloseSerialPort();
    }

    private void OnApplicationQuit()
    {
        CloseSerialPort();
    }

    private void CloseSerialPort()
    {
        keepReading = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(200);
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("[SerialCounterReader] Porta serial fechada.");
        }
    }
}
