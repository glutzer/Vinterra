[Decorator("normal")]
public class NormalDecorator : Decorator
{
    public override int[] GetBlockLayers(int yPos, double heightGradient, int worldX, int worldZ, float depthNoise, float rainNormal, float fertility)
    {
        int[] returnLayers = new int[layerLength];
        for (int i = 0; i < layerLength; i++)
        {
            returnLayers[i] = GetId(layers[i]);
        }
        return returnLayers;
    }
}