#include <AccelStepper.h>

#define DirectionPin  4
#define PulsePin  8
#define EnablePin 5
#define CWPin 6
#define CCWPin 7
#define HEsensorPin 10
#define LEDPin 9

#include <EEPROM.h>

//#include <Bounce.h>
#define DEVICE_RESPONSE "scopfocus Rotator ver 1008"
int ver = 1008;
// EEPROM addresses
#define FOCUSER_POS_START 0
#define STEPPER_SPEED_ADD 3      

AccelStepper stepper(1, PulsePin, DirectionPin);//(mode, step, direction)(1, 11, 7 for old polar ardiuno) 1,13,4 for scope arduino

// Global vars
boolean hold = false;
boolean reversed;
boolean positionSaved;               // Flag indicates if stepper position was saved as new focuser position
boolean firstPrint = false;
boolean homeFound = true;
String inputString;                  // Serial input command string (terminated with \n)
boolean findingHome = false;



// *************  this value must be changed for each specific setup ***************************************
long maxSteps = 72000;  // **** steps needed for 2 complete revolutions.  prevent cord wrap.  
//  Must also enter this value/720 = steps/degree for first time ASCOM driver setup, click "properties 
// ******************************************************************************************************

int manualMoveStepSize = 50;  //higher number for faster manual moves
int findHomeStepSize = 100; //adjusts finding home speed

int buttonStateCW = HIGH;
int buttonStateCCW = HIGH;
boolean manualMoveCW = false;
boolean manualMoveCCW = false;

  int HEState = 0;
  long lastDebounceTime = 0;
//  int counter = 0;
  long debounceDelay = 20;
void loop() 
{

HEState = digitalRead(HEsensorPin);  //goes low by magnet
 
  if (HEState == LOW) {     
    // turn LED on:    
    digitalWrite(LEDPin, HIGH);  
  } 
  else {
    // turn LED off:
    digitalWrite(LEDPin, LOW); 
  }
  
 if (findingHome == true){
   findHome();

 } 
  
  
  
 // Check for CCW manual move 
 buttonStateCCW = digitalRead(CCWPin);
 if (millis() - lastDebounceTime > debounceDelay){ 
 
if (buttonStateCCW == LOW && manualMoveCCW == false){
 
    manualMoveCCW = true;
   //  Serial.println("CCW Debounce ON");
     lastDebounceTime = millis();
  }
  if (buttonStateCCW == HIGH && manualMoveCCW == true){
     

    manualMoveCCW = false;

  lastDebounceTime = millis();
  }
} 

 
 if (manualMoveCCW == true){
  
 //move CW 

stepper.move(-manualMoveStepSize); // change number here to change manual move speed

//manualMoveCW = true;
//Serial.println("CW move");
positionSaved = false;

}

   
  // Check for CCW manual move 
 buttonStateCW = digitalRead(CWPin);
 if (millis() - lastDebounceTime > debounceDelay){ 
 
if (buttonStateCW == LOW && manualMoveCW == false){

    manualMoveCW = true;
 //    Serial.println("CW Debounce ON");
     lastDebounceTime = millis();
  }
  if (buttonStateCW == HIGH && manualMoveCW == true){
    
   manualMoveCW = false;

  lastDebounceTime = millis();
  }
} 

 
 if (manualMoveCW == true){
  
 //move CW 

stepper.move(manualMoveStepSize); // change number here to change manual move speed

//manualMoveCW = true;
//Serial.println("CW move");
positionSaved = false;

}

//if (!manualMoveCW && ! manualMoveCCW){

  // Stepper loop
if (stepper.distanceToGo() == 0){
  if (hold == false){
  digitalWrite(EnablePin, HIGH);
 
}}
  else{
  digitalWrite(EnablePin, LOW);
  }
  if (stepper.distanceToGo() == 0 && !positionSaved){
    saveFocuserPos(stepper.currentPosition());
    positionSaved = true;
  }
   if(stepper.distanceToGo() != 0 && !firstPrint) { 
    firstPrint = true;
     Serial.print("P ");
  Serial.print(stepper.targetPosition());//currentPosition() no accurate until move is done  **** was println, changes w/ adding # below
  Serial.print (";");
  Serial.print("M ");
  Serial.print("true");//true
  Serial.println("#");
//}
}
stepper.run();
}
