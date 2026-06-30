#include <AccelStepper.h>
#include <EEPROM.h>
#include <ESP8266WebServer.h>
#include <ESP8266WiFi.h>
#include <ESP8266mDNS.h>
#include <WebSocketsServer.h>
#include <ctype.h>
#include <math.h>

#define STEP_PIN D1
#define DIR_PIN D2
#define ENABLE_PIN D5
#define HALL_PIN D6
#define CW_PIN D7
#define CCW_PIN D3

#define DEVICE_RESPONSE "CAA ESP8266 Rotator ver 2001"
#define FIRMWARE_VERSION 2001
#define EEPROM_SIZE 512
#define SETTINGS_MAGIC 0xCAAF8266UL
#define ASCOM_TCP_PORT 4030
#define WEBSOCKET_PORT 81
#define MAX_TCP_CLIENTS 4

const char *AP_PASSWORD = "012345678";
IPAddress apIp(192, 168, 4, 1);
IPAddress apGateway(192, 168, 4, 1);
IPAddress apSubnet(255, 255, 255, 0);

struct RotatorSettings {
  uint32_t magic;
  long position;
  int stepsPerDegree;
  long maxSteps;
  int maxSpeed;
  int acceleration;
  int manualMoveStepSize;
  int findHomeStepSize;
  bool hold;
  bool reversed;
  char staSsid[32];
  char staPassword[64];
  long homeOffsetSteps;
  char tcpPassword[16];
  char apPassword[32];
  char nickname[32];
  int backlashSteps;
};

RotatorSettings settings;
AccelStepper stepper(AccelStepper::DRIVER, STEP_PIN, DIR_PIN);
ESP8266WebServer server(80);
WebSocketsServer webSocket(WEBSOCKET_PORT);
WiFiServer tcpServer(ASCOM_TCP_PORT);
WiFiClient tcpClients[MAX_TCP_CLIENTS];

String tcpBuffers[MAX_TCP_CLIENTS];
bool tcpAuthenticated[MAX_TCP_CLIENTS];
String serialBuffer;
String apSsid;
String mdnsHostname;

bool positionSaved = true;
bool findingHome = false;
bool homeFound = false;
bool backlashCompensating = false;
long backlashFinalTarget = 0;
bool manualMoveCW = false;
bool manualMoveCCW = false;
int lastCWState = HIGH;
int lastCCWState = HIGH;
unsigned long lastCWDebounce = 0;
unsigned long lastCCWDebounce = 0;
unsigned long lastStatusBroadcast = 0;
const unsigned long debounceDelayMs = 20;

