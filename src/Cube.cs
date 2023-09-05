namespace Bacon;

using Spectre.Console;
using System.Diagnostics;
using System.Numerics;

using static System.Math;
using static System.Numerics.Vector3;


sealed class Cube
{
  record Plane(Vector3 Normal, float Offset);

  const int MayRayMarches     = 50      ;
  const int MayShadowMarches  = 25      ;

  const float MaxRayLength    = 20.0F   ;
  const float Tolerance       = 1.0E-2F ;
  const float NormalOff       = 0.001F  ;

  static Vector3 SunDir       = Normalize(new Vector3(1.0F, 0.75F, 1.0F))       ;
  static Vector3 SunCol       = HSV2RGB(new(0.075F, 0.8F, 0.05F))       ;
  static Vector3 SkyDir       = Normalize(new(-1.0F, 3.0F, -1.0F))      ;
  static Vector3 SkyCol       = HSV2RGB(new(0.55F, 0.8F, 0.8F))         ;
  static Vector3 GroundCol    = HSV2RGB(new(0.85F, 0.8F, 0.8F))         ;
  static Vector3 BoxCol       = HSV2RGB(new(0.55F, 0.5F, 0.66F))        ;

  static Vector3 RayOrigin    = new (0.0F, 2.0F, 10.0F);
  static Vector3 LookAt       = new (0.0F, 0.0F, 0.0F);
  static Vector3 Up           = new (0.0F, 1.0F, 0.0F);

  static Plane PlaneDim       = new (new(0.0F, 1.0F, 0.0F), 6.0F);

  static float FOV            = (float)Tan(2.0*PI/6.0);

  float Bounce                = 0.0F;
  Matrix4x4 Transform0        = Matrix4x4.Identity;

  public void CubeMe(bool parallel, int duration,int w, int h, bool lug00ber)
  {
    var res = new Vector2(w, h);

    var canvas = new Canvas(w, h)
    {
      Scale = false
    };

    var logo = new float[w*h];

    if (lug00ber)
    {
      for (int y = 0; y < h; ++y)
      {
        for (int x = 0; x < w; ++x)
        {
          var q = new Vector2(x, y)/res;
          q.Y = 1.0F-q.Y;
          var p = -Vector2.One+2.0F*q;
          p.X *= res.X/res.Y;

          float d = Lug00ber(p)-0.025F;
          float aa = 1.0F/res.Y;
          logo[x+w*y] = Smoothstep(aa, -aa, d);
        }
      }
    }

    var clock = Stopwatch.StartNew();

    static Vector3? RasterBar(float time, int y, Vector2 res)
    {
      const double w = 0.125;
      var ry = 2.0F*y/res.Y-1.0F;

      Vector3? rasterBar = null;

      for (int i = 0; i < 7; ++i)
      {
        var rd = Abs(ry+0.4*Sin(time+0.4F*i)+0.2)-w;
        if (rd < 0.0)
        {
          var f = (float)Abs(rd/w);
          rasterBar = HSV2RGB(new(i/7.0F, 1.0F-0.5F*f, 1.5F*f));
        }
      }

      return rasterBar;
    }

    void Updater(LiveDisplayContext ldc)
    {
      float halfDuration = duration*0.5F;
      var timeOut = duration*1000.0;
      while (clock.ElapsedMilliseconds < timeOut)
      {
        var time = (float)(clock.ElapsedMilliseconds/1000.0);

        var ro  = RayOrigin;
        var a   = (float)time*0.1;
        ro.X    = (float)(Cos(a)*RayOrigin.X+Sin(a)*RayOrigin.Z);
        ro.Z    = (float)(-Sin(a)*RayOrigin.X+Cos(a)*RayOrigin.Z);
        var ww  = Normalize(LookAt - ro);
        var uu  = Normalize(Cross(Up, ww));
        var vv  = Cross(ww, uu);

        float ft= Fract((float)time*0.5F)-0.5F;
        Bounce = 20.0F*ft*ft;

        Transform0 = 
            Matrix4x4.CreateRotationX(0.923F*time)
          * Matrix4x4.CreateRotationY(0.731F*time)
          * Matrix4x4.CreateRotationZ(0.521F*time);

        float fade = duration > 9 ? Smoothstep(0.0F, 3.0F, halfDuration - Abs(time-halfDuration)) : 1.0F; 
        if (parallel)
        {
          Parallel.For(0, h, y =>
          {
            for (int x = 0; x < w-1; ++x)
            {
              canvas.SetPixel(x, y, Compute(res, ww, uu, vv, ro, fade, logo, RasterBar(time, y, res), x, y));
            }
          });
        }
        else
        {
          for (int y = 0; y < h-1; ++y)
          {
            for (int x = 0; x < w-1; ++x)
            {
              canvas.SetPixel(x, y, Compute(res, ww, uu, vv, ro, fade, logo, RasterBar(time, y, res), x, y));
            }
          }
        }
        ldc.Refresh();
      }
    }
    AnsiConsole.Live(canvas).Start(Updater);
  }

