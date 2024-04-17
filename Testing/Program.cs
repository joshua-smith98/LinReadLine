using LinuxConsoleReadLineFix;

Console.Write(">> ");
var result = LinuxTerminalFix.ReadLine();
Console.WriteLine();
Console.WriteLine(result);
Console.ReadKey(true);