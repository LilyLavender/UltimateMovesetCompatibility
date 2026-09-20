namespace CustomCharInfo.server.Models.DTOs
{
    // Body of PUT /api/movesets/{id}/admin-note. An empty or whitespace note clears it.
    public class MovesetAdminNoteDto
    {
        public string? Note { get; set; }
    }
}
