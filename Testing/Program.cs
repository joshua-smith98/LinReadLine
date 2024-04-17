using LinuxConsoleReadLineFix;

string result = "";

while(result != "exit")
{
Console.Write(">> ");
result = LinuxTerminalFix.ReadLine();
Console.WriteLine(result);
Console.WriteLine();
}