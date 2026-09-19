public class ReturnSeriesDto
{
    public int SeriesId { get; set; }
    public string SeriesName { get; set; }
    public string SeriesIconUrl { get; set; }
    public int MovesetCount { get; set; }

    // True when the requester is credited on or edits a moveset in this series, so the frontend can offer the edit flow.
    public bool UserOwnsMoveset { get; set; }
}