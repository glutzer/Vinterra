using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

public class SmoothCurve : Module
{
    readonly Module source;

    readonly List<ControlPoint> controlPoints = new();

    public SmoothCurve(Module source)
    {
        this.source = source;
    }

    public SmoothCurve()
    {
    }

    public SmoothCurve CP(double inputValue, double outputValue)
    {
        int index = FindInsertionPos(inputValue);
        InsertAtPos(index, inputValue, outputValue);
        return this;
    }

    private void InsertAtPos(int insertionPos, double inputValue, double outputValue)
    {
        ControlPoint newPoint = new(inputValue, outputValue);

        controlPoints.Insert(insertionPos, newPoint);
    }

    public ControlPoint[] ControlPoints()
    {
        return controlPoints.ToArray();
    }

    public int FindInsertionPos(double inputValue)
    {
        int insertionPos;

        for (insertionPos = 0; insertionPos < controlPoints.Count; insertionPos++)
        {
            if (inputValue < controlPoints[insertionPos].inputValue)
            {
                //We found the array index in which to insert the new control point. Exit now.
                break;
            }
            else if (inputValue == controlPoints[insertionPos].inputValue)
            {
                //Each control point is required to contain a unique input value, so throw an exception.
                throw new Exception("Input value must be unique.");
            }
        }

        return insertionPos;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        int size = controlPoints.Count;

        //if (size < 3) throw new Exception("Curve module must have at least 3 control points.");

        double sourceModuleValue = source.Get(x, z, sampleData);

        int indexPos;
        for (indexPos = 0; indexPos < size; indexPos++)
        {
            if (sourceModuleValue < controlPoints[indexPos].inputValue)
            {
                break;
            }
        }

        int lastIndex = size - 1;

        int index0 = Math.Clamp(indexPos - 1, 0, lastIndex);
        int index1 = Math.Clamp(indexPos, 0, lastIndex);

        if (index0 == index1)
        {
            return controlPoints[index1].outputValue;
        }

        double input0 = controlPoints[index0].inputValue;
        double input1 = controlPoints[index1].inputValue;
        double alpha = GameMath.SmoothStep((sourceModuleValue - input0) / (input1 - input0));

        return GameMath.Lerp(controlPoints[index0].outputValue, controlPoints[index1].outputValue, alpha);
    }

    public double Get(double sourceModuleValue)
    {
        int size = controlPoints.Count;

        //if (size < 3) throw new Exception("Curve module must have at least 3 control points.");

        int indexPos;
        for (indexPos = 0; indexPos < size; indexPos++)
        {
            if (sourceModuleValue < controlPoints[indexPos].inputValue)
            {
                break;
            }
        }

        int lastIndex = size - 1;

        int index0 = Math.Clamp(indexPos - 1, 0, lastIndex);
        int index1 = Math.Clamp(indexPos, 0, lastIndex);

        if (index0 == index1)
        {
            return controlPoints[index1].outputValue;
        }

        double input0 = controlPoints[index0].inputValue;
        double input1 = controlPoints[index1].inputValue;
        double alpha = GameMath.SmoothStep((sourceModuleValue - input0) / (input1 - input0));

        return GameMath.Lerp(controlPoints[index0].outputValue, controlPoints[index1].outputValue, alpha);
    }
}