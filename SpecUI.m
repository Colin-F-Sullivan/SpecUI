%% UI
function SpecUI

%Housekeeping
clc;
close all;
warning('off','all');

%Define Figure
figure('Name','SpecUI v1.0','NumberTitle','off', ...
    'Position',[100 120 450 450]);

%% Title UI
Title = uicontrol('Style','text','Position',[20 380 250 30]);
Title.String = 'SpecUI v1.0';
Title.FontSize = 16;
Title.FontWeight = 'bold';

%% Initialize Arduino Button
%Make Button
InitArduino = uicontrol('Position',[20 340 250 30]);
InitArduino.String = 'Initialize Arduino Controller';
InitArduino.Callback = @RunInitArduino;
    %Run Simulation Function
    function RunInitArduino(~,~)
        disp('Attempting to reach out to Arduino. . .');
        %Prompt the user for the name of the port we are interested in
        % Setup the Arduino and verify that needed libraries are installed
        arduinosetup;
        pause
        disp('Successful Setup');
    end

%% Save Outputs of Single Simulation in Table
%Make save output button
TakeSpectrum = uicontrol('Position',[20 300 250 30]);
TakeSpectrum.String = 'Run Time Averaged Spectrum';
TakeSpectrum.Callback = @RunTakeSpectrum;
    %Save craters matrix into .xls file
    function RunTakeSpectrum(~,~)
        disp('Beginning to Take Spectrum. . .');
        port = inputdlg('What port is the Arduino Running on?','COM?');
        port = port{1};
        runs = inputdlg('How many time averaged spectrums would you like to take?','Runs?');
        runs = runs{1};
        specUI_RunSpec(port,runs);
        foldername = inputdlg('What is the name of the folder you want to save these files to?','File Name?');
        mkdir('Data',foldername{1}); %Make Directory
        currentdir = pwd();
        source = [currentdir '\C#\GUI Sample\bin\Release\SpecUI_DAQ\*.txt'];
        destination = [currentdir '\Data\' foldername{1}];
        movefile(source,destination);
        %Move all files from 
    end


%% Plot Solar Spectrum
PlotSpectrum = uicontrol('Position',[20 260 250 30]);
PlotSpectrum.String = 'Plot Spectrum';
PlotSpectrum.Callback = @RunPlotSpectrum;
    %Save craters matrix into .xls file
    function RunPlotSpectrum(~,~)
        filename = inputdlg('Please input the name of the folder located in SpecUI\Data that you wish to process' ...
            ,'Specify File Directory');
        disp('SpecUI: Converting Files. This will take a while. . .');
        
        %Get file names from this folder
        direc = dir(['Data\' filename{1} '\*.txt']);
        direc = {direc.name};
        % Convery from counts to Volts (Calls C#)
        for i = 1:length(direc)
            %Move to Folder where Counts2Volts will see this
            source = [pwd() '\Data\' filename{1}];
            destination = [pwd() '\C#\CountsToVolts\CountsToVolts\bin\Release\SpecUI_DAQ'];
            movefile([source '\' direc{i}],destination);
            specUI_Counts2Volts(direc{i});
            %Check if this is finished, it not, WAIT
            done = 0;
            directemp = direc{i};
            while done == 0
                %Check if the file has been completed
                if exist([destination '\Converted_' directemp(1:end-4) '.csv'],'file') ~= 0
                    done = 1;
                else
                    pause(.5);
                end
            end
            %Move it Back and move old file to converted
            movefile([destination '\Converted_' directemp(1:end-4) '.csv'],source);
            %movefile([destination '\Converted_' directemp(1:end-3) '.OPDIO'],source);
            mkdir(['Data\' filename{1} '\'],[filename{1} '_Unconverted']); %Make Directory
            movefile([destination '\' direc{i}],[source '\' filename{1} '_Unconverted']);
        end
        disp('SpecUI: Done Converting to Voltages');
    end

%% Exit Program
ExitProgram = uicontrol('Position',[20 220 250 30]);
ExitProgram.String = 'Exit Program';
ExitProgram.Callback = @RunExitProgram;
    %Save craters matrix into .xls file
    function RunExitProgram(~,~)
        disp('Exiting. . .');
        close all;
    end
end