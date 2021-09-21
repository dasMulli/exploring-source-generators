using System.Text.Json;

var json = await File.ReadAllTextAsync("people.json");
var people = JsonSerializer.Deserialize<IList<Person>>(json) ?? Array.Empty<Person>();

foreach (var person in people)
{
    Console.WriteLine($"Hello {person.FullName}!");
}

record Person(string FirstName, string LastName, int? Age)
{
    public string FullName => Age switch
    {
        int age => $"{FirstName} {LastName} ({age})",
        null => FirstName + " " + LastName
    };
}