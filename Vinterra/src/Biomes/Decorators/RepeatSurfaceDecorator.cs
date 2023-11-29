[Decorator("repeatsurface")]
public class RepeatSurfaceDecorator : Decorator
{
    public override int[] GetBlockLayers(int yPos, double heightGradient, int worldX, int worldZ, float depthNoise, float rainNormal, float fertility)
    {
        int[] returnLayers = new int[]
        {
            GetId(layers[yPos / repeatLayerThickness % layerLength])
        };

        return returnLayers;
    }
}