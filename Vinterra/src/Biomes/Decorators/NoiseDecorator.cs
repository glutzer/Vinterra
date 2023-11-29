[Decorator("noise")]
public class NoiseDecorator : Decorator
{
    public override int[] GetBlockLayers(int yPos, double heightGradient, int worldX, int worldZ, float depthNoise, float rainNormal, float fertility)
    {
        int[] returnLayers = new int[layerLength];
        for (int i = 0; i < layerLength; i++)
        {
            returnLayers[i] = GetThreshold(noise.GetPosNoise(worldX, worldZ));
        }
        return returnLayers;
    }

    //Up to 0.5 is first threshold, 1 is last threshold
    public int GetThreshold(float value)
    {
        for (int i = 0; i < layerLength; i++)
        {
            if (value <= noiseThresholds[i]) return GetId(layers[i]);
        }

        return layers[0];
    }
}