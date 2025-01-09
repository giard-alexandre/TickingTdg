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
    public MainWindowViewModel() {
        var cache = new SourceCache<Person, int>(person => person.Id);
        cache.AddOrUpdate(GenerateFakes(3000));
        var data = cache.Connect().AutoRefresh(x => x.IsChecked);

        var filter = this.WhenValueChanged(x => x.FilterText)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(filterText => new Func<Person, bool>(person => string.IsNullOrEmpty(filterText)
                || (int.TryParse(filterText, out int parsedId) && person.Id == parsedId)
                || person.FirstName.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)
                || person.LastName.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)
                || person.Email.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)
                ));

        data
            .Filter(filter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var items, BindingOptions.NeverFireReset())
            .Subscribe();

        DataSource = new FlatTreeDataGridSource<Person>(items) {
            Columns = {
                new TextColumn<Person,int>("Id", x => x.Id),
                new TextColumn<Person,string>("FirstName", x => x.FirstName),
                new TextColumn<Person,string>("LastName", x => x.LastName),
                new TextColumn<Person,DateTime>("DoB", x => x.DateOfBirth),
                new TextColumn<Person,DateTime?>("MDateOfBirth", x => x.MDateOfBirth),
                new TextColumn<Person,TimeOnly>("WakeTime", x => x.WakeTime),
                new TextColumn<Person,TimeOnly>("MWakeTime", x => x.MWakeTime),
                new TemplateColumn<Person>("Height", "HeightCell"),
                new TextColumn<Person,double>("Raw Height", x => x.RawHeight),
                new TextColumn<Person,Name.Gender>("Gender", x => x.Gender),
                new TextColumn<Person,decimal>("Money", x => x.Money),
                new CheckBoxColumn<Person>("Checked", x => x.IsChecked),
                new TextColumn<Person,string>("Email", x => x.Email),
                new TextColumn<Person,string>("Phone", x => x.PhoneNumber),
                new TextColumn<Person,string>("Address", x => x.Address),
                new TextColumn<Person,string>("City", x => x.City),
                new TextColumn<Person,string>("State", x => x.State),
                new TextColumn<Person,string>("Badge", x => x.Badge),
                new TextColumn<Person,string>("PostalCode", x => x.PostalCode),
                new TextColumn<Person,string>("Country", x => x.Country),
                new CheckBoxColumn<Person>("Married", x => x.IsMarried),
                new TextColumn<Person,DateTime?>("Anniv.", x => x.WeddingAnniversary),
                new TextColumn<Person,double?>("Days Since", x => x.DaysSinceAnniversary),
                new TemplateColumn<Person>("Hobbies", "HobbiesCell"),
                new TemplateColumn<Person>("Languages", "LanguagesCell"),

            },
        };

        var faker = new Faker("en");

        // Tick Data on a 200 ms interval.
        Observable.Interval(TimeSpan.FromMilliseconds(200), RxApp.TaskpoolScheduler)
            .Where(_ => UpdateValues)
            .Select(_ => Enumerable.Range(1, 500).Select(_ => faker.Random.Int(0, cache.Count)).ToList())
            // .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(idList => {
                cache.Edit(updater => {
                    foreach (var id in idList) {
                        var item = updater.Lookup(id);
                        if (item.HasValue) {
                            var newItem = new Person();
                            using (newItem.SuppressChangeNotifications()) {
                                newItem.ApplyUpdate(item.Value);
                                newItem.ClaimedHeight = (double)faker.Random.Number(101, 100_000) / 100;
                                newItem.OtherHeight = (double)faker.Random.Number(101, 100_000) / 100;
                                newItem.WakeTime = faker.Date.BetweenTimeOnly(new TimeOnly(4, 30), new TimeOnly(10, 30));
                            }

                            updater.AddOrUpdate(newItem);
                        }
                    }
                });

            });
    }

    public FlatTreeDataGridSource<Person> DataSource { get; set; }

    [Reactive]
    public string? FilterText { get; set; }

    [Reactive]
    public bool UpdateValues { get; set; }

    private static List<Person> GenerateFakes(int amount) {
        //Set the randomizer seed to generate repeatable data sets.
        Randomizer.Seed = new Random(8675309);
        var faker = new Faker<Person>().RuleFor(p => p.Id, faker => faker.IndexFaker)
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


        return faker.Generate(amount);
    }
}

public class Person : ReactiveObject {
    private double _otherHeight;
    private double _claimedHeight;
    private bool _isChecked;
    public int Id { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime? MDateOfBirth => DateOfBirth;
    public TimeOnly WakeTime { get; set; }
    public TimeOnly MWakeTime => WakeTime;
    public double Height { get; set; }

    public double ClaimedHeight {
        get => _claimedHeight;
        set => this.RaiseAndSetIfChanged(ref _claimedHeight, value);
    }

    public double OtherHeight {
        get => _otherHeight;
        set => this.RaiseAndSetIfChanged(ref _otherHeight, value);
    }

    public double RawHeight => Height;
    public Name.Gender Gender { get; set; }
    public decimal Money { get; set; }
    public decimal EditMoney { get; set; }

    public bool IsChecked {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

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
