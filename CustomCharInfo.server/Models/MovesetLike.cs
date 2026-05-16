namespace CustomCharInfo.server.Models
{
    public class MovesetLike
    {
        public int MovesetId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Moveset Moveset { get; set; }
        public ApplicationUser User { get; set; }
    }
}