  static float Fract(float v)
  {
    return (float)(v - Math.Floor(v));
  }

  static Vector3 Floor3(Vector3 v)
  {
    return new((float)Math.Floor(v.X), (float)Math.Floor(v.Y), (float)Math.Floor(v.Z));
  }


  static Vector3 Fract3(Vector3 v)
  {
    return v - Floor3(v);
  }

  static float Lerp(float a, float b, float t)
  {
    return a + t*(b-a);
  }

  static float Smoothstep(float edge0, float edge1, float x)
  {
    float t = Clamp((x - edge0) / (edge1 - edge0), 0.0F, 1.0F);
    return t * t * (3.0F - 2.0F * t);
  }

  static Vector3 HSV2RGB(Vector3 hsv) 
  {
    var K = new Vector3(1.0F, 2.0F / 3.0F, 1.0F / 3.0F);
    var p = Abs(Fract3(new Vector3(hsv.X) + K) * 6.0F - 3.0F*One);
    return hsv.Z * Vector3.Lerp(One, Clamp(p - One, Zero, One), hsv.Y);
  }

  static float Box(Vector2 p, Vector2 b) 
  {
    var d = Vector2.Abs(p)-b;
    return Vector2.Max(d,Vector2.Zero).Length() + Max(Max(d.X,d.Y),0.0F);
  }

  static float PMin(float a, float b, float k) 
  {
    float h = Clamp(0.5F+0.5F*(b-a)/k, 0.0F, 1.0F);
    return Lerp(b, a, h) - k*h*(1.0F-h);
  }

  static float Segment(Vector2 p, Vector2 a, Vector2 b) 
  {
      var pa = p-a;
      var ba = b-a;
      float h = Clamp(Vector2.Dot(pa,ba)/Vector2.Dot(ba,ba), 0.0F, 1.0F);
      return (pa - ba*h).Length();
  }

  static float Lug00ber(Vector2 p) {
    var p0 = p;
    p0.Y = Abs(p0.Y);
    p0 -= new Vector2(-0.705F, 0.41F);
    float d0 = p0.Length()-0.16F;
  
    var topy = 0.68F;
    var bp = p-new Vector2(0.27F, -0.8F);
    var d1 = Segment(p, new(0.72F, topy), new(0.27F, -0.8F))-0.06F;
    var d2 = Segment(p, new(-0.13F, topy), new(0.33F, -0.8F))-0.1F;
    var d3 = p.Y-(topy-0.066F);

    var d4 = Box(p-new Vector2(-0.1F, topy), new(0.25F, 0.03F))-0.01F;
    var d5 = Box(p-new Vector2(0.685F, topy), new(0.19F, 0.03F))-0.01F;
    var d6 = Min(d4, d5);
  
    var ax7   = Vector2.Normalize(new Vector2(0.72F, topy)-new Vector2(0.27F, -0.8F));
    var nor7  = new Vector2(ax7.Y, -ax7.X);
    var d7    = Vector2.Dot(p, nor7)+Vector2.Dot(nor7, -new Vector2(0.72F, topy))+0.05F;
  
    d2 = Max(d2, d7);
    float d = d1;
    d = PMin(d,d2, 0.025F);
    d = Max(d, d3);
    d = PMin(d, d6, 0.1F);
    d = Min(d,d0);
  
    return d; 
  }


  static float RayPlane(Vector3 ro, Vector3 rd, Plane p)
  {
    return -(Dot(ro,p.Normal)+p.Offset)/Dot(rd,p.Normal);
  }

  static float Sphere8(Vector3 p, float r) {
    p *= p;
    p *= p;
    return ((float)Pow(Dot(p, p), 0.125))-r;
  }

  static float Sphere(Vector3 p, float r) {
    return p.Length() - r;
  }

