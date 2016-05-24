// This #include statement was automatically added by the Particle IDE.
#define DEBUG
#include "SparkFun_APDS9960/SparkFun_APDS9960.h"
// Interrupt Pin
#define APDS9960_INT    D3
char myIpAddress[24];

SparkFun_APDS9960 apds = SparkFun_APDS9960();
int isr_flag = 0;
TCPClient gestureClient;
TCPClient checkClient;
TCPServer gestureServer = TCPServer(3443);

int connectToMyServer(String myNothing) {

    gestureServer.begin();
    return 1;

}



int stopMyServer(String myNothing) {

   // gestureServer
    return 1;

}

void setup(){
    // Set interrupt pin as input
    pinMode(APDS9960_INT, INPUT);
    
    // Initialize Serial port
    Serial.begin(9600);
    Particle.variable("ipAddress", myIpAddress, STRING);
    IPAddress myIp = WiFi.localIP();
    sprintf(myIpAddress, "%d.%d.%d.%d", myIp[0], myIp[1], myIp[2], myIp[3]);
    Serial.println(myIpAddress);
    gestureServer.begin(); 
    delay(3000);

    // Initialize interrupt service routine
    attachInterrupt(APDS9960_INT, interruptRoutine, FALLING);
    
    if(apds.init()){
        Serial.println(F("Gesture Sensor initialized."));
    } else {
        Serial.println(F("Gesture Sensor failed."));
    }
    
    if(apds.enableGestureSensor(true)){
        Serial.println(F("Gesture sensor is now running"));
    } 
}

void loop() {
    if(isr_flag == 1){
        detachInterrupt(APDS9960_INT);
        
        checkClient = gestureServer.available();
        if (checkClient.connected()) {
            gestureClient = checkClient; 
        }
        
        handleGesture();
        
        isr_flag = 0;
        
        attachInterrupt(APDS9960_INT, interruptRoutine, FALLING);
    }
}

void interruptRoutine(){
    isr_flag = 1;
}

void handleGesture(){
    if(apds.isGestureAvailable()){
        char szEvent[8];
        
        szEvent[0] = 0;
        
        switch ( apds.readGesture() ) {
            case DIR_UP:            strcpy(szEvent, "UP");      break;
            case DIR_DOWN:          strcpy(szEvent, "DOWN");    break;
            case DIR_LEFT:          strcpy(szEvent, "LEFT");    break;
            case DIR_RIGHT:         strcpy(szEvent, "RIGHT");   break;
            case DIR_NEAR:          strcpy(szEvent, "NEAR");    break;
            case DIR_FAR:           strcpy(szEvent, "FAR");     break;
        }
        
        if(strlen(szEvent) > 0){
            Serial.println(szEvent);
            
            publishEvent(szEvent);
        }
    }
}

void publishEvent(char* szEvent){
    Particle.publish("GESTURE-EVENT", szEvent, 60, PRIVATE);
    if (gestureClient.connected()) {
         gestureClient.println(szEvent);
    }
}