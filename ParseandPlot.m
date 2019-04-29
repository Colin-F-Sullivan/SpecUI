function [timeStamps, Channel1Nums, Channel2Nums] = ParseandPlot(filename)
%Get the data
Data=LoadData(filename);

%% Seperate the two channels
j=1;
for i=2:3:length(Data) %Cycle through the data sperating the channels, stepping by 3...
    %ignoring first entry which is 1
    timeStamps(j)=Data(i); % Index holds timestamp
    Channel1Str(j)=Data(i+1); %Index plus 1 is channel 1
    Channel2Str(j)=Data(i+2); %Index plus 2 is channel 2
    j=j+1;
end
%% Convert Channel output to nums
for i=1:length(Channel1Str)
Channel1Nums(i)=str2num(Channel1Str(i));
Channel2Nums(i)=str2num(Channel2Str(i));
end
for i = 1:length(Channel1Nums)
    if Channel1Nums(i) < 0
        Channel1Nums(i) = Channel1Nums(i) + 2^16;
    end
end

for i = 1:length(Channel2Nums)
    if Channel2Nums(i) < 0
        Channel2Nums(i) = Channel2Nums(i) + 2^16;
    end
end

%Time Vector
time1 = linspace(0,(1/250)*length(Channel1Nums),length(Channel1Nums));
time2 = linspace(0,(1/250)*length(Channel1Nums),length(conv(Channel1Nums(1:2:end),ones(100,1)/100,'valid')));

figure;
subplot(2,2,1);
plot(time1(1:2:end),Channel1Nums(1:2:end));
hold on;
grid on;
title('Counts vs time')
xlabel('Time [s]')
ylabel('Counts');

subplot(2,2,2);
plot(time2,conv(Channel1Nums(1:2:end),ones(100,1)/100,'valid'));
hold on;
grid on;
title('Convoluted Counts v. Time');
xlabel('Time [s]');
ylabel('Counts');



% Counts to Volts Conversion
fid = fopen(filename);
rangeid = str2num(fgetl(fid));
fclose('all');

Volts1 = Channel1Nums./65535;
Volts1 = Volts1 * 10;

% if ((rangeid & 1) ~= 0)
%     Volts1 = Volts1 * 2 - 1;
% end
% if ((rangeid & 2) == 0)
%     Volts1 = Volts1 * 2;
% end
% if ((rangeid & 4) == 0)
%     Volts1 = Volts1 * 5;
% end

subplot(2,2,3);
plot(time1(1:2:end),Volts1(1:2:end));
hold on;
grid on;
title('Volts vs. time')
xlabel('Time [s]')
ylabel('Volts [V]');

subplot(2,2,4);
plot(time2,conv(Volts1(1:2:end),ones(100,1)/100,'valid'));
hold on;
title('Convoluted Volts v. Time');
ylabel('Volts [V]');
xlabel('Time [s]');


