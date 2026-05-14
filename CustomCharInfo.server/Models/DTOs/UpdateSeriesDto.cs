namespace CustomCharInfo.server.Models.DTOs
{
    public class UpdateSeriesDto
    {
        public string SeriesName { get; set; }
        public string SeriesIconUrl { get; set; }
        public string? Notes { get; set; }
    }
}