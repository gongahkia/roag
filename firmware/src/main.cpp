#include <Arduino.h>
#include <ArduinoJson.h>
#include <TFT_eSPI.h>
#include <WebServer.h>
#include <WiFi.h>

#if __has_include("secrets.h")
#include "secrets.h"
#else
#include "secrets.example.h"
#endif

namespace {

constexpr uint8_t kPowerPin = 15;
constexpr uint8_t kButton1Pin = 0;
constexpr uint8_t kButton2Pin = 14;
constexpr unsigned long kWifiConnectTimeoutMs = 15000;
constexpr unsigned long kWifiRetryMs = 10000;
constexpr unsigned long kButtonDebounceMs = 250;

enum class DisplayState { Booting, Offline, Idle, Running, Done };

TFT_eSPI display;
WebServer server(80);
DisplayState displayState = DisplayState::Booting;
String currentProject = "Codex";
unsigned long wifiAttemptStarted = 0;
unsigned long nextWifiRetry = 0;
unsigned long lastButtonPress = 0;
bool wifiAttemptActive = false;
bool wasWifiConnected = false;
bool previousButtonPressed = false;

const char* stateName() {
  switch (displayState) {
    case DisplayState::Booting:
      return "booting";
    case DisplayState::Offline:
      return "offline";
    case DisplayState::Idle:
      return "idle";
    case DisplayState::Running:
      return "running";
    case DisplayState::Done:
      return "done";
  }
  return "unknown";
}

String safeProject(String project) {
  project.trim();
  project.replace('\n', ' ');
  project.replace('\r', ' ');
  if (project.isEmpty()) {
    return "Codex";
  }
  if (project.length() > 32) {
    project = project.substring(0, 32);
  }
  return project;
}

void drawCentered(const char* text, int y, int font) {
  display.drawCentreString(text, display.width() / 2, y, font);
}

void drawProject(int y, int font) {
  display.drawCentreString(currentProject.c_str(), display.width() / 2, y, font);
}

void drawState() {
  display.fillScreen(TFT_BLACK);
  display.setTextColor(TFT_WHITE, TFT_BLACK);
  display.setTextDatum(TC_DATUM);

  switch (displayState) {
    case DisplayState::Booting:
      drawCentered("CODEX", 18, 2);
      drawCentered("BEACON", 44, 2);
      drawCentered("CONNECTING...", 104, 2);
      break;
    case DisplayState::Offline:
      drawCentered("CODEX", 18, 2);
      drawCentered("BEACON", 44, 2);
      display.setTextColor(TFT_ORANGE, TFT_BLACK);
      drawCentered("OFFLINE", 104, 2);
      display.setTextColor(TFT_WHITE, TFT_BLACK);
      break;
    case DisplayState::Idle:
      drawCentered("CODEX", 30, 2);
      display.setTextColor(TFT_GREEN, TFT_BLACK);
      drawCentered("READY", 92, 4);
      display.setTextColor(TFT_WHITE, TFT_BLACK);
      break;
    case DisplayState::Running:
      drawCentered("CODEX", 12, 2);
      display.setTextColor(TFT_CYAN, TFT_BLACK);
      drawCentered("RUNNING", 48, 3);
      display.setTextColor(TFT_WHITE, TFT_BLACK);
      drawProject(112, 2);
      break;
    case DisplayState::Done:
      display.setTextColor(TFT_GREEN, TFT_BLACK);
      display.drawLine(145, 28, 155, 38, 3);
      display.drawLine(155, 38, 177, 15, 3);
      drawCentered("DONE", 65, 5);
      display.setTextColor(TFT_WHITE, TFT_BLACK);
      drawProject(132, 2);
      break;
  }
}

void setState(DisplayState next) {
  displayState = next;
  drawState();
  Serial.printf("state=%s project=%s\n", stateName(), currentProject.c_str());
}

void startWifiAttempt() {
  WiFi.disconnect();
  WiFi.begin(CODEX_BEACON_WIFI_SSID, CODEX_BEACON_WIFI_PASSWORD);
  wifiAttemptStarted = millis();
  wifiAttemptActive = true;
  Serial.printf("wifi connecting ssid=%s\n", CODEX_BEACON_WIFI_SSID);
}

void maintainWifi() {
  const bool connected = WiFi.status() == WL_CONNECTED;
  if (connected) {
    wifiAttemptActive = false;
    if (!wasWifiConnected) {
      wasWifiConnected = true;
      server.begin();
      Serial.printf("wifi connected ip=%s\n", WiFi.localIP().toString().c_str());
      if (displayState == DisplayState::Booting || displayState == DisplayState::Offline) {
        setState(DisplayState::Idle);
      }
    }
    return;
  }

  if (wasWifiConnected) {
    wasWifiConnected = false;
    Serial.println("wifi disconnected");
    if (displayState == DisplayState::Idle) {
      setState(DisplayState::Offline);
    }
  }

  const unsigned long now = millis();
  if (wifiAttemptActive && now - wifiAttemptStarted >= kWifiConnectTimeoutMs) {
    wifiAttemptActive = false;
    nextWifiRetry = now + kWifiRetryMs;
    if (displayState == DisplayState::Booting) {
      setState(DisplayState::Offline);
    }
    Serial.println("wifi timeout; retrying");
  }

  if (!wifiAttemptActive && now >= nextWifiRetry) {
    startWifiAttempt();
  }
}

void sendError(int status, const char* message) {
  String body = String("{\"ok\":false,\"error\":\"") + message + "\"}";
  server.send(status, "application/json", body);
}

void handleHealth() {
  String body = String("{\"ok\":true,\"state\":\"") + stateName() +
                "\",\"ip\":\"" + WiFi.localIP().toString() + "\"}";
  server.send(200, "application/json", body);
}

void handleEvent() {
  if (!server.hasArg("plain")) {
    sendError(400, "missing JSON body");
    return;
  }

  const String raw = server.arg("plain");
  if (raw.length() > 512) {
    sendError(413, "request too large");
    return;
  }

  JsonDocument document;
  const DeserializationError parseError = deserializeJson(document, raw);
  if (parseError) {
    sendError(400, "invalid JSON");
    return;
  }

  const char* type = document["type"] | "";
  String project = document["project"] | "";
  project = safeProject(project);

  if (strcmp(type, "started") == 0) {
    currentProject = project;
    setState(DisplayState::Running);
  } else if (strcmp(type, "done") == 0) {
    currentProject = project;
    setState(DisplayState::Done);
  } else if (strcmp(type, "idle") == 0) {
    setState(DisplayState::Idle);
  } else {
    sendError(400, "type must be started, done, or idle");
    return;
  }

  server.send(200, "application/json", "{\"ok\":true}");
}

void handleNotFound() {
  sendError(404, "not found");
}

void checkButtons() {
  const bool pressed = digitalRead(kButton1Pin) == LOW || digitalRead(kButton2Pin) == LOW;
  const unsigned long now = millis();
  if (displayState == DisplayState::Done && pressed && !previousButtonPressed &&
      now - lastButtonPress >= kButtonDebounceMs) {
    lastButtonPress = now;
    setState(DisplayState::Idle);
  }
  previousButtonPressed = pressed;
}

}  // namespace

void setup() {
  Serial.begin(115200);
  delay(100);

  pinMode(kPowerPin, OUTPUT);
  digitalWrite(kPowerPin, HIGH);
  pinMode(kButton1Pin, INPUT_PULLUP);
  pinMode(kButton2Pin, INPUT_PULLUP);

  display.init();
  display.setRotation(1);
  display.fillScreen(TFT_BLACK);
  drawState();

  server.on("/health", HTTP_GET, handleHealth);
  server.on("/event", HTTP_POST, handleEvent);
  server.onNotFound(handleNotFound);

  WiFi.mode(WIFI_STA);
  WiFi.setSleep(false);
  startWifiAttempt();
}

void loop() {
  maintainWifi();
  if (WiFi.status() == WL_CONNECTED) {
    server.handleClient();
  }
  checkButtons();
  delay(2);
}
