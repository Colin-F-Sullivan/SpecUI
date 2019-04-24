function [] = specUI_startDAQ()
commandinterp = ['cd ',pwd(), .... 
 '\C#\GUI Sample\bin\Release && start Sample2.exe'];
system(commandinterp);
pause(3);   
inputemu('key_normal','\TAB');
pause(1);
inputemu('key_normal','\ENTER');
pause(2 );
end

