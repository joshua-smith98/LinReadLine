using LinReadLine;

string result = "";

while(result != "exit")
{
Console.Write(">> ");
result = Lin.ReadLine();
Console.WriteLine(result);
Console.WriteLine();
}