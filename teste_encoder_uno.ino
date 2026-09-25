// Teste de encoder rotativo KY-040 - Arduino Uno
// Objetivo: verificar se o encoder está funcionando corretamente

#define PIN_CLK 2   // precisa ser pino de interrupção (2 ou 3 no Uno)
#define PIN_DT  3
#define PIN_SW  4

volatile int contador = 0;
volatile int ultimoEstadoCLK;
volatile unsigned long ultimoTempo = 0;

void setup() {
  pinMode(PIN_CLK, INPUT_PULLUP);
  pinMode(PIN_DT, INPUT_PULLUP);
  pinMode(PIN_SW, INPUT_PULLUP);

  Serial.begin(9600);

  ultimoEstadoCLK = digitalRead(PIN_CLK);

  attachInterrupt(digitalPinToInterrupt(PIN_CLK), leituraEncoder, CHANGE);

  Serial.println("=== Teste do Encoder KY-040 (Uno) ===");
  Serial.println("Gire o encoder e observe os valores.");
  Serial.println("Pressione o botao para testar o switch.");
  Serial.println("--------------------------------------");
}

void loop() {
  static int contadorAnterior = 0;

  if (contador != contadorAnterior) {
    Serial.print("Contador: ");
    Serial.println(contador);
    contadorAnterior = contador;
  }

  if (digitalRead(PIN_SW) == LOW) {
    Serial.println(">>> Botao pressionado! <<<");
    delay(300); // debounce simples
  }

  delay(5);
}

void leituraEncoder() {
  // debounce por tempo (evita leituras falsas de ruido eletrico)
  unsigned long agora = millis();
  if (agora - ultimoTempo < 2) return;
  ultimoTempo = agora;

  int estadoCLK = digitalRead(PIN_CLK);

  if (estadoCLK != ultimoEstadoCLK) {
    int estadoDT = digitalRead(PIN_DT);

    if (estadoDT != estadoCLK) {
      contador++;
    } else {
      contador--;
    }
  }

  ultimoEstadoCLK = estadoCLK;
}
