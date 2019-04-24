function [Data] = LoadData(filename)
%% Get rid of Zeros
fid=fopen(filename);
currentline=fgetl(fid);
i=1;
while ischar(currentline)
    Data(i,:)=string(currentline);
    currentline=fgetl(fid);
    i=i+1;
end
fclose(fid);
end

