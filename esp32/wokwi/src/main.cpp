#include <Arduino.h>

const float BETA = 3950;

void setup() {
    pinMode(33, INPUT);  
    Serial.begin(115200);
}

void loop() {
    int32_t tempRead = analogRead(33);

    if (tempRead <= 0) {
        Serial.println("[-] Error: invalid reading");
        delay(1000);
        return; 
    }

    float celsius = 1.0 / (log(1.0 / (4095.0 / tempRead - 1)) / BETA + 1.0 / 298.15) - 273.15;
    Serial.print("[+] Temperature: ");
    Serial.print(celsius);
    Serial.println(" °C");
    delay(1000);    
}