function [a] = SpecUI_InitArduino(port)
% Setup the Arduino and verify that needed libraries are installed
arduinosetup;
%Reach out to arduino
a = arduino(port,'Uno');
end

