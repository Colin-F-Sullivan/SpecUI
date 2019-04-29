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
figure; 
plot(Channel1Nums(1:2:end));
hold on;
title('Counts vs time')
xlabel('Time')
ylabel('Counts');
% figure
% title('channel2')
% plot(Channel2Nums)
end