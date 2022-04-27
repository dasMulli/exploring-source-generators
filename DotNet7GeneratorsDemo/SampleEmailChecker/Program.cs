// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

var sampleInputs = new[]
{
    "1.2.2022",
    "2/1/2022",
    "Erster Februar 2022"
};

foreach (var input in sampleInputs)
{
    Console.WriteLine($"Input '{input} matches: " + Validations.DateRegex().IsMatch(input));
}

partial class Validations
{
    private const string dateRegex = """^(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})$|^(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))\4(?:(?:1[6-9]|[2-9]\d)?\d{2})$""";

    [RegexGenerator(dateRegex, RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    public static partial Regex DateRegex();
}