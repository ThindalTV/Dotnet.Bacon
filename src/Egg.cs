namespace Bacon;

using Spectre.Console;
using System.Diagnostics;
using System.Numerics;

using static System.Math;
using static System.Numerics.Vector3;


sealed class Egg
{
  record Plane(Vector3 Normal, float Offset);

  const int MayRayMarches     = 50      ;
  const int MayShadowMarches  = 25      ;

  const float MaxRayLength    = 20.0F   ;
  const float Tolerance       = 1.0E-2F ;
  const float NormalOff       = 0.001F  ;

  static Vector3 SunDir       = Normalize(new(1.0F, 0.75F, 1.0F))       ;
  static Vector3 SkyDir       = Normalize(new(-1.0F, 3.0F, -1.0F))      ;
  static Vector3 SkyCol       = HSV2RGB(new(0.55F, 0.8F, 0.8F))         ;
  static Vector3 GroundCol    = HSV2RGB(new(0.15F, 0.8F, 0.8F))         ;
  static Vector3 EggCol       = HSV2RGB(new(50.0F/360.0F, 0.24F, 1.0F)) ;

  static Vector3 RayOrigin    = new (0.0F, 2.0F, 10.0F);
  static Vector3 LookAt       = new (0.0F, 0.0F, 0.0F);
  static Vector3 Up           = new (0.0F, 1.0F, 0.0F);

  static Plane PlaneDim       = new (new(0.0F, 1.0F, 0.0F), 3.0F);
  static Vector3 EggDim       = 3.0F*(new Vector3(1.0F, 0.5F, 0.0F));

  static float Sqrt3          = (float)Sqrt(3.0);
  static float FOV            = (float)Tan(2.0*PI/6.0);

  float Bounce  = 0.0F;

  public void EggMe(bool parallel, int duration,int w, int h)
  {
    var res = new Vector2(w, h);

    var canvas = new Canvas(w, h)
    {
      Scale = false
    };
    var clock = Stopwatch.StartNew();

    void Updater(LiveDisplayContext ldc)
    {
      double timeOut = duration*1000.0;
      while (clock.ElapsedMilliseconds < timeOut)
      {
        var time = clock.ElapsedMilliseconds/1000.0;

        var ro  = RayOrigin;
        var a   = (float)time*0.1;
        ro.X    = (float)(Cos(a)*RayOrigin.X+Sin(a)*RayOrigin.Z);
        ro.Z    = (float)(-Sin(a)*RayOrigin.X+Cos(a)*RayOrigin.Z);
        var ww  = Normalize(LookAt - ro);
        var uu  = Normalize(Cross(Up, ww));
        var vv  = Cross(ww, uu);

        float ft= Fract((float)time)-0.5F;
        Bounce  = -2.0F*(0.25F-ft*ft);

        if (parallel)
        {
          Parallel.For(0, h, y =>
          {
            for (int x = 0; x < w-1; ++x)
            {
              canvas.SetPixel(x, y, Compute(res, ww, uu, vv, ro, x, y));
            }
          });
        }
        else
        {
          for (int y = 0; y < h-1; ++y)
          {
            for (int x = 0; x < w-1; ++x)
            {
              canvas.SetPixel(x, y, Compute(res, ww, uu, vv, ro, x, y));
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

  static Vector3 HSV2RGB(Vector3 hsv) 
  {
    var K = new Vector3(1.0F, 2.0F / 3.0F, 1.0F / 3.0F);
    var p = Abs(Fract3(new Vector3(hsv.X) + K) * 6.0F - 3.0F*One);
    return hsv.Z * Lerp(One, Clamp(p - One, Zero, One), hsv.Y);
  }

  static float RayPlane(Vector3 ro, Vector3 rd, Plane p)
  {
    return -(Dot(ro,p.Normal)+p.Offset)/Dot(rd,p.Normal);
  }

  static float Egg2(Vector2 p, Vector2 dim) 
  {
    p.X = Abs(p.X);
    float r = dim.X - dim.Y;
    return ((p.X<0.0F)      ? new Vector2(p.X,  p.Y     ).Length() - r :
            (Sqrt3*(p.X+r)<p.Y)? new Vector2(p.X,  p.Y-Sqrt3*r).Length()     :
                              new Vector2(p.X+r,p.Y     ).Length() - 2.0F*r) - dim.Y;
  }

  static float Egg3(Vector3 p, Vector3 dim) 
  {
    Vector2 w = new (p.X, p.Z);
    Vector2 q = new (w.Length() - dim.Z, p.Y);
    return Egg2(q, new(dim.X, dim.Y));
  }

  float DistanceField(Vector3 p) 
  {
    p.Y += Bounce;
    return Egg3(p, EggDim)-0.25F;
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

  Color Compute(Vector2 res, Vector3 ww, Vector3 uu, Vector3 vv, Vector3 ro, int x, int y)
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
      Vector3 ep = ro+rd*te;
      Vector3 en = Normal(ep);
    
      float sunDif = Max(Dot(en, SunDir), 0.0F);
      float skyDif = Max(Dot(en, SkyDir), 0.0F);
    
      col += 0.1F*GroundCol + sunDif*One + skyDif*SquareRoot(SkyCol);
      col *= EggCol;
    } 
    else if (tp > 0.0F)
    {
      Vector3 gp = ro+rd*tp;
      Vector3 gn = PlaneDim.Normal;

      float sunDif = Max(Dot(gn, SunDir), 0.0F);
      float skyDif = Max(Dot(gn, SkyDir), 0.0F);

      float sf = SoftShadow(gp, SunDir, 0.1F, 8.0F);
      col += 1.5F*sf*sunDif*One+0.25F*skyDif*SquareRoot(SkyCol);
      col *= GroundCol;
      col *= (float)Tanh(40.0F*(rd.Y*rd.Y)+0.125F);
    } 
    else 
    {
      col += SkyCol;
      col += new Vector3((float)(0.1/Max(Sqrt(rd.Y), 0.2)));
    }

    col = SquareRoot(col);
    col = Clamp(col, Zero, One);
    col *= 255.0F;

    return new((byte)col.X, (byte)col.Y, (byte)col.Z);
  }

}


