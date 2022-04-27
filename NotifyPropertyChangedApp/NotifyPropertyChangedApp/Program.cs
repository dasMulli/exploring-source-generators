// See https://aka.ms/new-console-template for more information

using System;
using AutoNotify;


var person = new PersonViewModel
{
    FirstName = "Martin",
    LastName = "Ullrich"
};

Console.WriteLine($"Staring out with person: {person.FirstName} {person.LastName}");

person.PropertyChanged += (obj, propertyName) => Console.WriteLine($"Person changed (Property {propertyName}): {person.FirstName} {person.LastName}");

person.LastName = "Bar";
person.FirstName = "Foo";


public interface IPersonViewModel
{
    public string FirstName { get; set; }

    public string LastName { get; set; }
}

[AutoNotify(typeof(IPersonViewModel))]
public partial class PersonViewModel : IPersonViewModel
{

}
