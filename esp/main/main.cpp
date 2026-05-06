#include <Arduino.h>
#include <cstdint>

uint8_t redLed = 14;

void setup() {
    Serial.begin(115200);
    pinMode(redLed, OUTPUT);

}

void loop() {
    digitalWrite(redLed, HIGH);
    delay(500);
    digitalWrite(redLed, LOW);
    delay(500);
}
