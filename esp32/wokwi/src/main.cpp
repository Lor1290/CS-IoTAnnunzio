#include <DHT.h>  
#include <Wire.h> 
#include <Keypad.h>
#include <Arduino.h>
#include <Adafruit_BMP085.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/semphr.h>
#include <freertos/queue.h>

#include <cstdint>


#define DHTTYPE DHT22

#define PIN_DHT22 4
#define PIN_NTC 35
#define PIN_GAS 32
#define PIN_WIND 33
#define PIN_WATER 26
#define PIN_PHOTORESISTOR 34

#define QUEUE_SIZE  10
#define MSG_SIZE   256


// ----------------- //
// --- CONSTANTS --- //
// ----------------- //

const byte ROWS = 4;
const byte COLS = 4;


// ----------------- //
// --- VARIABLES --- //
// ----------------- //

byte keys[ROWS][COLS] = {
  {'1','2','3','A'},
  {'4','5','6','B'},
  {'7','8','9','C'},
  {'*','0','#','D'}
};

byte rowPins[ROWS] = {13, 12, 14, 27};
byte colPins[COLS] = {25, 19, 18,  5};

const String BPWD = "1234";
String IPWD = "";


// --------------- //
// --- OBJECTS --- //
// --------------- //

Keypad keypad = Keypad(makeKeymap(keys), rowPins, colPins, ROWS, COLS);
DHT dht(PIN_DHT22, DHTTYPE);
Adafruit_BMP085 bmp;


// ---------------- //
// --- FreeRTOS --- //
// ---------------- //

SemaphoreHandle_t i2cMutex;
QueueHandle_t printQueue;


// ------------------ //
// --- PROTOTYPES --- //
// ------------------ //

float readNTCTemperature(void);
void taskPrint(void* params);
void taskBMP180(void* params);
void taskAnalog(void* params);
void taskKeypad(void* params);
void taskWater(void* params);
void taskDHT22(void* params);


// ----------------- //
// --- FUNCTIONS --- //
// ----------------- //
float readNTCTemperature(void) {
  int32_t raw = analogRead(PIN_NTC);

  float voltage = raw * (3.3f / 4095.0f);
  float resistance = (3.3f - voltage) / voltage * 10000.0f;
  float steinhart  = resistance / 10000.0f;

  steinhart = log(steinhart);
  steinhart /= 3950.0f;
  steinhart += 1.0f / (25.0f + 273.15f);
  steinhart = 1.0f / steinhart;

  return steinhart - 273.15f;
}

void taskPrint(void* params) {
  char msg[MSG_SIZE];

  for (;;) {
    if (xQueueReceive(printQueue, msg, portMAX_DELAY) == pdTRUE)
      Serial.print(msg);
  }
}

void taskDHT22(void* params) {
  vTaskDelay(pdMS_TO_TICKS(0));

  for (;;) {
    float humidity = dht.readHumidity();
    float tempDHT  = dht.readTemperature();

    char msg[MSG_SIZE];
    if (!isnan(humidity) && !isnan(tempDHT))
      snprintf(msg, 
               MSG_SIZE, 
               "DATA:TEMP1_T:%.1f,TEMP1_H:%.1f\r\n", 
               tempDHT, 
               humidity
              );
    else
      snprintf(msg, 
               MSG_SIZE, 
               "DATA:TEMP1_T:0.0,TEMP1_H:0.0\r\n"
              );

    xQueueSend(printQueue, msg, portMAX_DELAY);
    vTaskDelay(pdMS_TO_TICKS(1000));
  }
}

void taskBMP180(void* params) {
  vTaskDelay(pdMS_TO_TICKS(100));

  for (;;) {
    xSemaphoreTake(i2cMutex, portMAX_DELAY);
    float tempBMP  = bmp.readTemperature();
    long  pressure = bmp.readPressure();
    xSemaphoreGive(i2cMutex);

    char msg[MSG_SIZE];
    snprintf(msg, 
             MSG_SIZE, 
             "DATA:TEMP2_T:%.1f,TEMP2_P:%ld\r\n", 
             tempBMP, 
             pressure
            );

    xQueueSend(printQueue, msg, portMAX_DELAY);
    vTaskDelay(pdMS_TO_TICKS(1000));
  }
}

