using System;
using System.Collections.Generic;

public class CubicCurve : Module
{
    readonly Module source;

    readonly List<ControlPoint> controlPoints = new();

    public CubicCurve(Module source)
    {
        this.source = source;
    }

    public CubicCurve CP(double inputValue, double outputValue)
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

        if (size < 4) throw new Exception("Curve module must have at least 4 control points.");

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

        int index0 = Math.Clamp(indexPos - 2, 0, lastIndex);
        int index1 = Math.Clamp(indexPos - 1, 0, lastIndex);
        int index2 = Math.Clamp(indexPos, 0, lastIndex);
        int index3 = Math.Clamp(indexPos + 1, 0, lastIndex);

        if (index1 == index2)
        {
            return controlPoints[index1].outputValue;
        }

        double input0 = controlPoints[index1].inputValue;
        double input1 = controlPoints[index2].inputValue;
        double alpha = (sourceModuleValue - input0) / (input1 - input0);

        return VMath.CubicInterpolation(
            controlPoints[index0].outputValue, controlPoints[index1].outputValue, controlPoints[index2].outputValue,
            controlPoints[index3].outputValue, alpha);
    }
}