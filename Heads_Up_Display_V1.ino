#include <iostream>
#include <list>
#include <WiFi.h>
#include "AsyncUDP.h"

// initialises all the vaiables and conststants
const int DisplayWidth = 320;
const int DisplayHeight = 172;

// Wifi Details
const char *WiFiSSID = ""; // Set this to your WiFis SSID             // Remove SSID and Passowrd before git push
const char *WiFiPassword = ""; // Set this to your WiFis Password

// UDP Connection Details
AsyncUDP udp;
const int ListenPort = 11000;


void setup() {
  Serial.begin(115200);
  delay(1000); // Waits 1 second to let the serial interface initalise

  // Connect to the WiFi
  ConnectToWiFi();

  main();
}


void ConnectToWiFi() {
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


std::vector<uint8_t> StitchedPacketData;
int LengthOfImage;


int main() {
    while (WiFi.status() == WL_CONNECTED) {

        if (udp.listen(ListenPort)) { // Starts the UDP Listening

            udp.onPacket([](AsyncUDPPacket packet) { // Runs Code when a Packet is received

                Serial.print("Data: ");
                Serial.write(packet.data(), packet.length());
                Serial.println();
                
                PacketHandeler(packet); // Runs all the handeling with the packets
            });
        }
    }

    return 0;
}


std::vector<uint8_t> RLEDecode(std::vector<uint8_t> CompressedImage) {
  std::vector<uint8_t> DecompressedImage;
  uint16_t RunLength;
  uint16_t Offset;
  uint8_t Pixel;

  while (Offset < LengthOfImage) {
    RunLength = (CompressedImage[Offset] << 8 | CompressedImage[Offset + 1]);

    for (uint16_t i = 0; i < RunLength; i++) {
      DecompressedImage.push_back(CompressedImage[Offset + 2]);
    }
    Offset += 3;
  }
  return DecompressedImage;
}


void PacketHandeler(AsyncUDPPacket packet) {
  uint8_t NumOfPackets;
  uint8_t NumOfCountedPackets;
  // If it is the first packet of the packets and contains only 3 bytes those 3bytes contain the size of the image and how many packets are going to be sent
  if (StitchedPacketData.size() == 0 && packet.length() == 3) {
    LengthOfImage = (packet.data()[0] << 8) | packet.data()[1];
    NumOfPackets = packet.data()[2];
    return;
  }
    // Append packet bytes to stitched buffer
    StitchedPacketData.insert(
      StitchedPacketData.end(),
      packet.data(),
      packet.data() + packet.length()
    );
    NumOfCountedPackets++; // Countes how many packets have been sent

    if (StitchedPacketData.size() == LengthOfImage) {
      if (StitchedPacketData.size() < 55040) { // Detects if the data that has been received is the Compressed image or the Uncompressed image
        StitchedPacketData = RLEDecode(StitchedPacketData);
      }

      //Serial.print(StitchedPacketData);
      // Code for the dissplay goes HERE

      StitchedPacketData.clear();
    }
    
    if (packet.length() < 1024) {
      if (NumOfCountedPackets < NumOfPackets) {
        Serial.print("Bad Packets (Not Enough Packets!)");
      }
        StitchedPacketData.clear();
        return;
    }

    // If the number of received packets is bigger than the number of expected packets just delete all received packets (this will be changed later to a better system but it will work like this for now)
    if (NumOfCountedPackets > NumOfPackets) {
        StitchedPacketData.clear();
        return;
    }
}


void loop() {
  Serial.print("WiFi Disconnected...");
  ConnectToWiFi();
  main();
}
