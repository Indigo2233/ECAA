using System;

namespace AltAzDeRotator
{
    public static class MathEngine
    {
        /// <summary>
        /// Calculates the required rotation rate for an Alt-Az mount to compensate for field rotation.
        /// </summary>
        /// <param name="altitude">Current altitude in degrees.</param>
        /// <param name="azimuth">Current azimuth in degrees.</param>
        /// <param name="latitude">Site latitude in degrees.</param>
        /// <returns>Required rotator speed in degrees per hour.</returns>
        public static double CalculateRotationRate(double altitude, double azimuth, double latitude)
        {
            // Convert degrees to radians for C# Math functions
            double altRad = altitude * (Math.PI / 180.0);
            double azRad = azimuth * (Math.PI / 180.0);
            double latRad = latitude * (Math.PI / 180.0);

            // Earth's rotation rate is ~15.04 degrees per hour
            // Note: Make sure azimuth convention aligns with the math (0 = North typical, but verify for your mount)
            double rate = 15.04 * (Math.Cos(latRad) * Math.Cos(azRad)) / Math.Cos(altRad);
            
            return rate; 
        }
    }
}
