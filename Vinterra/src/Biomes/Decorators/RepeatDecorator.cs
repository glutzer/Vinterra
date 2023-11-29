using System;

[Decorator("repeat")]
public class RepeatDecorator : Decorator
{
    public override int[] GetBlockLayers(int yPos, double heightGradient, int worldX, int worldZ, float depthNoise, float rainNormal, float fertility)
    {
        int[] returnLayers = new int[Math.Min(Math.Max(yPos - seaLevel, 2), repeatTimes)]; //Go to sea level, or the repeat times

        for (int i = 0; i < returnLayers.Length; i++)
        {
            returnLayers[i] = GetId(layers[yPos / repeatLayerThickness % layerLength]);
            yPos--;
        }

        return returnLayers;
    }
}