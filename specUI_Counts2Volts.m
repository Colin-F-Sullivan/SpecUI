function [] = specUI_Counts2Volts(filename)
commandinterp = ['cd ',pwd(), .... 
 '\C#\CountsToVolts\CountsToVolts\bin\Release && start CountsToVolts.exe ', ...
 filename];
 system(commandinterp);
end

