using System.Text.Json;
using System.Text.Json.Serialization;

var json = await File.ReadAllTextAsync("people.json");
var people = JsonSerializer.Deserialize<IList<Person>>(json) ?? Array.Empty<Person>();

foreach (var person in people)
{
    Console.WriteLine($"Hello {person.FullName}!");
}

var serializedContent = JsonSerializer.Serialize(people);
Console.WriteLine($"Serialized:\n{serializedContent}!");

public class Person
{
    public Person(string firstName, string lastName, int? age)
    {
        FirstName = firstName;
        LastName = lastName;
        Age = age;
    }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public int? Age { get; set; }

    public string FullName => Age switch
    {
        int age => $"{FirstName} {LastName} ({age})",
        null => FirstName + " " + LastName
    };
}

[JsonSerializable(typeof(IList<Person>))]
public partial class PersonJsonContext : JsonSerializerContext
{
}