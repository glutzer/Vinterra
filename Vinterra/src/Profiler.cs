using System;
using System.Diagnostics;

public static class Profiler
{
    public static Stopwatch stopwatch = new();
    public static string currentProfile;
    public static int count = 0;
    public static long currentTime = 0;

    public static void Start(string profile)
    {
        currentProfile = profile;
        stopwatch.Reset();
        stopwatch.Start();
    }

    public static void Stop() 
    {
        stopwatch.Stop();
        count++;
        currentTime += stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"{currentProfile}: average: {currentTime / count}ms current: {stopwatch.ElapsedMilliseconds} {count}");
    }
}