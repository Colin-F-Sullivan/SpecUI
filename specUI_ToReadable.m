function [out] = specUI_ToReadable(filename)
indata = csv2struct(filename);

times = TimeStamp2Seconds(indata.TimeStamp_Channel_00_Channel_01);
ch1 = VoltCSV2Array(indata.B);
ch2 = VoltCSV2Array(indata.C);

out = [times; ch1; ch2];

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


end