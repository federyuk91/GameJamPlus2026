using System;

namespace GameJamPlus.Gameplay
{
    [Flags]
    public enum TestObservation
    {
        None = 0,
        EnjoysFood = 1,
        Exercises = 2,
        LovesPool = 4,
        ExtendedStay = 8,
        LittleMechanic = 16,
        VeryCurious = 32,
        Electrician = 64,
        SenseOfDirection = 128
    }
}
