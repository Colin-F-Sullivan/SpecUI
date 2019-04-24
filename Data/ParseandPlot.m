function [] = ParseandPlot(filename)
%Get the data
Data=LoadData(filename);

%% Seperate the two channels
j=1;
for i=2:3:length(Data) %Cycle through the data sperating the channels, stepping by 3...
    %ignoring first entry which is 1
    timeStamps(j)=Data(i); % Index holds timestamp
    Channel1(j)=Data(i+1); %Index plus 1 is channel 1
    Channel2(j)=Data(i+2); %Index plus 2 is channel 2
    j=j+1;
end
%% Convert Channel output to nums
for i=1:length(Channel1)
Channel1Nums(i)=str2num(Channel1(i));
end

figure 
title('Counts vs time')
xlabel('Time')
ylabel('Counts')
plot(Channel1Nums)

end
