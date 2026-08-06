using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class AstroNightWindow {
        public DateTime? Dusk { get; set; }
        public DateTime? Dawn { get; set; }
        public TimeSpan Duration => (Dawn.HasValue && Dusk.HasValue && Dawn > Dusk) ? Dawn.Value - Dusk.Value : TimeSpan.Zero;
        public bool UsedNauticalFallback { get; set; }
        public double TargetSunAltitude { get; set; }
    }

    public static class AstroUtils {
        /// <summary>
        /// Calculates the Sun's altitude in degrees at a given UTC time and geographical position.
        /// Uses the standard NOAA solar position algorithm.
        /// Latitude: Positive North, Negative South (-90 to +90)
        /// Longitude: Positive East, Negative West (-180 to +180)
        /// </summary>
        public static double CalculateSunAltitude(DateTime utcTime, double lat, double lon) {
            double year = utcTime.Year;
            double month = utcTime.Month;
            double day = utcTime.Day + utcTime.Hour / 24.0 + utcTime.Minute / 1440.0 + utcTime.Second / 86400.0 + utcTime.Millisecond / 86400000.0;

            if (month <= 2) {
                year -= 1;
                month += 12;
            }

            double a = Math.Floor(year / 100.0);
            double b = 2.0 - a + Math.Floor(a / 4.0);
            double jd = Math.Floor(365.25 * (year + 4716.0)) + Math.Floor(30.6001 * (month + 1.0)) + day + b - 1524.5;
            double t = (jd - 2451545.0) / 36525.0;

            // Geometric Mean Longitude of Sun (deg)
            double l0 = (280.46646 + t * (36000.76983 + t * 0.0003032)) % 360.0;
            if (l0 < 0) l0 += 360.0;

            // Geometric Mean Anomaly of Sun (deg)
            double m = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            double mRad = DegreesToRadians(m);

            // Eccentricity of Earth Orbit
            double e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);

            // Sun Equation of Center (deg)
            double C = Math.Sin(mRad) * (1.914602 - t * (0.004817 + 0.000014 * t))
                     + Math.Sin(2.0 * mRad) * (0.019993 - 0.000101 * t)
                     + Math.Sin(3.0 * mRad) * 0.000289;

            // Sun True Longitude & Apparent Longitude (deg)
            double sunTrueLong = l0 + C;
            double omega = 125.04 - 1934.136 * t;
            double sunAppLong = sunTrueLong - 0.00569 - 0.00478 * Math.Sin(DegreesToRadians(omega));
            double sunAppLongRad = DegreesToRadians(sunAppLong);

            // Mean Obliquity of Ecliptic (deg)
            double seconds = 21.448 - t * (46.815 + t * (0.00059 - t * 0.001813));
            double meanObliq = 23.0 + (26.0 + seconds / 60.0) / 60.0;
            double obliqCorr = meanObliq + 0.00256 * Math.Cos(DegreesToRadians(omega));
            double obliqCorrRad = DegreesToRadians(obliqCorr);

            // Sun Declination (deg)
            double sinDec = Math.Sin(obliqCorrRad) * Math.Sin(sunAppLongRad);
            double decRad = Math.Asin(sinDec);

            // Equation of Time (minutes)
            double y = Math.Tan(obliqCorrRad / 2.0) * Math.Tan(obliqCorrRad / 2.0);
            double l0Rad = DegreesToRadians(l0);
            double eqTime = 4.0 * RadiansToDegrees(
                y * Math.Sin(2.0 * l0Rad)
                - 2.0 * e * Math.Sin(mRad)
                + 4.0 * e * y * Math.Sin(mRad) * Math.Cos(2.0 * l0Rad)
                - 0.5 * y * y * Math.Sin(4.0 * l0Rad)
                - 1.25 * e * e * Math.Sin(2.0 * mRad)
            );

            // True Solar Time (minutes)
            double utcMinutes = utcTime.TimeOfDay.TotalMinutes;
            double trueSolarTimeMinutes = (utcMinutes + eqTime + 4.0 * lon) % 1440.0;
            if (trueSolarTimeMinutes < 0) trueSolarTimeMinutes += 1440.0;

            // Hour Angle (deg)
            double hourAngle = trueSolarTimeMinutes / 4.0 - 180.0;
            if (hourAngle < -180.0) hourAngle += 360.0;

            // Solar Zenith Angle & Altitude (deg)
            double latRad = DegreesToRadians(lat);
            double hourAngleRad = DegreesToRadians(hourAngle);

            double cosZenith = Math.Sin(latRad) * Math.Sin(decRad) + Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(hourAngleRad);
            cosZenith = Math.Max(-1.0, Math.Min(1.0, cosZenith));
            double zenithRad = Math.Acos(cosZenith);
            double zenith = RadiansToDegrees(zenithRad);
            double altitude = 90.0 - zenith;

            return altitude;
        }

        public static double CalculateSunPosition(DateTime utcTime, double lat, double lon) {
            return CalculateSunAltitude(utcTime, lat, lon);
        }

        /// <summary>
        /// Calculates Astronomical Night Window (Astro Dusk to Astro Dawn) anchored to 12:00 PM (noon).
        /// Implements Tiered Twilight Fallback: Astro (-18°) -> Nautical (-12°) -> Civil (-6°).
        /// </summary>
        public static AstroNightWindow GetAstronomicalNightWindow(DateTime sessionStart, double lat, double lon) {
            // Anchor date to 12:00 PM (noon)
            DateTime astroAnchorDate = sessionStart.TimeOfDay < TimeSpan.FromHours(12) ? sessionStart.Date.AddDays(-1) : sessionStart.Date;
            DateTime localNoon = astroAnchorDate.AddHours(12);

            int totalSteps = 1440; // 1-minute steps over 24 hours
            double minAlt = 90.0;
            double[] altAtStep = new double[totalSteps + 1];

            for (int i = 0; i <= totalSteps; i++) {
                DateTime dtLocal = localNoon.AddMinutes(i);
                DateTime dtUtc = dtLocal.Kind == DateTimeKind.Utc ? dtLocal : dtLocal.ToUniversalTime();
                double alt = CalculateSunAltitude(dtUtc, lat, lon);
                altAtStep[i] = alt;
                if (alt < minAlt) minAlt = alt;
            }

            double targetAlt = -18.0;
            bool usedNauticalFallback = false;

            if (minAlt <= -18.0) {
                targetAlt = -18.0;
                usedNauticalFallback = false;
            } else if (minAlt <= -12.0) {
                targetAlt = -12.0;
                usedNauticalFallback = true;
            } else if (minAlt <= -6.0) {
                targetAlt = -6.0;
                usedNauticalFallback = true;
            } else {
                return new AstroNightWindow { Dusk = null, Dawn = null, UsedNauticalFallback = true, TargetSunAltitude = 0 };
            }

            DateTime? dusk = null;
            int duskStepIndex = -1;
            for (int i = 0; i < totalSteps; i++) {
                if (altAtStep[i] >= targetAlt && altAtStep[i + 1] < targetAlt) {
                    double frac = (targetAlt - altAtStep[i]) / (altAtStep[i + 1] - altAtStep[i]);
                    long extraTicks = (long)(frac * TimeSpan.FromMinutes(1).Ticks);
                    dusk = localNoon.AddMinutes(i).AddTicks(extraTicks);
                    duskStepIndex = i;
                    break;
                }
            }

            DateTime? dawn = null;
            int searchStart = duskStepIndex >= 0 ? duskStepIndex + 1 : 0;
            for (int i = searchStart; i < totalSteps; i++) {
                if (altAtStep[i] < targetAlt && altAtStep[i + 1] >= targetAlt) {
                    double frac = (targetAlt - altAtStep[i]) / (altAtStep[i + 1] - altAtStep[i]);
                    long extraTicks = (long)(frac * TimeSpan.FromMinutes(1).Ticks);
                    dawn = localNoon.AddMinutes(i).AddTicks(extraTicks);
                    break;
                }
            }

            return new AstroNightWindow {
                Dusk = dusk,
                Dawn = dawn,
                UsedNauticalFallback = usedNauticalFallback,
                TargetSunAltitude = targetAlt
            };
        }

        private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
        private static double RadiansToDegrees(double rad) => rad * 180.0 / Math.PI;
    }
}