void taskAnalog(void* params) {
  vTaskDelay(pdMS_TO_TICKS(200));

  for (;;) {
    int32_t lightRaw = analogRead(PIN_PHOTORESISTOR);
    int32_t windRaw = analogRead(PIN_WIND);
    int32_t gasRaw = analogRead(PIN_GAS);

    float lux = lightRaw * (10000.0f / 4095.0f);
    float tempNTC = readNTCTemperature();
    float windLevel = windRaw / 4095.0f;
    float gasLevel = gasRaw  / 4095.0f;

    char msg[MSG_SIZE];
    snprintf(msg, 
             MSG_SIZE,
             "DATA:TEMP3_T:%.1f,LUCE:%.0f,GAS:%.2f,VENTO:%.2f\r\n",
             tempNTC, 
             lux, 
             gasLevel, 
             windLevel
            );

    xQueueSend(printQueue, msg, portMAX_DELAY);

    if (gasLevel > 0.6f)
      xQueueSend(printQueue, (void*)"ALERT:GAS:high\r\n", portMAX_DELAY);
    vTaskDelay(pdMS_TO_TICKS(1000));
  }
}

void taskWater(void* params) {
  vTaskDelay(pdMS_TO_TICKS(300));
  bool lastState = false;

  for (;;) {
    bool waterDetected = !digitalRead(PIN_WATER);

    if (waterDetected != lastState) {
      char msg[MSG_SIZE];
      snprintf(msg, 
               MSG_SIZE, 
               "DATA:ACQUA:%d\r\n", 
               waterDetected ? 1 : 0
              );

      xQueueSend(printQueue, msg, portMAX_DELAY);

      if (waterDetected)
        xQueueSend(printQueue, (void*)"ALERT:ACQUA:critical\r\n", portMAX_DELAY);
      lastState = waterDetected;
    }

    vTaskDelay(pdMS_TO_TICKS(500));
  }
}

void taskKeypad(void* params) {
  vTaskDelay(pdMS_TO_TICKS(400));

  for (;;) {
    char key = keypad.getKey();

    if (key) {
      char msg[MSG_SIZE];
      if (key == '#') {
        snprintf(msg, 
                 MSG_SIZE, 
                 "[KEYPAD] %s\r\n", 
                 IPWD == BPWD ? "Password corretta" : "Password errata"
                );
        IPWD = "";
      } else if (key == '*') {
        IPWD = "";
        snprintf(msg, 
                 MSG_SIZE, 
                 "[KEYPAD] Input reset\r\n"
                );
      } else {
        IPWD += key;
        snprintf(msg, 
                 MSG_SIZE, 
                 "[KEYPAD] Input: %s\r\n", 
                 IPWD.c_str()
                );
      }
      xQueueSend(printQueue, msg, portMAX_DELAY);
    }

    vTaskDelay(pdMS_TO_TICKS(10));
  }
}

void setup() {
  Serial.begin(115200);

  delay(500);

  dht.begin();
  Wire.begin(21, 22);

  if (!bmp.begin())
    Serial.print("[-] BMP180 non trovato!\r\n");
  else
    Serial.print("[+] BMP180 inizializzato\r\n");

  pinMode(PIN_WATER, INPUT_PULLUP);

  i2cMutex   = xSemaphoreCreateMutex();
  printQueue = xQueueCreate(QUEUE_SIZE, MSG_SIZE);

  Serial.print("[+] Sistema avviato\r\n");

  xTaskCreate(taskPrint, "Print", 1024*2, NULL, 4, NULL);
  xTaskCreate(taskDHT22, "DHT22", 1024*2, NULL, 1, NULL);
  xTaskCreate(taskBMP180, "BMP180", 1024*2, NULL, 1, NULL);
  xTaskCreate(taskAnalog, "Analog", 1024*4, NULL, 1, NULL);
  xTaskCreate(taskWater, "Water", 1024*2, NULL, 2, NULL);
  xTaskCreate(taskKeypad, "Keypad", 1024*2, NULL, 3, NULL);
}

void loop() {
  vTaskDelete(NULL);
}