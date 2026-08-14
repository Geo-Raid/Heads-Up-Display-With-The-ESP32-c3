#include <iostream>
#include <WiFi.h>
#include "AsyncUDP.h"

// initialises all the vaiables and conststants
const int DisplayWidth = 320;
const int DisplayHeight = 172;

// Wifi Details
const char WiFiSSID[] = ""; // Set this to your WiFis SSID             // Remove SSID and Passowrd before git push
const char WiFiPassword[] = ""; // Set this to your WiFis Password

// UDP Connection Details
AsyncUDP udp;
const int ListenPort = 11000;


void setup() {
  Serial.begin(115200);
  delay(1000); // Waits 1 second to let the serial interface initalise

  // Connect to the WiFi
  WiFi.mode(WIFI_STA);
  WiFi.begin(WiFiSSID, WiFiPassword);
  Serial.println("Connecting to WiFi...");

  while (WiFi.status() != WL_CONNECTED) {
    delay(500); // Waits half a second between checking if connected to a WiFi
    Serial.println("...");
  }

  Serial.println("\nConnected to WiFi!");
  Serial.print("IP Address: ");
  Serial.println(WiFi.localIP());

}


void loop() {
  if (udp.listen(ListenPort)) {

  udp.onPacket([](AsyncUDPPacket packet) {
    Serial.print("Data: ");
    Serial.write(packet.data);
    Serial.println();
  });
}
}
