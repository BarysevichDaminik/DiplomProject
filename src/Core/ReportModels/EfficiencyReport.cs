using Core.GitModels;

namespace Core.ReportModels;

public class EfficiencyReport
{
    public DateTime RunDate { get; set; }

    public double TotalTestsTime { get; set; }

    public int TestsRunCount { get; set; }

    public int TotalTestsCount { get; set; }

    public List<CodeEntity> ImpactedMethods { get; set; } = [];

    public double TimeSaved { get; set; }
}