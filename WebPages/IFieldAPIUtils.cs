using NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.WebPages;

public interface IFieldAPIUtils
{
    string HostNameField { get; }
    string HostBasePathField { get; }
    HttpClient HttpClientField { get; }
    Client ClientField { get; }

    string HostNameCartographicProjection { get; }
    string HostBasePathCartographicProjection { get; }
    HttpClient HttpClientCartographicProjection { get; }
    Client ClientCartographicProjection { get; }

    string HostNameUnitConversion { get; }
    string HostBasePathUnitConversion { get; }
}
