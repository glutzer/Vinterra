using System;
using Vintagestory.API.MathTools;

/// <summary>
/// Terraces values from 0 to 1.
/// </summary>
public class Terrace : Module
{
    readonly Module module;

    int controlPointCount = 0;

    double[] controlPoints = Array.Empty<double>();

    public Terrace(Module module, int points)
    {
        this.module = module;
        MakeControlPoints(points);
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double sourceModuleValue = module.Get(x, z, sampleData);

        int indexPos;

        for (indexPos = 0; indexPos < controlPointCount; indexPos++)
        {
            if (sourceModuleValue < controlPoints[indexPos])
            {
                break;
            }
        }

        int index0 = Math.Clamp(indexPos - 1, 0, controlPointCount - 1);
        int index1 = Math.Clamp(indexPos, 0, controlPointCount - 1);

        if (index0 == index1)
        {
            return controlPoints[index1];
        }

        double value0 = controlPoints[index0];
        double value1 = controlPoints[index1];

        double alpha = (sourceModuleValue - value0) / (value1 - value0);

        //Squaring alpha produces a curve to the next point, producing a terrace effect
        alpha *= alpha;

        return GameMath.Lerp(value0, value1, alpha);
    }

    public void AddControlPoint(double value)
    {
        int insertionPos = FindInsertionPos(value);
        InsertAtPos(insertionPos, value);
    }

    public void ClearControlPoints()
    {
        controlPoints = null;
        controlPointCount = 0;
    }

    public void MakeControlPoints(int controlPointCount)
    {
        if (controlPointCount < 2)
        {
            throw new Exception("Must have more than 2 control points.");
        }

        ClearControlPoints();

        double terraceStep = 1.0 / (controlPointCount - 1.0);
        double curValue = 0;

        for (int i = 0; i < controlPointCount; i++)
        {
            AddControlPoint(curValue);
            curValue += terraceStep;
        }
    }

    private int FindInsertionPos(double value)
    {
        int insertionPos;

        for (insertionPos = 0; insertionPos < controlPointCount; insertionPos++)
        {
            if (value < controlPoints[insertionPos])
            {
                break;
            }
            else if (value == controlPoints[insertionPos])
            {
                throw new Exception("Value must be unique.");
            }
        }

        return insertionPos;
    }

    private void InsertAtPos(int insertionPos, double value)
    {
        double[] newControlPoints = new double[controlPointCount + 1];

        for (int i = 0; i < controlPointCount; i++)
        {
            if (i < insertionPos)
            {
                newControlPoints[i] = controlPoints[i];
            }
            else
            {
                newControlPoints[i + 1] = controlPoints[i];
            }
        }

        controlPoints = newControlPoints;

        ++controlPointCount;

        controlPoints[insertionPos] = value;
    }
}