using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

using Bogus;
using Bogus.DataSets;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SampleApp.ViewModels;

public class MainWindowViewModel : ReactiveObject {
    private const int ItemsCount = 500;
    private readonly Faker<Person> _faker = new Faker<Person>().RuleFor(p => p.Id, faker => faker.IndexFaker)
        .RuleFor(p => p.Badge, faker => faker.Person.UserName)
        .RuleFor(p => p.DateOfBirth, faker => faker.Date.Past(80))
        .RuleFor(p => p.WakeTime, faker => faker.Date.BetweenTimeOnly(new TimeOnly(4, 30), new TimeOnly(10, 30)))
        .RuleFor(p => p.Height, faker => faker.Random.Double())
        .RuleFor(p => p.ClaimedHeight, faker => (double)faker.Random.Number(101, 100_000) / 100)
        .RuleFor(p => p.OtherHeight, faker => (double)faker.Random.Number(101, 100_000) / 100)
        .RuleFor(p => p.Gender, faker => faker.Person.Gender)
        .RuleFor(p => p.Money, faker => faker.Finance.Amount(-1000M, 1000M, 5))
        .RuleFor(p => p.EditMoney, faker => faker.Finance.Amount(-1000M, 1000M, 5))
        .RuleFor(p => p.IsChecked, faker => faker.Random.Bool())
        .RuleFor(p => p.FirstName, f => f.Name.FirstName())
        .RuleFor(p => p.LastName, f => f.Name.LastName())
        .RuleFor(p => p.Email, (f, p) => f.Internet.Email(p.FirstName, p.LastName))
        .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber())
        .RuleFor(p => p.Address, f => f.Address.StreetAddress())
        .RuleFor(p => p.City, f => f.Address.City())
        .RuleFor(p => p.State, f => f.Address.State())
        .RuleFor(p => p.PostalCode, f => f.Address.ZipCode())
        .RuleFor(p => p.Country, f => f.Address.Country())
        .RuleFor(p => p.IsMarried, f => f.Random.Bool())
        .RuleFor(p => p.WeddingAnniversary,
            (f, p) => p.IsMarried ? f.Date.Past(10, p.DateOfBirth.AddYears(18)) : (DateTime?)null)
        .RuleFor(p => p.Hobbies,
            f => f.Make(3,
                () => f.PickRandom("Fishing", "Cooking", "Gardening", "Reading", "Traveling", "Sports", "Art",
                    "Music")))
        .RuleFor(p => p.LanguagesSpoken, f => f.Make(2, () => f.Random.Word()));

    private readonly Func<Person, ReactivePerson> _transformFactory = static p => new ReactivePerson {
        Id = p.Id,
        WakeTime = p.WakeTime,
        Address = p.Address,
        Badge = p.Badge,
        City = p.City,
        Country = p.City,
        Email = p.Email,
        Gender = p.Gender,
        Height = p.Height,
        Hobbies = p.Hobbies,
        FirstName = p.FirstName,
        Money = p.Money,
        State = p.State,
        ClaimedHeight = p.ClaimedHeight,
        EditMoney = p.EditMoney,
        IsChecked = p.IsChecked,
        IsMarried = p.IsMarried,
        LanguagesSpoken = p.LanguagesSpoken,
        LastName = p.LastName,
        OtherHeight = p.OtherHeight,
        PhoneNumber = p.PhoneNumber,
        PostalCode = p.PostalCode,
        WeddingAnniversary = p.WeddingAnniversary,
        DateOfBirth = p.DateOfBirth,
    };

    private readonly Action<ReactivePerson, Person> _updateAction = static (rp, p) => {
        rp.WakeTime = p.WakeTime;
        rp.Address = p.Address;
        rp.Badge = p.Badge;
        rp.City = p.City;
        rp.Country = p.City;
        rp.Email = p.Email;
        rp.Gender = p.Gender;
        rp.Height = p.Height;
        rp.Hobbies = p.Hobbies;
        rp.FirstName = p.FirstName;
        rp.Money = p.Money;
        rp.State = p.State;
        rp.ClaimedHeight = p.ClaimedHeight;
        rp.EditMoney = p.EditMoney;
        rp.IsChecked = p.IsChecked;
        rp.IsMarried = p.IsMarried;
        rp.LanguagesSpoken = p.LanguagesSpoken;
        rp.LastName = p.LastName;
        rp.OtherHeight = p.OtherHeight;
        rp.PhoneNumber = p.PhoneNumber;
        rp.PostalCode = p.PostalCode;
        rp.WeddingAnniversary = p.WeddingAnniversary;
        rp.DateOfBirth = p.DateOfBirth;
    };

    public MainWindowViewModel() {
        //Set the randomizer seed to generate repeatable data sets.
        Randomizer.Seed = new Random(8675309);

        var cache = new SourceCache<Person, int>(person => person.Id);
        cache.AddOrUpdate(_faker.Generate(ItemsCount));
        var data = cache.Connect();

        var filter = this.WhenValueChanged(x => x.FilterText)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(static filterText => new Func<Person, bool>(person => string.IsNullOrEmpty(filterText)
                || (int.TryParse(filterText, out int parsedId) && person.Id == parsedId)
                || person.FirstName.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)
                || person.LastName.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)
                || person.Email.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)
                ));

        data.Filter(filter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .TransformWithInlineUpdate(_transformFactory, _updateAction)

            .Sort(SortExpressionComparer<ReactivePerson>.Ascending(x => x.FirstName), resetThreshold: int.MaxValue)
            .Bind(out var items, BindingOptions.NeverFireReset())
            // .SortAndBind(out var items, SortExpressionComparer<ReactivePerson>.Ascending(x => x.FirstName),
            //     new SortAndBindOptions() { ResetThreshold = int.MaxValue, UseReplaceForUpdates = true })
            .Subscribe(x =>
                Console.WriteLine(
                    $"Adds: {x.Adds}, Refreshes: {x.Refreshes}, Removes: {x.Removes}, Updates: {x.Updates}"));

        items.ObserveCollectionChanges()
            .Subscribe(x => { Console.WriteLine($"Collection Change Reason: {x.EventArgs.Action}"); });

        DataSource = new FlatTreeDataGridSource<ReactivePerson>(items) {
            Columns = {
                new TextColumn<ReactivePerson,int>("Id", x => x.Id, new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("FirstName", x => x.FirstName, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("LastName", x => x.LastName, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,DateTime>("DoB", x => x.DateOfBirth, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,DateTime?>("MDateOfBirth", x => x.MDateOfBirth, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,TimeOnly>("WakeTime", x => x.WakeTime, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,TimeOnly>("MWakeTime", x => x.MWakeTime, width: new GridLength(100, GridUnitType.Pixel)),
                new TemplateColumn<ReactivePerson>("Height", "HeightCell", width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,double>("Raw Height", x => x.RawHeight, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,Name.Gender>("Gender", x => x.Gender, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,decimal>("Money", x => x.Money, width: new GridLength(100, GridUnitType.Pixel)),
                new CheckBoxColumn<ReactivePerson>("Checked", x => x.IsChecked, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("Email", x => x.Email, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("Phone", x => x.PhoneNumber, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("Address", x => x.Address, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("City", x => x.City, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("State", x => x.State, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("Badge", x => x.Badge, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("PostalCode", x => x.PostalCode, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,string>("Country", x => x.Country, width: new GridLength(100, GridUnitType.Pixel)),
                new CheckBoxColumn<ReactivePerson>("Married", x => x.IsMarried, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,DateTime?>("Anniv.", x => x.WeddingAnniversary, width: new GridLength(100, GridUnitType.Pixel)),
                new TextColumn<ReactivePerson,double?>("Days Since", x => x.DaysSinceAnniversary, width: new GridLength(100, GridUnitType.Pixel)),
                new TemplateColumn<ReactivePerson>("Hobbies", "HobbiesCell", width: new GridLength(100, GridUnitType.Pixel)),
                new TemplateColumn<ReactivePerson>("Languages", "LanguagesCell", width: new GridLength(100, GridUnitType.Pixel)),
            },
        };

        // Tick Data on a 200 ms interval.
        Observable.Interval(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
            .Where(_ => UpdateValues)
            .Subscribe(idList => {
                var newItems = _faker.Generate(ItemsCount/5).Select(x => {
                    x.Id = Random.Shared.Next(0, ItemsCount-1);
                    return x;
                }).ToList();
                cache.AddOrUpdate(newItems);
            });
    }

    public FlatTreeDataGridSource<ReactivePerson> DataSource { get; set; }

    [Reactive]
    public string? FilterText { get; set; }

    [Reactive]
    public bool UpdateValues { get; set; }
}

public class ReactivePerson : ReactiveObject {
    public int Id { get; set; }
    [Reactive] public DateTime DateOfBirth { get; set; }
    public DateTime? MDateOfBirth => DateOfBirth;
    [Reactive] public TimeOnly WakeTime { get; set; }
    public TimeOnly MWakeTime => WakeTime;
    [Reactive] public double Height { get; set; }

    [Reactive] public double ClaimedHeight { get; set; }

    [Reactive] public double OtherHeight { get; set; }

    public double RawHeight => Height;
    [Reactive] public Name.Gender Gender { get; set; }
    [Reactive] public decimal Money { get; set; }
    [Reactive] public decimal EditMoney { get; set; }

    [Reactive] public bool IsChecked { get; set; }

    [Reactive] public string FirstName { get; set; }
    [Reactive] public string LastName { get; set; }
    [Reactive] public string Email { get; set; }
    [Reactive] public string PhoneNumber { get; set; }
    [Reactive] public string Address { get; set; }
    [Reactive] public string City { get; set; }
    [Reactive] public string State { get; set; }
    [Reactive] public string PostalCode { get; set; }
    [Reactive] public string Country { get; set; }
    [Reactive] public bool IsMarried { get; set; }
    [Reactive] public DateTime? WeddingAnniversary { get; set; }
    public double? DaysSinceAnniversary => (DateTime.Now - WeddingAnniversary)?.TotalDays;
    public List<string> Hobbies { get; set; } = [];
    public List<string> LanguagesSpoken { get; set; } = [];
    [Reactive] public string Badge { get; set; }
}

public class Person {
    public int Id { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime? MDateOfBirth => DateOfBirth;
    public TimeOnly WakeTime { get; set; }
    public TimeOnly MWakeTime => WakeTime;
    public double Height { get; set; }
    public double ClaimedHeight { get; set; }
    public double OtherHeight { get; set; }
    public double RawHeight => Height;
    public Name.Gender Gender { get; set; }
    public decimal Money { get; set; }
    public decimal EditMoney { get; set; }
    public bool IsChecked { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
    public bool IsMarried { get; set; }
    public DateTime? WeddingAnniversary { get; set; }
    public double? DaysSinceAnniversary => (DateTime.Now - WeddingAnniversary)?.TotalDays;
    public List<string> Hobbies { get; set; } = [];
    public List<string> LanguagesSpoken { get; set; } = [];
    public string Badge { get; set; }
}