const char INDEX_HTML[] PROGMEM = R"rawliteral(
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<title>CAA Rotator</title>
<style>
:root{color-scheme:dark;--bg:#111318;--panel:#1a1f28;--panel2:#202734;--line:#384252;--text:#f2f5f8;--muted:#aeb8c6;--accent:#4db6ac;--warn:#f4b24e;--danger:#ee6b63;--ok:#7bc96f}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;letter-spacing:0}
main{max-width:720px;margin:0 auto;padding:18px 14px 28px}
header{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:14px}
h1{font-size:22px;margin:0;font-weight:680}
.status{font-size:13px;color:var(--muted)}
.grid{display:grid;grid-template-columns:1fr;gap:10px}
section{border:1px solid var(--line);background:var(--panel);border-radius:8px;padding:12px}
.readout{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.metric{background:var(--panel2);border:1px solid var(--line);border-radius:8px;padding:10px;min-height:76px}
.metric span{display:block;color:var(--muted);font-size:12px;margin-bottom:8px}
.metric strong{font-size:22px;line-height:1.2;white-space:nowrap}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.controls{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}
button,input,select{font:inherit}
button{border:1px solid var(--line);background:#263244;color:var(--text);border-radius:8px;min-height:44px;padding:0 12px;cursor:pointer}
button:active{transform:translateY(1px)}
button.primary{background:var(--accent);border-color:var(--accent);color:#071211;font-weight:700}
button.warn{background:var(--warn);border-color:var(--warn);color:#211400;font-weight:700}
button.danger{background:var(--danger);border-color:var(--danger);color:#210706;font-weight:700}
button.toggle.active{background:var(--ok);border-color:var(--ok);color:#061008;font-weight:700}
label{display:grid;gap:5px;color:var(--muted);font-size:12px;min-width:0}
input{width:100%;background:#121720;color:var(--text);border:1px solid var(--line);border-radius:8px;min-height:42px;padding:8px 10px}
.form{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.wide{grid-column:1/-1}
.dial{aspect-ratio:1;max-width:330px;margin:6px auto 12px;border:2px solid var(--line);border-radius:50%;position:relative;background:radial-gradient(circle,#202734 0,#151a22 68%,#111318 100%)}
.needle{position:absolute;left:50%;top:50%;width:3px;height:43%;background:var(--accent);transform-origin:50% 0;transform:rotate(0deg);border-radius:3px}
.hub{position:absolute;left:50%;top:50%;width:18px;height:18px;background:var(--accent);border-radius:50%;transform:translate(-50%,-50%)}
.tick{position:absolute;color:var(--muted);font-size:12px}
.t0{left:50%;top:8px;transform:translateX(-50%)}.t90{right:8px;top:50%;transform:translateY(-50%)}.t180{left:50%;bottom:8px;transform:translateX(-50%)}.t270{left:8px;top:50%;transform:translateY(-50%)}
.small{font-size:12px;color:var(--muted)}
.toast{font-size:13px;color:var(--ok);min-height:20px;transition:color .3s;display:flex;align-items:center;gap:6px}
.toast.info{color:var(--accent)}
.toast.warn{color:var(--warn)}
.spinner{display:inline-block;width:14px;height:14px;border:2px solid var(--line);border-top-color:var(--accent);border-radius:50%;animation:spin .7s linear infinite;flex-shrink:0}
@keyframes spin{to{transform:rotate(360deg)}}
@media (min-width:680px){.grid{grid-template-columns:1fr 1fr}.span{grid-column:1/-1}}
</style>
</head>
<body>
<main>
<header>
<h1 id="pageTitle">CAA Rotator</h1>
<div class="status" id="link">Connecting</div>
</header>
<div class="grid">
<section class="span">
<div class="dial" id="dial">
<div class="tick t0">0</div><div class="tick t90">90</div><div class="tick t180">180</div><div class="tick t270">270</div>
<div class="needle" id="needle"></div><div class="hub"></div>
</div>
<div class="readout">
<div class="metric"><span>Angle</span><strong id="angle">--</strong></div>
<div class="metric"><span>Steps</span><strong id="steps">--</strong></div>
</div>
</section>
<section>
<div class="form">
<label class="wide">Absolute angle<input id="absoluteAngle" type="number" min="0" max="359.99" step="0.01" value="0"></label>
<button class="primary wide" id="moveAbsolute">Move</button>
<button data-rel="-10">-10</button><button data-rel="-1">-1</button><button data-rel="-0.1">-0.1</button>
<button data-rel="0.1">+0.1</button><button data-rel="1">+1</button><button data-rel="10">+10</button>
<button class="danger" id="halt">STOP</button><button id="home">Home</button><button id="setZero">Set 0</button>
</div>
</section>
<section>
<div class="form">
<button class="toggle" id="hold">Hold</button>
<button class="toggle" id="reverse">Reverse</button>
<label>Steps/degree<input id="stepsPerDegree" type="number" min="1" step="1"></label>
<label>Max speed<input id="maxSpeed" type="number" min="1" step="1"></label>
<label>Acceleration<input id="acceleration" type="number" min="1" step="1"></label>
<label>Manual step<input id="manualStep" type="number" min="1" step="1"></label>
<label>Device Name<input id="nickname" type="text" maxlength="31" placeholder="mDNS hostname"></label>
<label>AP Password<input id="apPassword" type="password" maxlength="31" placeholder="default: 012345678"></label>
<label>STA SSID<input id="staSsid" type="text" maxlength="31"></label>
<label>STA Password<input id="staPassword" type="password" maxlength="63"></label>
<label>TCP Password<input id="tcpPassword" type="password" maxlength="15" placeholder="empty = no auth"></label>
<button class="primary wide" id="saveSettings">Save settings</button>
<div class="toast wide" id="saveMsg"></div>
<div class="small wide" id="network"></div>
</div>
</section>
</div>
</main>
<script>
const $=id=>document.getElementById(id);
let state={};
async function api(path,body){
 const opt=body===undefined?{}:{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)};
 const res=await fetch(path,opt);
 if(!res.ok)throw new Error(path);
 return res.json();
}
function setState(s){
 state=s;
 const title=s.nickname?s.nickname:'CAA Rotator';
 document.title=title;
 $('pageTitle').textContent=title;
 $('angle').textContent=(s.angle??0).toFixed(2)+'°';
 $('steps').textContent=String(s.positionSteps??0);
 $('needle').style.transform='rotate('+((s.angle??0)+180)+'deg)';
 $('link').textContent=s.isMoving?'Moving':'Ready';
 $('hold').classList.toggle('active',!!s.hold);
 $('reverse').classList.toggle('active',!!s.reversed);
 $('stepsPerDegree').value=s.stepsPerDegree??100;
 $('maxSpeed').max=(s.stepsPerDegree??100)*10;
 $('maxSpeed').value=s.maxSpeed??800;
 $('acceleration').value=s.acceleration??1000;
 $('manualStep').value=s.manualStep??50;
 if(document.activeElement!==$('nickname'))$('nickname').value=s.nickname||'';
 if(document.activeElement!==$('staSsid'))$('staSsid').value=s.staSsid||'';
 if(document.activeElement!==$('tcpPassword'))$('tcpPassword').placeholder=s.tcpLocked?'••••••••':'empty = no auth';
 if(document.activeElement!==$('apPassword'))$('apPassword').placeholder=s.apCustom?'••••••••':'default: 012345678';
 const staIp=s.staIp&&s.staIp!=='0.0.0.0'?s.staIp:'not connected';
 const mdnsHost=(s.mdns||'').replace('.local','');
 $('network').innerHTML='AP: '+(s.apIp||'192.168.4.1')+' | STA: '+staIp+'<br>mDNS: <b>'+(s.mdns||'')+'</b> | TCP: '+(s.tcpPort||4030)+'<br>ASCOM Host: <b>'+mdnsHost+'</b> or <b>'+staIp+'</b>';
}
async function refresh(){try{setState(await api('/api/status'));}catch(e){$('link').textContent='Disconnected';}}
function connectWs(){
 const ws=new WebSocket('ws://'+location.hostname+':81/');
 ws.onopen=()=>{$('link').textContent='Connected'};
 ws.onmessage=e=>{try{setState(JSON.parse(e.data));}catch(_){}};
 ws.onclose=()=>{setTimeout(connectWs,1200);};
}
$('moveAbsolute').onclick=()=>api('/api/move',{angle:Number($('absoluteAngle').value)}).then(setState);
document.querySelectorAll('[data-rel]').forEach(b=>b.onclick=()=>api('/api/move',{relativeDeg:Number(b.dataset.rel)}).then(setState));
$('halt').onclick=()=>api('/api/halt',{}).then(setState);
$('home').onclick=()=>api('/api/home',{}).then(setState);
$('setZero').onclick=()=>api('/api/set-position',{angle:0}).then(setState);
$('hold').onclick=()=>api('/api/settings',{hold:!state.hold}).then(setState);
$('reverse').onclick=()=>api('/api/settings',{reversed:!state.reversed}).then(setState);
let staWaiting=false,staWatchTimer=null,staWatchCount=0;
function showMsg(text,cls,spin){
 const m=$('saveMsg');
 m.innerHTML=(spin?'<span class="spinner"></span>':'')+text;
 m.className='toast wide'+(cls?' '+cls:'');
}
async function watchStaIp(){
 if(!staWaiting)return;
 staWatchCount++;
 try{
  const s=await api('/api/status');
  if(s.staIp&&s.staIp!=='0.0.0.0'){
   staWaiting=false;
   showMsg('WiFi connected: '+s.staIp);
   if(staWatchTimer){clearInterval(staWatchTimer);staWatchTimer=null;}
   setState(s);
   return;
  }
  if(staWatchCount>30){staWaiting=false;showMsg('WiFi timeout - check SSID/password','warn');if(staWatchTimer){clearInterval(staWatchTimer);staWatchTimer=null;}}
 }catch(e){}
}
$('saveSettings').onclick=async()=>{
 const btn=$('saveSettings');
 btn.disabled=true;btn.textContent='Saving...';
 showMsg('Saving settings...','info',true);
 const body={
  stepsPerDegree:Number($('stepsPerDegree').value),
  maxSpeed:Number($('maxSpeed').value),
  acceleration:Number($('acceleration').value),
  manualStep:Number($('manualStep').value),
  nickname:$('nickname').value.trim()
 };
 const hasSta=$('staSsid').value.trim();
 if(hasSta)body.staSsid=hasSta;
 if($('staPassword').value)body.staPassword=$('staPassword').value;
 body.tcpPassword=$('tcpPassword').value;
 if($('apPassword').value)body.apPassword=$('apPassword').value;
 try{
  const s=await api('/api/settings',body);
  setState(s);
  btn.textContent='Save settings';
  btn.disabled=false;
  if(hasSta&&(!s.staIp||s.staIp==='0.0.0.0')){
   staWaiting=true;staWatchCount=0;
   showMsg('Connecting to WiFi...','info',true);
   if(staWatchTimer)clearInterval(staWatchTimer);
   staWatchTimer=setInterval(watchStaIp,1500);
  }else if(hasSta&&s.staIp&&s.staIp!=='0.0.0.0'){
   showMsg('Settings saved. WiFi: '+s.staIp);
  }else{
   showMsg('Settings saved');
  }
 }catch(e){
  btn.textContent='Save settings';
  btn.disabled=false;
  showMsg('Save failed - retry','warn');
 }
};
connectWs();
refresh();
setInterval(refresh,5000);
</script>
</body>
</html>
)rawliteral";

void saveSettings() {
  settings.magic = SETTINGS_MAGIC;
  settings.position = stepper.currentPosition();
  EEPROM.put(0, settings);
  EEPROM.commit();
}

void loadSettings() {
  EEPROM.begin(EEPROM_SIZE);
  EEPROM.get(0, settings);
  if (settings.magic != SETTINGS_MAGIC || settings.stepsPerDegree <= 0) {
    memset(&settings, 0, sizeof(settings));
    settings.magic = SETTINGS_MAGIC;
    settings.position = 20000;
    settings.stepsPerDegree = 100;
    settings.maxSteps = 40000;
    settings.maxSpeed = 800;
    settings.acceleration = 1000;
    settings.manualMoveStepSize = 50;
    settings.findHomeStepSize = 100;
    settings.hold = false;
    settings.reversed = false;
    settings.homeOffsetSteps = 0;
    strcpy(settings.apPassword, "012345678");
    EEPROM.put(0, settings);
    EEPROM.commit();
  }
  if (settings.apPassword[0] == '\0' || !isprint(settings.apPassword[0])) {
    strcpy(settings.apPassword, "012345678");
  }
  // Clean garbage tcpPassword from old EEPROM layout
  if (settings.tcpPassword[0] != '\0' && !isprint(settings.tcpPassword[0])) {
    memset(settings.tcpPassword, 0, sizeof(settings.tcpPassword));
  }
  // Clean garbage nickname from old EEPROM layout
  if (settings.nickname[0] != '\0' && !isprint(settings.nickname[0])) {
    memset(settings.nickname, 0, sizeof(settings.nickname));
  }
  if (settings.maxSteps <= 0) {
    settings.maxSteps = (long)settings.stepsPerDegree * 400L;
  }
  long center = settings.maxSteps / 2L;
  if (settings.homeOffsetSteps < -center || settings.homeOffsetSteps > center) {
    settings.homeOffsetSteps = 0;
  }
  // Default backlash compensation: 5 degrees
  if (settings.backlashSteps <= 0 || settings.backlashSteps > settings.stepsPerDegree * 30) {
    settings.backlashSteps = settings.stepsPerDegree * 5;
  }
}

void applyMotionSettings() {
  stepper.setMaxSpeed(settings.maxSpeed);
  stepper.setAcceleration(settings.acceleration);
  stepper.setPinsInverted(settings.reversed, false, false);
}

void updateEnablePin() {
  if (stepper.distanceToGo() != 0 || settings.hold) {
    digitalWrite(ENABLE_PIN, LOW);
  } else {
    digitalWrite(ENABLE_PIN, HIGH);
  }
}

bool hallTriggered() {
  return digitalRead(HALL_PIN) == LOW;
}

long mechanicalHomeSteps() {
  return settings.maxSteps / 2L;
}

long zeroPhysicalSteps() {
  return mechanicalHomeSteps() + settings.homeOffsetSteps;
}

long physicalToLogicalSteps(long physicalSteps) {
  return physicalSteps - settings.homeOffsetSteps;
}

long logicalToPhysicalSteps(long logicalSteps) {
  return logicalSteps + settings.homeOffsetSteps;
}

void clampHomeOffset() {
  // Limit homeOffsetSteps to ±half of physical range
  // Physical range (0 to maxSteps) ensures no excessive rotation
  long maxOffset = settings.maxSteps / 2L;
  if (settings.homeOffsetSteps < -maxOffset) {
    settings.homeOffsetSteps = -maxOffset;
  }
  if (settings.homeOffsetSteps > maxOffset) {
    settings.homeOffsetSteps = maxOffset;
  }
}

void setCurrentLogicalPosition(long logicalSteps) {
  settings.homeOffsetSteps = stepper.currentPosition() - logicalSteps;
  clampHomeOffset();
}

float stepsToAngle(long physicalSteps) {
  long logicalSteps = physicalToLogicalSteps(physicalSteps);
  float angle = ((float)logicalSteps - mechanicalHomeSteps()) / settings.stepsPerDegree;
  while (angle < 0.0F) angle += 360.0F;
  while (angle >= 360.0F) angle -= 360.0F;
  return angle;
}

float normalizeAngle(float angle) {
  while (angle < 0.0F) angle += 360.0F;
  while (angle >= 360.0F) angle -= 360.0F;
  return angle;
}

bool physicalStepIsInRange(long physicalSteps) {
  return physicalSteps >= 0 && physicalSteps <= settings.maxSteps;
}

long choosePhysicalTarget(long physicalA, long physicalB) {
  bool validA = physicalStepIsInRange(physicalA);
  bool validB = physicalStepIsInRange(physicalB);
  if (validA && validB) {
    long current = stepper.currentPosition();
    return abs(current - physicalA) <= abs(current - physicalB) ? physicalA : physicalB;
  }
  if (validA) {
    return physicalA;
  }
  return physicalB;
}

long angleToPhysicalSteps(float angle) {
  float normalized = normalizeAngle(angle);
  long logicalA = lround(normalized * settings.stepsPerDegree);
  long logicalB = lround((normalized + 360.0F) * settings.stepsPerDegree);
  return choosePhysicalTarget(logicalToPhysicalSteps(logicalA), logicalToPhysicalSteps(logicalB));
}

bool moveToPhysicalSteps(long target) {
  if (!findingHome && (target < 0 || target > settings.maxSteps)) {
    return false;
  }
  
  // Cancel any pending backlash compensation if new move requested
  backlashCompensating = false;
  
  long current = stepper.currentPosition();
  
  // Backlash compensation: when moving to larger position, overshoot first
  long moveDistance = target - current;
  if (!findingHome && settings.backlashSteps > 0 && moveDistance > 0) {
    long overshoot = target + settings.backlashSteps;
    // Allow overshoot beyond maxSteps by backlashSteps amount for compensation
    long overshootLimit = settings.maxSteps + settings.backlashSteps;
    if (overshoot <= overshootLimit) {
      backlashCompensating = true;
      backlashFinalTarget = target;
      stepper.moveTo(overshoot);
      positionSaved = false;
      return true;
    }
  }
  
  stepper.moveTo(target);
  positionSaved = false;
  return true;
}

bool moveToLogicalSteps(long logicalTarget) {
  long revolutionSteps = 360L * settings.stepsPerDegree;
  long physicalA = logicalToPhysicalSteps(logicalTarget);
  long physicalB = logicalToPhysicalSteps(logicalTarget + revolutionSteps);
  long physicalC = logicalToPhysicalSteps(logicalTarget - revolutionSteps);

  long target = choosePhysicalTarget(physicalA, physicalB);
  target = choosePhysicalTarget(target, physicalC);
  return moveToPhysicalSteps(target);
}

String boolText(bool value) {
  return value ? "true" : "false";
}

String statusResponse() {
  String response = "P ";
  response += physicalToLogicalSteps(stepper.currentPosition());
  response += ";M ";
  response += boolText(stepper.distanceToGo() != 0 || findingHome);
  response += "#";
  return response;
}

String ipToString(const IPAddress &ip) {
  return String(ip[0]) + "." + String(ip[1]) + "." + String(ip[2]) + "." + String(ip[3]);
}

String statusJson() {
  String json = "{";
  json += "\"firmware\":";
  json += FIRMWARE_VERSION;
  json += ",\"positionSteps\":";
  json += physicalToLogicalSteps(stepper.currentPosition());
  json += ",\"targetSteps\":";
  json += physicalToLogicalSteps(stepper.targetPosition());
  json += ",\"mechanicalSteps\":";
  json += stepper.currentPosition();
  json += ",\"targetMechanicalSteps\":";
  json += stepper.targetPosition();
  json += ",\"homeOffsetSteps\":";
  json += settings.homeOffsetSteps;
  json += ",\"zeroPhysicalSteps\":";
  json += zeroPhysicalSteps();
  json += ",\"angle\":";
  json += String(stepsToAngle(stepper.currentPosition()), 2);
  json += ",\"targetAngle\":";
  json += String(stepsToAngle(stepper.targetPosition()), 2);
  json += ",\"isMoving\":";
  json += boolText(stepper.distanceToGo() != 0 || findingHome);
  json += ",\"home\":";
  json += boolText(hallTriggered());
  json += ",\"hold\":";
  json += boolText(settings.hold);
  json += ",\"reversed\":";
  json += boolText(settings.reversed);
  json += ",\"stepsPerDegree\":";
  json += settings.stepsPerDegree;
  json += ",\"maxSteps\":";
  json += settings.maxSteps;
  json += ",\"maxSpeed\":";
  json += settings.maxSpeed;
  json += ",\"acceleration\":";
  json += settings.acceleration;
  json += ",\"manualStep\":";
  json += settings.manualMoveStepSize;
  json += ",\"homeStep\":";
  json += settings.findHomeStepSize;
  json += ",\"apSsid\":\"";
  json += apSsid;
  json += "\",\"apIp\":\"";
  json += ipToString(WiFi.softAPIP());
  json += "\",\"staIp\":\"";
  json += ipToString(WiFi.localIP());
  json += "\",\"staSsid\":\"";
  json += settings.staSsid;
  json += "\",\"tcpPort\":";
  json += ASCOM_TCP_PORT;
  json += ",\"tcpLocked\":";
  json += (strlen(settings.tcpPassword) > 0) ? "true" : "false";
  json += ",\"apCustom\":";
  json += (strcmp(settings.apPassword, "012345678") != 0) ? "true" : "false";
  json += ",\"mdns\":\"";
  json += mdnsHostname;
  json += ".local\"";
  json += ",\"nickname\":\"";
  json += settings.nickname;
  json += "\",\"backlashSteps\":";
  json += settings.backlashSteps;
  json += "}";
  return json;
}

void broadcastStatus() {
  String json = statusJson();
  webSocket.broadcastTXT(json);
}

long commandParameter(String command) {
  if (command.length() <= 1) {
    return 0;
  }
  String param = command.substring(1);
  param.trim();
  return param.toInt();
}

String processCommand(String command, bool isAuthenticated) {
  command.trim();
  if (command.endsWith("#")) {
    command.remove(command.length() - 1);
  }
  command.trim();
  if (command.length() == 0) {
    return "ERR:empty#";
  }

  char code = command.charAt(0);

  // Auth gate: if password is set and not authenticated, only allow K/V/G/I/T/#
  bool needsAuth = strlen(settings.tcpPassword) > 0;
  if (needsAuth && !isAuthenticated) {
    if (code != 'K' && code != 'V' && code != 'G' && code != 'I' && code != 'T' && code != '#') {
      return "ERR:auth#";
    }
  }

  long value = commandParameter(command);
  switch (code) {
    case '#':
      return String(DEVICE_RESPONSE) + "#";
    case 'G':
      return statusResponse();
    case 'K': {
      String pwd = command.substring(1);
      pwd.trim();
      if (strlen(settings.tcpPassword) > 0 && pwd.equals(settings.tcpPassword)) {
        return "K ok#";
      }
      return "K fail#";
    }
    case 'P':
      setCurrentLogicalPosition(value);
      positionSaved = true;
      saveSettings();
      broadcastStatus();
      return statusResponse();
    case 'M':
      if (!moveToLogicalSteps(value)) {
        return "ERR:out_of_range#";
      }
      broadcastStatus();
      return statusResponse();
    case 'N': {
      // No-backlash move for field rotation tracking
      // Input is PHYSICAL steps (not logical), directly move without backlash
      // Plugin calculates: physicalSteps = angle * stepsPerDegree + 200 * stepsPerDegree
      long target = value;
      if (target < 0 || target > settings.maxSteps) {
        return "ERR:out_of_range#";
      }
      backlashCompensating = false;
      stepper.moveTo(target);
      positionSaved = false;
      broadcastStatus();
      return statusResponse();
    }
    case 'H':
      findingHome = true;
      homeFound = false;
      broadcastStatus();
      return "H false#";
    case 'S':
      stepper.stop();
      findingHome = false;
      backlashCompensating = false;
      broadcastStatus();
      return "S#";
    case 'R':
      settings.reversed = value != 0;
      applyMotionSettings();
      saveSettings();
      broadcastStatus();
      return String("reversed = ") + boolText(settings.reversed) + "#";
    case 'C':
      settings.hold = value != 0;
      saveSettings();
      updateEnablePin();
      broadcastStatus();
      return String("hold = ") + boolText(settings.hold) + "#";
    case 'V':
      return String("V ") + FIRMWARE_VERSION + "#";
    case 'T':
      return String("T Rotator#");
    case 'I':
      return statusJson() + "#";
    case 'D':
      if (value <= 0) {
        return "ERR:steps_per_degree#";
      }
      settings.stepsPerDegree = (int)value;
      settings.maxSteps = (long)settings.stepsPerDegree * 400L;
      clampHomeOffset();
      saveSettings();
      broadcastStatus();
      return String("D ") + settings.stepsPerDegree + "#";
    case 'A': {
      String paramStr = command.substring(1);
      paramStr.trim();
      if (paramStr.length() > 0) {
        int val = paramStr.toInt();
        if (val > 0) {
          settings.acceleration = val;
          applyMotionSettings();
          saveSettings();
          broadcastStatus();
        }
      }
      return String("acceleration = ") + settings.acceleration + "#";
    }
    case 'X': {
      String paramStr = command.substring(1);
      paramStr.trim();
      if (paramStr.length() > 0) {
        int val = paramStr.toInt();
        if (val > 0) {
          int limit = settings.stepsPerDegree * 10;
          if (val > limit) val = limit;
          settings.maxSpeed = val;
          applyMotionSettings();
          saveSettings();
          broadcastStatus();
        }
      }
      return String("maxSpeed = ") + settings.maxSpeed + "#";
    }
    default:
      return String("ERR:") + code + "#";
  }
}

void sendJson(int code, const String &json) {
  server.sendHeader("Cache-Control", "no-store");
  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.send(code, "application/json", json);
}

bool extractNumber(const String &body, const String &key, double &value) {
  int keyIndex = body.indexOf("\"" + key + "\"");
  if (keyIndex < 0) {
    return false;
  }
  int colon = body.indexOf(':', keyIndex);
  if (colon < 0) {
    return false;
  }
  int start = colon + 1;
  while (start < (int)body.length() && isspace(body.charAt(start))) {
    start++;
  }
  int end = start;
  while (end < (int)body.length()) {
    char c = body.charAt(end);
    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.') {
      end++;
    } else {
      break;
    }
  }
  if (end == start) {
    return false;
  }
  value = body.substring(start, end).toFloat();
  return true;
}

bool extractBool(const String &body, const String &key, bool &value) {
  int keyIndex = body.indexOf("\"" + key + "\"");
  if (keyIndex < 0) {
    return false;
  }
  int colon = body.indexOf(':', keyIndex);
  if (colon < 0) {
    return false;
  }
  String tail = body.substring(colon + 1);
  tail.trim();
  if (tail.startsWith("true") || tail.startsWith("1")) {
    value = true;
    return true;
  }
  if (tail.startsWith("false") || tail.startsWith("0")) {
    value = false;
    return true;
  }
  return false;
}

bool extractString(const String &body, const String &key, char *dest, size_t destSize) {
  int keyIndex = body.indexOf("\"" + key + "\"");
  if (keyIndex < 0) {
    return false;
  }
  int colon = body.indexOf(':', keyIndex);
  int firstQuote = body.indexOf('"', colon + 1);
  int secondQuote = body.indexOf('"', firstQuote + 1);
  if (colon < 0 || firstQuote < 0 || secondQuote < 0 || destSize == 0) {
    return false;
  }
  String value = body.substring(firstQuote + 1, secondQuote);
  value.toCharArray(dest, destSize);
  dest[destSize - 1] = '\0';
  return true;
}

void handleRoot() {
  server.send_P(200, "text/html", INDEX_HTML);
}

void handleStatus() {
  sendJson(200, statusJson());
}

void handleMoveApi() {
  String body = server.arg("plain");
  double value;
  bool ok = false;
  if (extractNumber(body, "steps", value)) {
    ok = moveToLogicalSteps(lround(value));
  } else if (extractNumber(body, "angle", value)) {
    ok = moveToPhysicalSteps(angleToPhysicalSteps(value));
  } else if (extractNumber(body, "relativeDeg", value)) {
    long target = physicalToLogicalSteps(stepper.currentPosition()) + lround(value * settings.stepsPerDegree);
    if (target >= settings.maxSteps) {
      target -= 360L * settings.stepsPerDegree;
    }
    if (target < 0) {
      target += 360L * settings.stepsPerDegree;
    }
    ok = moveToLogicalSteps(target);
  }

  if (!ok) {
    sendJson(400, "{\"error\":\"invalid_move\"}");
    return;
  }
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleHaltApi() {
  stepper.stop();
  findingHome = false;
  backlashCompensating = false;
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleHomeApi() {
  moveToPhysicalSteps(zeroPhysicalSteps());
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleSetPositionApi() {
  String body = server.arg("plain");
  double value;
  if (extractNumber(body, "steps", value)) {
    setCurrentLogicalPosition(lround(value));
  } else if (extractNumber(body, "angle", value)) {
    setCurrentLogicalPosition(lround((normalizeAngle(value) + 360.0F) * settings.stepsPerDegree));
  } else {
    sendJson(400, "{\"error\":\"invalid_position\"}");
    return;
  }
  positionSaved = true;
  saveSettings();
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleSettingsGetApi() {
  sendJson(200, statusJson());
}

void handleSettingsPostApi() {
  String body = server.arg("plain");
  double numberValue;
  bool boolValue;

  if (extractNumber(body, "stepsPerDegree", numberValue) && numberValue > 0) {
    settings.stepsPerDegree = (int)numberValue;
    settings.maxSteps = (long)settings.stepsPerDegree * 400L;
    clampHomeOffset();
  }
  if (extractNumber(body, "maxSpeed", numberValue) && numberValue > 0) {
    int spd = (int)numberValue;
    if (spd > settings.stepsPerDegree * 10) spd = settings.stepsPerDegree * 10;
    settings.maxSpeed = spd;
  }
  if (extractNumber(body, "acceleration", numberValue) && numberValue > 0) {
    settings.acceleration = (int)numberValue;
  }
  if (extractNumber(body, "manualStep", numberValue) && numberValue > 0) {
    settings.manualMoveStepSize = (int)numberValue;
  }
  if (extractNumber(body, "homeStep", numberValue) && numberValue > 0) {
    settings.findHomeStepSize = (int)numberValue;
  }
  if (extractBool(body, "hold", boolValue)) {
    settings.hold = boolValue;
  }
  if (extractBool(body, "reversed", boolValue)) {
    settings.reversed = boolValue;
  }
  bool staChanged = false;
  staChanged |= extractString(body, "staSsid", settings.staSsid, sizeof(settings.staSsid));
  staChanged |= extractString(body, "staPassword", settings.staPassword, sizeof(settings.staPassword));
  extractString(body, "tcpPassword", settings.tcpPassword, sizeof(settings.tcpPassword));

  char oldNickname[32];
  strcpy(oldNickname, settings.nickname);
  bool nicknameChanged = extractString(body, "nickname", settings.nickname, sizeof(settings.nickname));

  char newApPwd[32];
  newApPwd[0] = '\0';
  bool apPwdChanged = extractString(body, "apPassword", newApPwd, sizeof(newApPwd));
  if (apPwdChanged && strlen(newApPwd) >= 8) {
    strcpy(settings.apPassword, newApPwd);
    WiFi.softAP(apSsid.c_str(), settings.apPassword);
  }

  applyMotionSettings();
  saveSettings();
  updateEnablePin();

  if (staChanged && strlen(settings.staSsid) > 0) {
    WiFi.begin(settings.staSsid, settings.staPassword);
  }

  if (nicknameChanged && strcmp(oldNickname, settings.nickname) != 0) {
    updateMdnsHostname();
    MDNS.begin(mdnsHostname.c_str());
  }

  broadcastStatus();
  sendJson(200, statusJson());
}

void handleOptions() {
  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.sendHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
  server.sendHeader("Access-Control-Allow-Headers", "Content-Type");
  server.send(204, "text/plain", "");
}

void setupHttp() {
  server.on("/", HTTP_GET, handleRoot);
  server.on("/api/status", HTTP_GET, handleStatus);
  server.on("/api/move", HTTP_POST, handleMoveApi);
  server.on("/api/halt", HTTP_POST, handleHaltApi);
  server.on("/api/home", HTTP_POST, handleHomeApi);
  server.on("/api/set-position", HTTP_POST, handleSetPositionApi);
  server.on("/api/settings", HTTP_GET, handleSettingsGetApi);
  server.on("/api/settings", HTTP_POST, handleSettingsPostApi);
  server.onNotFound([]() {
    if (server.method() == HTTP_OPTIONS) {
      handleOptions();
      return;
    }
    server.send(404, "text/plain", "Not found");
  });
  server.begin();
}

String sanitizeHostname(const char *name) {
  String result = "";
  for (int i = 0; name[i] != '\0' && i < 31; i++) {
    char c = name[i];
    if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) {
      result += c;
    } else if (c >= 'A' && c <= 'Z') {
      result += (char)(c + 32);
    } else if (c == ' ' || c == '_' || c == '-') {
      if (result.length() > 0 && result.charAt(result.length() - 1) != '-') {
        result += '-';
      }
    }
  }
  while (result.length() > 0 && result.charAt(result.length() - 1) == '-') {
    result.remove(result.length() - 1);
  }
  return result;
}

void updateMdnsHostname() {
  String chipIdStr = String(ESP.getChipId(), HEX);
  String suffix = chipIdStr.substring(chipIdStr.length() > 4 ? chipIdStr.length() - 4 : 0);
  if (strlen(settings.nickname) > 0) {
    String sanitized = sanitizeHostname(settings.nickname);
    if (sanitized.length() >= 2) {
      mdnsHostname = sanitized + "-" + suffix;
    } else {
      mdnsHostname = "caa-rotator-" + chipIdStr;
    }
  } else {
    mdnsHostname = "caa-rotator-" + chipIdStr;
  }
}

void setupWifi() {
  apSsid = "CAA-Rotator-" + String(ESP.getChipId(), HEX);
  updateMdnsHostname();
  WiFi.mode(WIFI_AP_STA);
  WiFi.softAPConfig(apIp, apGateway, apSubnet);
  WiFi.softAP(apSsid.c_str(), settings.apPassword);
  if (strlen(settings.staSsid) > 0) {
    WiFi.begin(settings.staSsid, settings.staPassword);
  }
  tcpServer.begin();
  tcpServer.setNoDelay(true);
}

void handleTcpClients() {
  if (tcpServer.hasClient()) {
    WiFiClient nextClient = tcpServer.available();
    bool assigned = false;
    for (byte i = 0; i < MAX_TCP_CLIENTS; i++) {
      if (!tcpClients[i] || !tcpClients[i].connected()) {
        if (tcpClients[i]) {
          tcpClients[i].stop();
        }
        tcpClients[i] = nextClient;
        tcpClients[i].setNoDelay(true);
        tcpBuffers[i] = "";
        tcpAuthenticated[i] = false;
        assigned = true;
        break;
      }
    }
    if (!assigned) {
      nextClient.stop();
    }
  }

  for (byte i = 0; i < MAX_TCP_CLIENTS; i++) {
    if (!tcpClients[i] || !tcpClients[i].connected()) {
      continue;
    }
    while (tcpClients[i].available()) {
      char c = (char)tcpClients[i].read();
      if (c == '#') {
        String response = processCommand(tcpBuffers[i], tcpAuthenticated[i]);
        if (response.startsWith("K ok")) {
          tcpAuthenticated[i] = true;
        }
        tcpClients[i].print(response);
        tcpBuffers[i] = "";
      } else if (c != '\r' && c != '\n') {
        tcpBuffers[i] += c;
      }
    }
  }
}

void handleSerial() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '#') {
      Serial.print(processCommand(serialBuffer, true));
      serialBuffer = "";
    } else if (c != '\r' && c != '\n') {
      serialBuffer += c;
    }
  }
}

void updateManualButton(int pin, int &lastState, bool &manualFlag, unsigned long &lastDebounce, int direction) {
  int reading = digitalRead(pin);
  if (reading != lastState) {
    lastDebounce = millis();
    lastState = reading;
  }
  if ((millis() - lastDebounce) > debounceDelayMs) {
    if (reading == LOW && !manualFlag) {
      manualFlag = true;
      if (!findingHome) {
        stepper.move(direction * settings.manualMoveStepSize);
        positionSaved = false;
      }
    } else if (reading == HIGH) {
      manualFlag = false;
    }
  }
}

void handleManualButtons() {
  updateManualButton(CW_PIN, lastCWState, manualMoveCW, lastCWDebounce, 1);
  updateManualButton(CCW_PIN, lastCCWState, manualMoveCCW, lastCCWDebounce, -1);
}

void serviceHome() {
  if (!findingHome) {
    return;
  }
  if (hallTriggered()) {
    findingHome = false;
    homeFound = true;
    stepper.stop();
    stepper.setCurrentPosition(mechanicalHomeSteps());
    moveToPhysicalSteps(zeroPhysicalSteps());
    broadcastStatus();
    return;
  }
  if (stepper.distanceToGo() == 0) {
    stepper.moveTo(stepper.currentPosition() - settings.findHomeStepSize);
  }
}

void setup() {
  pinMode(STEP_PIN, OUTPUT);
  pinMode(DIR_PIN, OUTPUT);
  pinMode(ENABLE_PIN, OUTPUT);
  pinMode(HALL_PIN, INPUT_PULLUP);
  pinMode(CW_PIN, INPUT_PULLUP);
  pinMode(CCW_PIN, INPUT_PULLUP);

  Serial.begin(9600);
  Serial.setTimeout(2000);

  loadSettings();
  stepper.setCurrentPosition(settings.position);
  applyMotionSettings();
  updateEnablePin();

  setupWifi();
  if (MDNS.begin(mdnsHostname.c_str())) {
    Serial.println("mDNS: " + mdnsHostname + ".local");
  }
  setupHttp();
  webSocket.begin();
  webSocket.onEvent([](uint8_t num, WStype_t type, uint8_t *payload, size_t length) {
    if (type == WStype_CONNECTED) {
      String json = statusJson();
      webSocket.sendTXT(num, json);
    }
  });

  Serial.println();
  Serial.println(DEVICE_RESPONSE);
  Serial.print("AP SSID: ");
  Serial.println(apSsid);
  Serial.println("AP URL: http://192.168.4.1");
}

void loop() {
  MDNS.update();
  server.handleClient();
  webSocket.loop();
  handleTcpClients();
  handleSerial();
  handleManualButtons();
  serviceHome();
  updateEnablePin();
  stepper.run();

  // Handle backlash compensation: after overshoot, move to final target
  if (backlashCompensating && stepper.distanceToGo() == 0) {
    backlashCompensating = false;
    stepper.moveTo(backlashFinalTarget);
    // Don't save position yet, wait for final move to complete
  }

  if (stepper.distanceToGo() == 0 && !positionSaved && !findingHome && !backlashCompensating) {
    positionSaved = true;
    saveSettings();
    broadcastStatus();
  }

  if (millis() - lastStatusBroadcast > 500) {
    lastStatusBroadcast = millis();
    broadcastStatus();
  }
}
