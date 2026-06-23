// Initialization routine
#define DirectionPin  4
#define PulsePin  8
#define EnablePin 5
#define CWPin 6
#define CCWPin 7
#define HEsensorPin 10
#define LEDPin 9
void setup() 
{

   pinMode(CWPin, INPUT_PULLUP); 
   pinMode(CCWPin, INPUT_PULLUP);
   
  pinMode(PulsePin, OUTPUT);
  digitalWrite(PulsePin, LOW);
  pinMode(DirectionPin, OUTPUT);
  digitalWrite(DirectionPin, LOW);
  pinMode(EnablePin, OUTPUT);
  digitalWrite(EnablePin, LOW);
  
  pinMode(HEsensorPin, INPUT);
  pinMode(LEDPin, OUTPUT);

  // Initialize serial
  Serial.begin(9600);
  Serial.setTimeout(2000);

  // Initialize stepper motor
  stepper.setMaxSpeed(800);// 5000 works good,  use 500000 for confrom test1000 geared rotator10000 for geared stepper   200 for non-geared large stepper(500 max)  also may depend on what else is on loop()
  stepper.setAcceleration(1000);//1000 for geared rotator  10000 for geared stepper    1000 for non-geared large stepper
  stepper.setCurrentPosition(readFocuserPos());
  positionSaved = true;
  inputString = "";
  Serial.print("ASCOM.Arduino.Rotator ");
  Serial.println("ver 1.0.0.8");
  reverseDir(false);
  Serial.print("Pos = ");
  Serial.println(stepper.currentPosition());
}
