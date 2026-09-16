#include <Arduino.h>
#include <ArduinoJson.h>
#include <TFT_eSPI.h>
#include <WebServer.h>
#include <WiFi.h>

#if __has_include("secrets.h")
#include "secrets.h"
#else
#error "Missing include/secrets.h. Copy secrets.example.h and set your Wi-Fi credentials."
#define CODEX_BEACON_WIFI_SSID ""
#define CODEX_BEACON_WIFI_PASSWORD ""
#endif

namespace {

constexpr uint8_t kPowerPin = 15;
constexpr uint8_t kButton1Pin = 0;
constexpr uint8_t kButton2Pin = 14;
constexpr unsigned long kWifiConnectTimeoutMs = 15000;
constexpr unsigned long kWifiRetryMs = 10000;
constexpr unsigned long kButtonDebounceMs = 250;
constexpr int16_t kCatX = 212;
constexpr int16_t kCatY = 56;
constexpr int16_t kCatWidth = 100;
constexpr int16_t kCatHeight = 108;
constexpr int16_t kTextCenterX = 108;

enum class DisplayState { Booting, Offline, Idle, Running, Done };

TFT_eSPI display;
TFT_eSprite catSprite(&display);
WebServer server(80);
DisplayState displayState = DisplayState::Booting;
DisplayState lastCatState = DisplayState::Booting;
String currentProject = "Codex";
unsigned long wifiAttemptStarted = 0;
unsigned long nextWifiRetry = 0;
unsigned long lastButtonPress = 0;
uint8_t lastCatFrame = 255;
bool catSpriteReady = false;
bool wifiAttemptActive = false;
bool wasWifiConnected = false;
bool previousButtonPressed = false;

constexpr uint16_t kCatFur = 0xFD20;
constexpr uint16_t kCatFurDark = 0xC380;
constexpr uint16_t kCatCream = 0xFF59;
constexpr uint16_t kCatPink = 0xFB76;
constexpr uint16_t kCatInk = 0x2945;
constexpr uint16_t kCatEye = 0x0010;
constexpr uint16_t kLaptop = 0x39E7;
constexpr uint16_t kLaptopGlow = 0x07FF;

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
  if (project.length() > 16) {
    project = project.substring(0, 16);
  }
  return project;
}

void drawCentered(const char* text, int y, int font) {
  display.drawCentreString(text, kTextCenterX, y, font);
}

void drawProject(int y) {
  display.drawString(currentProject.c_str(), 12, y, 2);
}

uint8_t frameCount() {
  switch (displayState) {
    case DisplayState::Booting:
    case DisplayState::Offline:
      return 4;
    case DisplayState::Idle:
      return 6;
    case DisplayState::Running:
      return 8;
    case DisplayState::Done:
      return 10;
  }
  return 1;
}

unsigned long frameIntervalMs() {
  switch (displayState) {
    case DisplayState::Booting:
      return 240;
    case DisplayState::Offline:
      return 320;
    case DisplayState::Idle:
      return 160;
    case DisplayState::Running:
      return 120;
    case DisplayState::Done:
      return 110;
  }
  return 250;
}

uint8_t currentCatFrame() {
  return (millis() / frameIntervalMs()) % frameCount();
}

void drawTail(int phase, int bodyY, bool celebratory, bool worried) {
  const int startX = 29;
  const int startY = bodyY + 30;
  int tipX = 7;
  int tipY = bodyY + 17;
  if (celebratory) {
    tipX = phase % 2 == 0 ? 9 : 18;
    tipY = bodyY + 5 - (phase % 3) * 4;
  } else if (worried) {
    tipX = phase % 2 == 0 ? 16 : 9;
    tipY = bodyY + 39;
  } else {
    tipX = phase % 2 == 0 ? 5 : 13;
    tipY = bodyY + (phase % 3 == 0 ? 12 : 20);
  }
  for (int offset = -2; offset <= 2; ++offset) {
    catSprite.drawLine(startX, startY + offset, tipX, tipY + offset, kCatFurDark);
  }
  catSprite.fillRect(tipX - 2, tipY - 2, 5, 5, kCatFur);
}

