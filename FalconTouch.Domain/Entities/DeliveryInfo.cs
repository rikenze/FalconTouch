namespace FalconTouch.Domain.Entities;

public class DeliveryInfo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public int GameId { get; set; }
    public Game Game { get; set; } = default!;

    public string Street { get; set; } = default!;
    public string Number { get; set; } = default!;
    public string Neighborhood { get; set; } = default!;
    public string City { get; set; } = default!;
    public string State { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    public bool PrizeSent { get; set; }
}
