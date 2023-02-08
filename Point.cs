using System;

public class PointD
{
	public double X;
	public double Y;
	public PointD(double x, double y)
	{
		X = x;
		Y = y;
	}
    public override bool Equals(object? obj)
    {
		if(obj is PointD p)
        {
			return X == p.X && Y == p.Y;
        }
		return base.Equals(obj);
    }
}