  float DistanceField(Vector3 p) 
  {
    var p0 = p;
    var p1 = p;
    p1.Y += Bounce;
    p1 = Abs(p1);
    p1.X -= 5.0F;
    p1.Z -= 5.0F;

    p0 = Transform(p0, Transform0);

    float d0 = Sphere8(p0, 3.5F);
    float d1 = Sphere(p1, 1.0F);
    float d = d0;
    d = Min(d, d1);
    return d;
  }

  float RayMarch(Vector3 ro, Vector3 rd) 
  {
    float t     = 0.0F;

    for (int i = 0; i < MayRayMarches; ++i) {
      if (t > MaxRayLength) break;
      float d = DistanceField(ro + rd*t);
      if (d < Tolerance) break;
      t += d;
    }

    return t;
  }

  Vector3 Normal(Vector3 pos) {
    var epsx = new Vector3(NormalOff, 0.0F, 0.0F);
    var epsy = new Vector3(0.0F, NormalOff, 0.0F);
    var epsz = new Vector3(0.0F, 0.0F, NormalOff);
    return Normalize(new(
        DistanceField(pos+epsx)-DistanceField(pos-epsx)
      , DistanceField(pos+epsy)-DistanceField(pos-epsy)
      , DistanceField(pos+epsz)-DistanceField(pos-epsz))
      );
  }

  float SoftShadow(Vector3 ps, Vector3 ld, float mint, float k)
  {
    float res = 1.0F;
    float t = mint*6.0F;
    for (int i=0; i<MayShadowMarches; ++i) 
    {
      Vector3 p = ps + ld*t;
      float d = DistanceField(p);
      res = Min(res, k*d/t);
      if (res < Tolerance) break;
      t += Max(d, mint);
    }
    return Min(Max(res, 0.0F), 1.0F);
  }

  Color Compute(Vector2 res, Vector3 ww, Vector3 uu, Vector3 vv, Vector3 ro, float fade, float[] logo, Vector3? rasterBar, int x, int y)
  {
    var q = new Vector2(x, y)/res;
    q.Y = 1.0F-q.Y;
    var p = -Vector2.One+2.0F*q;
    p.X *= res.X/res.Y;
    var rd = Normalize(-p.X*uu + p.Y*vv+FOV*ww);

    var tp = RayPlane(ro, rd, PlaneDim);
    var te = RayMarch(ro, rd);

    var col = Zero;

    if (te < MaxRayLength && (tp < 0.0 || te < tp)) 
    {
      var ep = ro+rd*te;
      var en = Normal(ep);
      var er = Reflect(rd, en);
    
      var sunDif = Max(Dot(en, SunDir), 0.0F);
      var skyDif = Max(Dot(en, SkyDir), 0.0F);
      var sunSpe = (float)Pow(Max(Dot(er, SunDir), 0.0F), 10.0);
      sunDif *= sunDif;

      col += 0.1F*GroundCol + sunDif*One + skyDif*SquareRoot(SkyCol);
      col *= BoxCol;
      col += sunSpe*One;
    } 
    else if (tp > 0.0F)
    {
      if (rasterBar is not null)
      {
        col = rasterBar.Value;
      }
      else
      {
        var gp = ro+rd*tp;
        var gn = PlaneDim.Normal;
        var gr = Reflect(rd, gn);

        var sunDif = Max(Dot(gn, SunDir), 0.0F);
        var sunSpe = (float)Pow(Max(Dot(gr, SunDir), 0.0F), 10.0);
        sunDif *= sunDif;

        var sf = SoftShadow(gp, SunDir, 0.1F, 8.0F);
        col += 1.5F*sf*sunDif*One+0.25F*SquareRoot(SkyCol);
        col *= GroundCol;
        col += sunSpe*One;
        col /= 1.0F+0.0025F*tp*tp;
      }
    } 
    else
    {
      if (rasterBar is not null)
      {
        col = rasterBar.Value;
      }
      else
      {
        col += SkyCol;
        col += SunCol/(1.01F-Dot(rd, SunDir));
        col += new Vector3((float)(0.1/Max(Sqrt(rd.Y), 0.2)));
      }
    }

    col = Clamp(col, Zero, One);

    col = Vector3.Lerp(col, One, logo[x + (int)(y*res.X)]);

    col *= fade;

    col = SquareRoot(col);
    col *= 255.0F;

    return new((byte)col.X, (byte)col.Y, (byte)col.Z);
  }

}


