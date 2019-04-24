clc;
format long;

% Pull in data
indata = csv2struct('Data\INTTEST\Converted_1555952485.csv');

times = TimeStamp2Seconds(indata.TimeStamp_Channel_00_Channel_01);
ch1 = VoltCSV2Array(indata.B);
ch2 = VoltCSV2Array(indata.C);

% Do some nice plotting
figure;
plot(times,ch1,'*','LineWidth',1);
hold on;
%plot(times,ch2,'*','LineWidth',1);
xlabel('Time [s]','FontSize',14);
ylabel('Voltage [V]','FontSize',14);
xlim([0 5]);
lgd = legend('CH 1');
lgd.FontSize = 14;
grid on;
title('DAQ Test Data with 1KHz Sine Wave','FontSize',16);


function [out] = VoltCSV2Array(struct)
    %Preallocation
    out_temp = zeros(1,length(struct));
    %Character Cell Array to Double Vector + Remove 'V'
    for i = 1:length(struct)-1
        out_temp(i) = str2double(erase(struct{i},'V'));
    end
    out = out_temp;
end
function [out] = TimeStamp2Seconds(struct)
    % INPUT TIMESTAMP FORMAT 00:00:00.0000000
    out_temp = zeros(1,length(struct));
    % Timestamp to seconds
    for i = 1:length(struct)-1
        time_temp = struct{i};
        hours_temp = 3600*str2double(time_temp(1:2));
        minutes_temp = 60*str2double(time_temp(4:5));
        seconds_temp = str2double(time_temp(7:8));
        subseconds_temp = str2double(time_temp(9:end));
        out_temp(i) = hours_temp + minutes_temp + seconds_temp + ...
            subseconds_temp;
    end
    out = out_temp;
end