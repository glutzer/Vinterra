public class GenerationData
{
    public TectonicPlate plate;

    /// <summary>
    /// Actual Y position generated.
    /// </summary>
    public ushort[,] heightLevels;

    /// <summary>
    /// Data to be used for erosion.
    /// </summary>
    public float[,] erosionMap;

    /// <summary>
    /// Continentalness for continent generation.
    /// </summary>
    public double[,] continentalness;

    /// <summary>
    /// Distance from the edge of a river. 0 if inside.
    /// </summary>
    public double[,] riverDistance;

    /// <summary>
    /// How slanted the terrain is (in block measure).
    /// </summary>
    public double[,] heightGradient;

    /// <summary>
    /// Biome for each block.
    /// </summary>
    public ushort[,] biomeIds;

    /// <summary>
    /// Is the block a beach?
    /// </summary>
    public bool[,] isBeach;
    public bool[,] specialArea;

    //Vectors for river flow that need to be set when the real chunk is starting to generate
    public float[] flowVectorsX;
    public float[] flowVectorsZ;

    public GenerationData(int chunkSize)
    {
        heightLevels = new ushort[chunkSize, chunkSize];
        erosionMap = new float[chunkSize, chunkSize];

        continentalness = new double[chunkSize, chunkSize];
        riverDistance = new double[chunkSize, chunkSize];

        heightGradient = new double[chunkSize, chunkSize];

        biomeIds = new ushort[chunkSize, chunkSize];

        isBeach = new bool[chunkSize, chunkSize];
        specialArea = new bool[chunkSize, chunkSize];
    }
}