/*
  encoder_counter.ino
  --------------------
  Lê um encoder rotativo KY-040 usando interrupções e mantém um contador
  com sinal (long), sem limite superior/inferior (vai de -2.147.483.648
  até +2.147.483.647, que na prática é "infinito" para esse uso).

  O contador é enviado pela Serial em formato texto, terminado em '\n',
  sempre que o valor muda. Ex: "42\n", "-13\n", "0\n"

  Pinos do KY-040 (ajuste conforme sua fiação):
    CLK -> pino 2  (precisa suportar interrupção)
    DT  -> pino 3  (precisa suportar interrupção)
    SW  -> pino 4  (botão do encoder, opcional)

  No Arduino Leonardo, os pinos com interrupção são: 0, 1, 2, 3, 7
*/

const uint8_t PIN_CLK = 2;
const uint8_t PIN_DT  = 3;
const uint8_t PIN_SW  = 4;

// Contador principal — usar volatile pois é alterado dentro da interrupção
volatile long counter = 0;

// Guarda o último estado do CLK para detectar a borda
volatile uint8_t lastCLK = 0;

// Envia o valor só quando ele muda, para não floodar a Serial
long lastSentCounter = 0;

void setup() {
  Serial.begin(115200);

  pinMode(PIN_CLK, INPUT_PULLUP);
  pinMode(PIN_DT, INPUT_PULLUP);
  pinMode(PIN_SW, INPUT_PULLUP);

  lastCLK = digitalRead(PIN_CLK);

  // Interrompe tanto na subida quanto na descida do CLK para máxima resolução
  attachInterrupt(digitalPinToInterrupt(PIN_CLK), handleEncoder, CHANGE);

  // Envia o valor inicial (0)
  Serial.println(counter);
}

void loop() {
  // Envia o contador toda vez que ele mudar desde o último envio
  noInterrupts();
  long current = counter;
  interrupts();

  if (current != lastSentCounter) {
    Serial.println(current);
    lastSentCounter = current;
  }
}

void handleEncoder() {
  uint8_t clkState = digitalRead(PIN_CLK);
  uint8_t dtState  = digitalRead(PIN_DT);

  // Só processa na transição de CLK (evita contar duas vezes por passo)
  if (clkState != lastCLK) {
    // Se DT for diferente de CLK, girou em um sentido; se igual, no outro
    if (dtState != clkState) {
      counter++;
    } else {
      counter--;
    }
  }

  lastCLK = clkState;
}
