namespace AdminTemplate.Web.Models;

public class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? Search { get; set; }
    public int SortColumn { get; set; }
    public string SortDirection { get; set; } = "asc";
    public Dictionary<string, string> Filters { get; set; } = [];
}
