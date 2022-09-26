using System.Text.RegularExpressions;

Console.WriteLine("Hello, World!");

// var ipv4Regex = new Regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
var ipv4Regex = ValidationRegularExpressions.IPv4Regex();

var examples = new string[]
{
    "1.2.3.4",
    "999.1.2.3",
    "255.255.255.0"
};

foreach (var example in examples)
{
    Console.WriteLine(
        ipv4Regex.IsMatch(example)
            ? $"{example} is a valid IPv4 address"
            : $"{example} is not a valid IPv4 address"
    );
}

static partial class ValidationRegularExpressions
{

    [GeneratedRegex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$")]
    public static partial Regex IPv4Regex();
}