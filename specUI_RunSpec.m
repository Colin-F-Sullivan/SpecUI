function [] = specUI_RunSpec(port,runs)
a = arduino(port,'Uno');

%% Initializations
stp = 'D2';
dir = 'D3';
%MS1 = 'D4';
%MS2 = 'D5';
%EN = 'D6';

%Optical Switch Pins
start = 'A0';
stop = 'A1';
configurePin(a,start,'DigitalInput');
configurePin(a,stop,'DigitalInput');


delay = 0;

%Set Higher Serial rate
s = serial(port);
s.BaudRate = 115000;

%% Main
%If neither switch is active
runs = str2double(runs);
for i = 1:runs
    if (readVoltage(a,start) >= .3) && (readVoltage(a,stop) >= .3)
        disp('LOG: Not at start of track, resetting. . .');
        %Move back to end of track and reset
        writeDigitalPin(a,dir,1); % Put Direction Backwards
        stepCount = 0;
        while (readVoltage(a,start) >= .3)
            %While we arent at the start, move backwards
            writeDigitalPin(a,stp,1); %Step One
            stepCount = stepCount + 1;
            %disp(stepCount);
            pause(delay);
            writeDigitalPin(a,stp,0); %Set Pin Back To Low
        end
        disp('LOG: Reached Start');
        disp('LOG: Starting DAQ. . .');
        specUI_startDAQ();
        pause(1);
        disp('LOG: DAQ Started Successfully');
        % Advancing Motor at specified step size
        writeDigitalPin(a,dir,0); %Put Direction Forward
        stepCount = 0;
        while (readVoltage(a,stop) >= .3)
            %While we arent at the end, advance
            writeDigitalPin(a,stp,1); %Step Forward One
            stepCount = stepCount + 1;
            %disp(stepCount);
            pause(delay);
            writeDigitalPin(a,stp,0); %Set Pin Back To Low
        end
        disp('LOG: Reached Stop');
        disp('LOG: Stopping DAQ. . .');
        specUI_stopDAQ();
        pause(1);
        disp('LOG: DAQ Stopped');
        %Reset after reaching end
        disp('LOG: Resetting starting position. . .');
        writeDigitalPin(a,dir,1); % Put Direction Backwards
        stepCount = 0;
        while (readVoltage(a,start) >= .3)
            %While we arent at the start, move backwards
            writeDigitalPin(a,stp,1); %Step One
            stepCount = stepCount + 1;
            %disp(stepCount);
            pause(delay);
            writeDigitalPin(a,stp,0); %Set Pin Back To Low
        end
        disp('LOG: Back at Start, Run Complete');
    elseif (readVoltage(a,start) <= .3) && (readVoltage(a,stop) <= .3)
        disp('ERROR: ALL OPTICAL SWITCHES ARE CAUGHT, EXITING');
        return
    %if already at Start
    elseif readVoltage(a,start) <= .3
        disp('LOG: Already at Start');
        disp('LOG: Starting DAQ. . .');
        specUI_startDAQ();
        pause(1);
        disp('LOG: DAQ Started Successfully');
        % Advancing Motor at specified step size
        writeDigitalPin(a,dir,0); %Put Direction Forward
        stepCount = 0;
        while (readVoltage(a,stop) >= .3)
            %While we arent at the end, advance
            writeDigitalPin(a,stp,1); %Step Forward One
            stepCount = stepCount + 1;
            %disp(stepCount);
            pause(delay);
            writeDigitalPin(a,stp,0); %Set Pin Back To Low
        end
        disp('LOG: Reached Stop');
        disp('LOG: Stopping DAQ. . .');
        specUI_stopDAQ();
        pause(1);
        disp('LOG: DAQ Stopped');
        %Reset after reaching end
        disp('LOG: Resetting starting position. . .');
        writeDigitalPin(a,dir,1); % Put Direction Backwards
        stepCount = 0;
        while (readVoltage(a,start) >= .3)
            %While we arent at the start, move backwards
            writeDigitalPin(a,stp,1); %Step One
            stepCount = stepCount + 1;
            %disp(stepCount);
            pause(delay);
            writeDigitalPin(a,stp,0); %Set Pin Back To Low
        end
        disp('LOG: Back at Start, Run Complete');
    end
end
end