void drawConfetti(uint8_t frame) {
  const uint16_t colors[] = {TFT_YELLOW, TFT_CYAN, TFT_MAGENTA, TFT_GREEN};
  for (uint8_t i = 0; i < 8; ++i) {
    const int x = (i * 17 + frame * 7) % 92 + 3;
    const int y = (i * 23 + frame * 11) % 48 + 2;
    catSprite.fillRect(x, y, 3, 5, colors[i % 4]);
  }
}

void drawCat(uint8_t frame) {
  if (!catSpriteReady) {
    return;
  }

  catSprite.fillSprite(TFT_BLACK);
  const bool running = displayState == DisplayState::Running;
  const bool done = displayState == DisplayState::Done;
  const bool offline = displayState == DisplayState::Offline;
  const bool booting = displayState == DisplayState::Booting;
  const int bob = done ? (frame % 2 == 0 ? -4 : 1) : (frame % 3 == 0 ? -1 : 0);
  const int headX = 35 + (booting && frame % 4 == 3 ? 3 : 0);
  const int headY = 18 + bob;
  const int bodyX = 37;
  const int bodyY = 53 + bob;
  const bool blink = !running && !done && (frame == 4 || (offline && frame == 2));
  const bool pawsUp = done && frame % 3 != 0;

  if (done) {
    drawConfetti(frame);
  }
  drawTail(frame, bodyY, done, offline);

  catSprite.fillRect(bodyX + 5, bodyY, 34, 35, kCatFur);
  catSprite.fillRect(bodyX + 1, bodyY + 8, 42, 24, kCatFur);
  catSprite.fillRect(bodyX + 12, bodyY + 22, 20, 14, kCatCream);
  catSprite.fillRect(bodyX + 3, bodyY + 30, 13, 7, kCatFurDark);
  catSprite.fillRect(bodyX + 29, bodyY + 30, 13, 7, kCatFurDark);

  catSprite.fillRect(headX + 4, headY + 6, 42, 34, kCatFur);
  catSprite.fillRect(headX, headY + 15, 50, 22, kCatFur);
  catSprite.fillTriangle(headX + 4, headY + 16, headX + 11, headY, headX + 19, headY + 16, kCatFur);
  catSprite.fillTriangle(headX + 31, headY + 16, headX + 40, headY, headX + 47, headY + 16, kCatFur);
  catSprite.fillTriangle(headX + 8, headY + 13, headX + 11, headY + 5, headX + 15, headY + 14, kCatPink);
  catSprite.fillTriangle(headX + 35, headY + 14, headX + 40, headY + 5, headX + 43, headY + 15, kCatPink);
  catSprite.fillRect(headX + 9, headY + 24, 9, 10, kCatCream);
  catSprite.fillRect(headX + 32, headY + 24, 9, 10, kCatCream);
  if (blink) {
    catSprite.fillRect(headX + 11, headY + 27, 7, 2, kCatEye);
    catSprite.fillRect(headX + 33, headY + 27, 7, 2, kCatEye);
  } else {
    catSprite.fillRect(headX + 13, headY + 25, 4, 7, kCatEye);
    catSprite.fillRect(headX + 34, headY + 25, 4, 7, kCatEye);
    catSprite.fillRect(headX + 14, headY + 26, 1, 2, TFT_WHITE);
    catSprite.fillRect(headX + 35, headY + 26, 1, 2, TFT_WHITE);
  }
  catSprite.fillRect(headX + 23, headY + 32, 5, 3, kCatPink);
  catSprite.drawLine(headX + 25, headY + 35, headX + 25, headY + 38, kCatInk);
  catSprite.drawLine(headX + 20, headY + 38, headX + 25, headY + 38, kCatInk);
  catSprite.drawLine(headX + 25, headY + 38, headX + 30, headY + 38, kCatInk);
  catSprite.drawLine(headX + 4, headY + 34, headX + 17, headY + 36, kCatInk);
  catSprite.drawLine(headX + 33, headY + 36, headX + 47, headY + 34, kCatInk);

  if (running) {
    catSprite.fillRoundRect(3, 72, 31, 20, 2, kLaptop);
    catSprite.fillRect(6, 75, 25, 13, kLaptopGlow);
    catSprite.fillRect(1, 92, 37, 4, kCatInk);
    const int pawY = frame % 2 == 0 ? bodyY + 17 : bodyY + 21;
    catSprite.fillRect(bodyX - 7, pawY, 16, 7, kCatCream);
    catSprite.fillRect(bodyX + 31, bodyY + (frame % 2 == 0 ? 21 : 17), 16, 7, kCatCream);
  } else if (pawsUp) {
    catSprite.fillRect(bodyX - 7, bodyY - 9 - (frame % 2) * 6, 9, 18, kCatFur);
    catSprite.fillRect(bodyX + 40, bodyY - 9 - ((frame + 1) % 2) * 6, 9, 18, kCatFur);
    catSprite.fillRect(bodyX - 9, bodyY - 10 - (frame % 2) * 6, 12, 7, kCatCream);
    catSprite.fillRect(bodyX + 39, bodyY - 10 - ((frame + 1) % 2) * 6, 12, 7, kCatCream);
  } else {
    catSprite.fillRect(bodyX - 4, bodyY + 19, 15, 8, kCatCream);
    catSprite.fillRect(bodyX + 31, bodyY + 19, 15, 8, kCatCream);
  }

  if (offline) {
    catSprite.drawLine(87, 58, 91, 64, TFT_ORANGE);
    catSprite.drawLine(91, 64, 95, 58, TFT_ORANGE);
    catSprite.fillRect(89, 68, 4, 10, TFT_ORANGE);
    catSprite.fillRect(89, 81, 4, 4, TFT_ORANGE);
  }
  if (booting) {
    catSprite.fillRect(83, 28, 4, 4, TFT_CYAN);
    catSprite.fillRect(90, 22, 3, 3, TFT_CYAN);
  }

  catSprite.pushSprite(kCatX, kCatY);
}

