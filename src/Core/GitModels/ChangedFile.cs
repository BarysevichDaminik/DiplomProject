namespace Core.GitModels;

public struct ChangedFile
{
    public string OldPath { get; set; }

    public string NewPath { get; set; }

    public ChangeTypes ChangeType { get; set; }

    public List<LineRange> ChangedLineRanges { get; set; }
}