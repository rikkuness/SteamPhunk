
using UnityEngine;
using System;

/** USAGE INSTRUCTIONS 
 * 
 * Pass lat/lng to ToPointPixel to return pixel dimensions from top left
 * 
 * examples using -4.5895, 137.4417 (Bradbury Landing site)
 * 
 * For the world map (512x256 pixels)
   var a = new PointUtilityScaler(512,256);
   var b = a.ToPointPixel(-4.5895, 137.4417);
   Debug.Log(b.X);
   Debug.Log(b.Y);

 * For looking at which tile we need to load, 
   our world is 64x32 tiles in dimension
   var a = new PointUtilityScaler(64, 32);
   var b = a.ToPointPixel(-4.5895, 137.4417);
   Debug.Log(b.X);
   Debug.Log(b.Y);

 * For plotting on the unity world where the point of interest should be

    var a = new PointUtilityScaler(64*256 , 32*256);
    var b = a.ToPointPixel(-4.5895, 137.4417);
    var c = a.FromPointPixelToUnityPixel(b);
    Debug.Log(c.X);
    Debug.Log(c.Y);

 * 
 * */
public static class PointUtility
{

    public const double MercatorGoogleHeight = 256;
    public const double MercatorGoogleWidth = 512; // our map is 512 wide
    public const double PixelLongintudeOrigin = MercatorGoogleWidth / 2;
    public const double PixelLatitudeOrigin = MercatorGoogleHeight / 2;
    public const double PixelsPerLongintudeDegre = MercatorGoogleWidth / 360;
    public const double RadsPerLatitudeDegre = MercatorGoogleHeight / (2 * System.Math.PI);

}


public class PointUtilityScaler
{

    public double MercatorGoogleHeight { get; set; }
    public double MercatorGoogleWidth { get; set; }
    public double PixelLongintudeOrigin { get; set; }
    public double PixelLatitudeOrigin { get; set; }
    public double PixelsPerLongintudeDegre { get; set; }
    public double RadsPerLatitudeDegre { get; set; }

    public PointUtilityScaler (int width, int height)
    {
        this.MercatorGoogleHeight   = height;
        this.MercatorGoogleWidth    = width;

        this.PixelLongintudeOrigin      = this.MercatorGoogleWidth / 2;
        this.PixelLatitudeOrigin        = this.MercatorGoogleHeight / 2;
        this.PixelsPerLongintudeDegre   = this.MercatorGoogleWidth / 360;
        this.RadsPerLatitudeDegre       = this.MercatorGoogleHeight / (2 * System.Math.PI);
    }

    public PointCoordinates ToPointCoordinates(double X, double Y)
    {
        var lng = (X - this.PixelLongintudeOrigin) / this.PixelsPerLongintudeDegre;
        var latRad = (Y - this.PixelLatitudeOrigin) / -this.RadsPerLatitudeDegre;
        var lat = (2 * System.Math.Atan(System.Math.Exp(latRad)) - System.Math.PI / 2) * Mathf.Rad2Deg;
        return new PointCoordinates()
        {
            Latitude = lat,
            Longitude = lng
        };
    }

    /**
     * returns a set of pixels from top left corner
     */
    public PointPixel ToPointPixel(double Latitude, double Longitude)
    {
        var x = this.PixelLongintudeOrigin + this.PixelsPerLongintudeDegre * Longitude;
        var siny = System.Math.Sin(Latitude * Mathf.Deg2Rad);
        var y = this.PixelLatitudeOrigin - (System.Math.Log((1 + siny) / (1 - siny)) / 2) * this.RadsPerLatitudeDegre;
        return new PointPixel() { X = x, Y = y };
    }

    /**
     * Takes a point pixel from this system and converts to unity X/Y
     */
    public PointUnityPixel FromPointPixelToUnityPixel(PointPixel pixel)
    {
        var p = new PointUnityPixel();
        p.X = pixel.X;
        p.Y = this.MercatorGoogleHeight - pixel.Y;
        return p;
    }

}

public class PointPixel
{
    public double X { get; set; }
    public double Y { get; set; }

}

public class PointUnityPixel
{
    public double X { get; set; }
    public double Y { get; set; }

}

public class PointCoordinates
{

    public double Latitude { get; set; }
    public double Longitude { get; set; }

}