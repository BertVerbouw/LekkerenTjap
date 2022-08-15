// Arduino Uno WiFi Dev Ed Library - Version: Latest 
#include <UnoWiFiDevEd.h>
// Arduino-Temperature-Control-Library-master - Version: Latest 
#include <DallasTemperature.h>
// OneWire - Version: Latest 
#include <OneWire.h>
#include <EEPROM.h>

//LED pin
#define LED_PIN 3
//Relay pin
#define RELAY_PIN 6
// Data wire is plugged into pin 2 on the Arduino
#define ONE_WIRE_BUS A0
// Setup a oneWire instance to communicate with any OneWire devices (not just Maxim/Dallas temperature ICs)
OneWire oneWire(ONE_WIRE_BUS);
// Pass our oneWire reference to Dallas Temperature. 
DallasTemperature sensors(&oneWire);

double currentTemp, requestedTemp = 15;
double calibration = 0;
int cooling = 0;
long waittimebetweenstates = 300000; //5 minutes
long statechangedtime;

void setup() {  
  Serial.begin(9600);
  Serial.println("Initializing");
  //Set starttime
  statechangedtime = 0;
  //Init sensors
  sensors.begin();
  Serial.println("Sensors ready"); 
  //Start up wifi
  Wifi.begin();
  Serial.println("Wifi ready"); 
  //Set output pins
  pinMode(RELAY_PIN, OUTPUT);
  pinMode(LED_PIN, OUTPUT);
  Serial.println("Pinouts set"); 
  //Read requested temp after restart  
  EEPROM.get(0, requestedTemp);     
  Serial.println("Loaded requestedTemp: "+String(requestedTemp,2)); 
  //Set timeout on wificlient lower
  Wifi.setTimeout(100);
  Serial.println("Initialization complete");
}

//Will run continuously
void loop() {
  //Read temperature values and let PID do it's thing
  processTemperatures();
  controlCooling();
  //Serve api while wifi is available
  while(Wifi.available()){
    serveApi(Wifi);
  }
}

void controlCooling(){
  //only allow statechange after a period of time has passed
  if(statechangedtime == 0 || statechangedtime + waittimebetweenstates < millis()){
    Serial.println("Setting cooling: " + String(cooling));  
    statechangedtime = millis();  
    digitalWrite(RELAY_PIN, cooling);
  }
}

void processTemperatures(){
  sensors.requestTemperatures(); 
  currentTemp = sensors.getTempCByIndex(0) + calibration;
  if(currentTemp > requestedTemp){
    cooling = 1;  
  }
}
 
void serveApi(WifiData client){
  // read the command
  String command = client.readStringUntil('/');
  // is "temp" command?
  if (command == "digital"){
    tempCommand(client);
  }
}

void tempCommand(WifiData client){
  String value = "";
  String command = client.readStringUntil('/');
  if(command == "current"){
    //value = "{\"CurrentTemp\":\""+String(currentTemp,2)+"\"}";
    value = String(currentTemp,2)+";"+String(requestedTemp,2)+";"+String(cooling);
    Serial.println(value);
  }
  else if(command == "setrequestedtemp"){
    requestedTemp = client.readStringUntil('/').toDouble();
    statechangedtime = 0;
    EEPROM.put(0, requestedTemp);      
    value = String(requestedTemp, 2);
    Serial.println("New temperature requested: "+String(requestedTemp,2)); 
  }
  else{
    sendLine(client, "HTTP/1.1 400 Bad Request\n\nInvalid API Call");
    client.print(EOL); //char terminator
    return;
  }
  sendLine(client, "HTTP/1.1 200 OK\nContent-Type: application/json\n\n"+value);
  client.print(EOL); //char terminator
}

void sendLine(WifiData client, String line){
  for(int i = 0; i<=line.length();i++){
    client.print(line[i]);  
  }
}