void tickAnimation(bool force = false) {
  if (!catSpriteReady) {
    return;
  }
  const uint8_t frame = currentCatFrame();
  if (force || displayState != lastCatState || frame != lastCatFrame) {
    drawCat(frame);
    lastCatState = displayState;
    lastCatFrame = frame;
  }
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
      break;
    case DisplayState::Idle:
      drawCentered("CODEX", 30, 2);
      display.setTextColor(TFT_GREEN, TFT_BLACK);
      drawCentered("READY", 82, 4);
      break;
    case DisplayState::Running:
      drawCentered("CODEX", 12, 2);
      display.setTextColor(TFT_CYAN, TFT_BLACK);
      drawCentered("RUNNING", 48, 3);
      display.setTextColor(TFT_WHITE, TFT_BLACK);
      drawProject(126);
      break;
    case DisplayState::Done:
      display.setTextColor(TFT_GREEN, TFT_BLACK);
      display.drawLine(96, 30, 106, 40, 3);
      display.drawLine(106, 40, 128, 17, 3);
      drawCentered("DONE", 62, 5);
      display.setTextColor(TFT_WHITE, TFT_BLACK);
      drawProject(130);
      break;
  }
  display.setTextColor(TFT_WHITE, TFT_BLACK);
  tickAnimation(true);
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
                "\",\"locked\":" + (displayState == DisplayState::Done ? "true" : "false") +
                ",\"ip\":\"" + WiFi.localIP().toString() + "\"}";
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
  const bool knownType = strcmp(type, "started") == 0 || strcmp(type, "done") == 0 ||
                         strcmp(type, "idle") == 0;
  if (!knownType) {
    sendError(400, "type must be started, done, or idle");
    return;
  }
  if (displayState == DisplayState::Done) {
    sendError(409, "done must be acknowledged with a physical button");
    return;
  }

  String project = document["project"] | "";
  project = safeProject(project);
  if (strcmp(type, "started") == 0) {
    currentProject = project;
    setState(DisplayState::Running);
  } else if (strcmp(type, "done") == 0) {
    currentProject = project;
    setState(DisplayState::Done);
  } else {
    setState(DisplayState::Idle);
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

void initCatSprite() {
  catSprite.setColorDepth(16);
  catSpriteReady = catSprite.createSprite(kCatWidth, kCatHeight) != nullptr;
  if (!catSpriteReady) {
    Serial.println("cat sprite allocation failed; continuing without animation");
  }
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
  initCatSprite();
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
  tickAnimation();
  delay(2);
}
