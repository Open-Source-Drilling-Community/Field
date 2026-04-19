using FieldModelShared = NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.WebPages;

public interface IFieldAPIUtils
{
    string HostNameField { get; }
    string HostBasePathField { get; }
    HttpClient HttpClientField { get; }
    FieldModelShared.Client ClientField { get; }

    string HostNameTrajectory { get; }
    string HostBasePathTrajectory { get; }
    HttpClient HttpClientTrajectory { get; }
    FieldModelShared.Client ClientTrajectory { get; }

    string HostNameCartographicProjection { get; }
    string HostBasePathCartographicProjection { get; }
    HttpClient HttpClientCartographicProjection { get; }
    FieldModelShared.Client ClientCartographicProjection { get; }

    string HostNameUnitConversion { get; }
    string HostBasePathUnitConversion { get; }
}
