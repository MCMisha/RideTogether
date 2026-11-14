using RideTogether.Models;

namespace RideTogether.Entities;

public partial class Ride
{
    public int Id { get; set; }
    public string Departure { get; set; }
    public string Destination { get; set; }
    public RideInfo Info { get; set; }
}
