// Interrupt serial event
void reverseDir(boolean rev)
{
    stepper.setPinsInverted(rev, false, false);  //for FSQ85(true, false, false) ---  for TSA 120should be (false, false, false)  (dir, step, enable) 
  //not reversed is std setup for tsa-120
 reversed = rev;
  Serial.print("reversed = ");
  Serial.print(rev); //*** was println
  Serial.println("#");
}
void Hold(boolean conthold)
{
   
 hold = conthold;
  Serial.print("hold = ");
  Serial.print(hold); //*** was println
  Serial.println("#");
}


void serialEvent() {
 
  while (Serial.available() > 0) {
    char inChar = (char)Serial.read(); 
    if (inChar == '#'){
      serialCommand(inputString); 
      inputString = "";     
    } 
    else {
      inputString += inChar;
    }

  } 

}

void serialCommand(String command) {
  String param = command.substring(2); 

  switch(command.charAt(0)) {
  case '#':
    Serial.print(DEVICE_RESPONSE);

    break;

   case 'G':    // Return current position  (was 'P' on orig Jolo)
    printCurrentPosition();
    break; 
  case 'P':  {  // Save current position
 // the driver needs to multiply by 100 to avoid dealing with decimal points in the arduino code


//  long degreesTimes100 = stringToLong(param);
//  if (degreesTimes100 > 36000){
//    degreesTimes100 = degreesTimes100 - 36000;
//  }
//  if (degreesTimes100 < 0){
//    degreesTimes100 = degreesTimes100 + 36000;
//  }
// 
//    saveCurrentPos(degreesTimes100);
  saveCurrentPos(stringToLong(param));


    break;
  }
  case 'H':    // find home
   // halt(); 
 findHome();  
    break;
  case 'M':  {  // Move focuser to new position
//  long degreesTimes100 = stringToLong(param);  // driver multiplies by 100
//  if (degreesTimes100 > 54000){
//    degreesTimes100 = degreesTimes100 - 54000;
//  }
//  if (degreesTimes100 < 0){
//    degreesTimes100 = degreesTimes100 + 54000;
//  }

   // moveStepper(degreesTimes100, false); 
      moveStepper(stringToLong(param), false); 
    break;
  }
  case 'S':  //Stop movement  (Halt)
     //saveStepperSpeed(stringToNumber(param));
     halt();
     break;
  case 'R':
      
      int value;
     value = (stringToNumber(param));
      if (value == 0)
      reverseDir(false);
      else
      reverseDir(true);
    break;
    
case 'C':// continuos hold
      
      int value2;
     value2 = (stringToNumber(param));
      if (value2 == 0)
      Hold(false);
      else
      Hold(true);
    break;
  case 'V':
//    maxFocuserPos = stringToLong(param);

 Serial.print("V ");
  Serial.print(ver); //*** was println
  Serial.println("#");

    break;
  default:
    Serial.print("ERR:");      
    Serial.println(byte(command.charAt(1)), DEC); 
  
  }
}
 

void printCurrentPosition() {
  Serial.print("P ");

  Serial.print(stepper.currentPosition()); // was println
  //**** try adding is moving
  Serial.print(";");//delimiter
  Serial.print ("M ");
   if(stepper.distanceToGo() == 0) 
    Serial.print("false");//false
  else
    Serial.print("true");//true
  
  Serial.println("#");//  add stop bit
}


void moveStepper(long newPos, boolean manualMove) {

  if (findingHome == true){
      stepper.moveTo(newPos);
      return;
  }

      
  if(newPos != stepper.currentPosition()) {
    if(newPos < 0 || newPos > maxSteps) {
   Serial.println("out of range");
   return;
    }
    
    else
    {
       stepper.moveTo(newPos);
       positionSaved = false;
       firstPrint = false;
    }
  }
}


void halt() {
  stepper.stop();
  Serial.print("S");
  Serial.println("#");
}

void saveCurrentPos(long newPos) {
  stepper.setCurrentPosition(newPos);
  positionSaved = true;
  saveFocuserPos(newPos);
  printCurrentPosition();
}

void findHome(){
  HEState = digitalRead(HEsensorPin);
 // Serial.println("Finding Home");  // leave for testing
  homeFound = false;
  findingHome = true;
 if (HEState == HIGH){ // sensor goes low by magnet
    moveStepper(stepper.currentPosition()-findHomeStepSize, false);  // ***  adjust speed by changing the -x number 100 seems to work well
   
       findingHome = true; 
   
    }
   if (HEState == LOW){
     if(digitalRead(HEsensorPin)== LOW){ // double check sensor state
     homeFound = true;
     findingHome = false;
      saveCurrentPos(maxSteps/2);  //allow for 2 complete revolustions.  centered at 360 degrees
     Serial.print("H ");
     Serial.print("true");//true  
    Serial.println("#");//  add stop bit
  Serial.println(HEState);
    }
   }
}

  
//void saveStepperSpeed(word stepperSpeed) {
//  writeWord(STEPPER_SPEED_ADD, stepperSpeed);
//  stepper.setMaxSpeed(readWord(STEPPER_SPEED_ADD));
//  Serial.print("S");
//}



