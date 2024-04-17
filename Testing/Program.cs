using LinuxConsoleReadLineFix;

while(true)
{
Console.Write(">> ");
var result = LinuxTerminalFix.ReadLine();
Console.WriteLine(result);
Console.WriteLine();
}