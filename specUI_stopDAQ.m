function [] = specUI_stopDAQ()
pause(1);
%Stop DAQ
inputemu('key_normal','\TAB');
pause(1);
inputemu('key_normal','\ENTER');
pause(1);
%Exit Program
commandinterp = 'taskkill /F /IM "Sample2.exe" /T';
system(commandinterp);
pause(3);
end
