/// <summary>
/// Sample data to be passed into generators.
/// </summary>
public class SampleData
{
    public double zoneCenterDistance;
    public double borderDistance;
    public double riverDistance;
    public bool specialArea = false;
    
    public SampleData(double zoneCenterDistance, double borderDistance, double riverDistance)
    {
        this.zoneCenterDistance = zoneCenterDistance;
        this.borderDistance = borderDistance;
        this.riverDistance = riverDistance;
    }
}