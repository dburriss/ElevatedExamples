using LanguageExt;
using SharedTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static LanguageExt.Prelude;

namespace LanguageExtExamples
{
    public class Person : Record<Person>
    {
        private Person(string name, DateTime dob)
        {
            Name = name;
            DateOfBirth = dob;
        }

        public string Name { get; }
        public DateTime DateOfBirth { get; }

        public static Person New(string name, DateTime dateOfBirth) => new Person(name, dateOfBirth);
        public static Func<Person,string,Person> UpdateName => fun<Person,string,Person>((person, name) => new Person(name, person.DateOfBirth));
        public static Func<Person, DateTime, Person> UpdateDateOfBirth => fun<Person, DateTime, Person>((person, dateOfBirth) => new Person(person.Name, dateOfBirth));
    }

    public class People
    {
        private Lst<Person> people;

        public People(Lst<Person> people)
        {
            this.people = people;
        }
        public Try<IEnumerable<Person>> All()
        {
            var getPeopleCallToDb = fun<IEnumerable<Person>>(() => this.people);
            return Try(getPeopleCallToDb);//from using static LanguageExt.Prelude;
        }
        public TryOption<Person> Fetch(string name) {
            var debug = this.people.ToArray();
            Func<string, Person> getPeopleCallToDb = n => this.people.FirstOrDefault(x => x.Name == n);
            var toCall = TryOption(getPeopleCallToDb);
            return toCall.Apply(TryOption(name));// apply the argument to the function 
        }

        public Try<Unit> Add(string name, Person person)
        {
            if (!people.Any(p => p.Name == name))
            {
                people = people.Add(person);
            }            
            return Try(unit);
        }

        public Try<Unit> Update(string name, Person person)
        {
            var toUpdate = people.Find(x => x.Name == name);
            return toUpdate.Match(
                Some: p =>
                {
                    this.people = people.Remove(p).Add(person);
                    return Try(unit);
                },
                None: () => Try(unit)
                ); 
        }
    }

    public static class Filters
    {
        public static Func<IEnumerable<Person>, Func<Person, bool>, Person> First = (ps, pred) => ps.First(pred); 
        public static Func<IEnumerable<Person>, IEnumerable<Person>> OlderThan(DateTime dateTime) 
        {
            return fun<IEnumerable<Person>, IEnumerable<Person>>(ps => ps.Where(p => p.DateOfBirth > dateTime));
        }
    }
    
    public class ComplexTests
    {
        private People people;

        public ComplexTests()
        {
            Func<int, DateTime> inThePast = i => DateTime.Now - TimeSpan.FromDays(365 * i);
            var testData = new Lst<Person>(new Person[] {
                Person.New("Eve", inThePast(30)),
                Person.New("Bob", inThePast(70)),
                Person.New("Max", inThePast(10))}
            );
            this.people = new People(testData);
        }

        [Fact]
        public void Use_Map_To_Extract_A_Value()
        {
            var result =
                people.All()
                .Map(ps => Filters.First(ps, x => x.Name == "Bob"))//map from Lst to single Person
                .Map(p => p.Name)//map from Person to person name
                .Try();
            var name = ElevatedTypesUnsafeHelpers.ExtractUnsafe(result);
            Assert.Equal("Bob", name);
        }

        [Fact]
        public void Use_Map_To_Change_The_Value_Within_Elevated_Type()
        {
            var result = 
                people
                .Fetch("Bob")
                .Map(p => Person.UpdateName(p, "Bobby"))
                .Try();
            var person = ElevatedTypesUnsafeHelpers.ExtractUnsafe(result);
            Assert.Equal("Bobby", person.Name);
        }

        [Fact]
        public void Use_Bind_To_Use_Result_As_Argument_In_Another_Function_Call_That_Returns_An_Elevated_Type()
        {
            var result =
                people
                .Fetch("Bob")
                .Map(p => Person.UpdateName(p, "Bobby"))
                .ToTry()
                .Bind(p => people.Update("Bob", p))
                .Try();
            var updated = ElevatedTypesUnsafeHelpers.ExtractUnsafe(people.Fetch("Bobby").Try());
            Assert.Equal("Bobby", updated.Name);
        }

        [Fact]
        public void Use_Bind_Instead_Of_ToTry_To_Return_A_Different_Elevated_Type()
        {
            var toTry = fun<TryOption<Person>, Try<Person>>(opt => opt.Match(
                    Some: x => Try(x),
                    None: () => Try<Person>(new ArgumentNullException()),
                    Fail: ex => Try<Person>(ex)
                ));

            var updatedPerson =
                people
                .Fetch("Bob")
                .Map(p => Person.UpdateName(p, "Bobby"))// map: on an E(x) takes in the normal wrapped value x and returns a normal value y. Result is transformed value E(y).
                .Apply(toTry)// apply: on an E(x) takes in E(x) and returns whatever. Useful for changing elevated types or passing to function that accepts E(x)
                .Bind(p => people.Update("Bob", p))//bind: on an E(x) takes in the normal wrapped value x and returns a E(y). Useful when function takes in normal value but returns same elevated value.
                .Try();
            var updated = ElevatedTypesUnsafeHelpers.ExtractUnsafe(people.Fetch("Bobby").Try());
            Assert.Equal("Bobby", updated.Name);
        }

    }
}
