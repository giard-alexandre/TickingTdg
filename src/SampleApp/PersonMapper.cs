using Riok.Mapperly.Abstractions;

using SampleApp.ViewModels;

namespace SampleApp;

[Mapper(AllowNullPropertyAssignment = false)]
internal static partial class PersonMapper
{
    public static partial void ApplyUpdate([MappingTarget] this Person fruit, Person update);
}
