// Arduino Uno WiFi Dev Ed Library - Version: Latest 
#include <UnoWiFiDevEd.h>
// br3ttb-Arduino-PID-Library-5adeed5 - Version: Latest 
#include <PID_v1.h>
// Arduino-Temperature-Control-Library-master - Version: Latest 
#include <DallasTemperature.h>
// OneWire - Version: Latest 
#include <OneWire.h>

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

/**
  PID Variables:
  Setpoint = the value we want our analog input to become = desired temperature
  Input = the value we get from the analog input = current temperature
  Output = PWM pulsewidth
**/
double setpoint, input, output;
double calibration = 0;

bool resting = false;
String PWM = "FALSE";
int tempindex = 0;
//Desired temps in degrees Celsius
int temperatures[] = {55, 62, 72, 78};
//resting times in minutes
long restingTimes[] = {15, 15, 10, 0};

//PID tuning parameters
double Kp = 2, Ki = 5, Kd = 1;
PID waterTempPid(&input, &output, &setpoint, Kp, Ki, Kd, DIRECT);

//The period of our PWM signal in mS
int period = 5000;
unsigned long windowStartTime, restingStartTime;

void setup() {
  //Initialize PID
  //initialize starttime
  windowStartTime = millis();
  //PID output between 0 and period
  waterTempPid.SetOutputLimits(0, period);
  //turn the PID on
  waterTempPid.SetMode(AUTOMATIC);
  //Start up sensors
  sensors.begin();
  //Start up wifi
  Wifi.begin();
  //Set output pins
  pinMode(RELAY_PIN, OUTPUT);
  pinMode(LED_PIN, OUTPUT);
  //Set timeout on wificlient lower
  Wifi.setTimeout(100);
}

//Will run continuously
void loop() {
  //Read temperature values and let PID do it's thing
  processTemperatures();
  //Serve api while wifi is available
  while(Wifi.available()){
    serveApi(Wifi);
  }
}

void processTemperatures(){
  if (tempindex >= sizeof(temperatures)) {
    //stop working, temps have been reached
    waterTempPid.SetMode(MANUAL);
  }
  else {
    //PID function
    reachTemperature(temperatures[tempindex]);
    //If temperatures are equal and we are not resting, start resting
    if (compareTemps(input ,(double)temperatures[tempindex]) && !resting) {
      //start timer
      restingStartTime = millis();
      resting = true;
    }
    //If our restingtime is elapsed, increment our temperature and start heating again
    if (restingStartTime + restingTimes[tempindex] > millis()) {
      tempindex ++;
      resting = false;
    }
  }
}
 
void reachTemperature(int temp) {
  setpoint = temp;
  //read input value
  sensors.requestTemperatures(); 
  input = sensors.getTempCByIndex(0) + calibration;
  //compute pid
  waterTempPid.Compute();

  //if the time elapsed is larger that the windowsize, we need to reset
  if (millis() - windowStartTime > period)
  { //time to shift the Relay Window
    windowStartTime += period;
  }

  //write 1 if our output is lower than the time elapsed, 0 if higher => PWM signal
  if (output < millis() - windowStartTime) {
    digitalWrite(RELAY_PIN, HIGH);
    digitalWrite(LED_PIN, HIGH);
    PWM = "TRUE";
  }
  else {
    digitalWrite(RELAY_PIN, LOW);
    digitalWrite(LED_PIN, LOW);
    PWM = "FALSE";
  }
}

bool compareTemps(double temp1, double temp2){
  double precision = 1;
  return (temp1 == temp2-precision || temp1 == temp2+precision);
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
    value = String(input,2);
  }else if(command == "desired"){
    value = String(setpoint,2);
  }else if(command == "temps"){
    value = String(input,2)+":"+String(setpoint,2);
  }else if(command == "pwm"){
    value = PWM;
  }else if(command == "all"){
    value = String(input,2)+":"+String(setpoint,2)+":"+PWM;
  }else{
      client.println("HTTP/1.1 400 Bad Request\n");
      client.println("Invalid API Call");
      client.print(EOL); //char terminator
      return;
  }
  client.println("HTTP/1.1 200 OK\n");
  client.println(value);
  client.print(EOL); //char terminator
}
