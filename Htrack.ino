#include <WiFi.h>
#include <HTTPClient.h>
#include <SPI.h>
#include <MFRC522.h>

// WiFi credentials
// const char* ssid = "Farxod_wifi";
// const char* password = "542130Deco";
// const char* ssid = "IlmHub";
const char* ssid = "Uztelecom2,5G";
const char* password = "12341234";
// const char* password = "IlmHub1234";

const char* serverBase = "https://htrack.ilmhub.uz/api/Attendances/create-attendance";
const char* companyId = "9f3ac220-bfaf-4da7-93da-914cb0baf33d";

// RFID pins
#define RST_PIN 22
#define SS_PIN  21

// Buzzer pin
#define BUZZER_PIN 27

// RFID instance
MFRC522 mfrc522(SS_PIN, RST_PIN);

// ==================== Buzzer Functions ====================

// Play melody on WiFi connection
void playWiFiConnectedMelody() {
  tone(BUZZER_PIN, 1000, 150);
  delay(200);
  tone(BUZZER_PIN, 1200, 150);
  delay(200);
  tone(BUZZER_PIN, 1500, 300);
  delay(300);
  noTone(BUZZER_PIN);
}

// Short beep for card read
void shortBeep() {
  tone(BUZZER_PIN, 1000, 100);
  delay(100);
  noTone(BUZZER_PIN);
}

// Long buzz for error
void errorBuzz() {
  tone(BUZZER_PIN, 400, 600);
  delay(600);
  noTone(BUZZER_PIN);
}

// ==================== Setup ====================
void setup() {
  Serial.begin(115200);
  delay(100);

  pinMode(BUZZER_PIN, OUTPUT);
  digitalWrite(BUZZER_PIN, LOW);

  // Connect to WiFi
  WiFi.begin(ssid, password);
  Serial.print("Connecting to WiFi...");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nConnected!");
  playWiFiConnectedMelody();

  // Initialize RFID
  SPI.begin(18, 19, 23);
  mfrc522.PCD_Init();
  Serial.println("Scan a card...");
}

// ==================== Loop ====================
void loop() {
  if (!mfrc522.PICC_IsNewCardPresent()) return;
  if (!mfrc522.PICC_ReadCardSerial()) return;

  shortBeep();  // Feedback for card detection

  // Format UID
  String uid = "";
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    if (mfrc522.uid.uidByte[i] < 0x10) uid += "0";
    uid += String(mfrc522.uid.uidByte[i], HEX);
    if (i < mfrc522.uid.size - 1) uid += "%20";
  }
  uid.toUpperCase();

  Serial.print("Card UID: ");
  Serial.println(uid);

  // Construct URL
  String url = String(serverBase) + "/" + companyId + "/" + uid;

  // Send POST
  HTTPClient http;
  http.begin(url);
  http.addHeader("Content-Type", "application/json");

  int httpCode = http.POST("");  // Empty body

  if (httpCode > 0) {
    Serial.printf("POST sent. Response code: %d\n", httpCode);
    String payload = http.getString();
    Serial.println(payload);

    // Handle failed request (400+)
    if (httpCode >= 400) {
      errorBuzz();
    }
  } else {
    Serial.printf("POST failed. Error: %s\n", http.errorToString(httpCode).c_str());
    errorBuzz();
  }

  http.end();

  // Reset reader
  delay(2000);
  mfrc522.PICC_HaltA();
  mfrc522.PCD_StopCrypto1();
}
