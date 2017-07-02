using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barotrauma
{
    static public class Vector2Manipulator
    {
        public static Vector2 Substract(Vector2 v, Vector2 w)
        {
            return new Vector2(v.X - w.X, v.Y - w.Y);
        }

        public static Vector2 Add(Vector2 v, Vector2 w)
        {
            return new Vector2(v.X + w.X, v.Y + w.Y);
        }

        public static float Multiply(Vector2 v, Vector2 w)
        {
            return v.X * w.X + v.Y * w.Y;
        }

        public static Vector2 Multiply(Vector2 v, float mult)
        {
            return new Vector2(v.X * mult, v.Y * mult);
        }

        public static Vector2 Multiply(float mult, Vector2 v)
        {
            return new Vector2(v.X * mult, v.Y * mult);
        }

        public static float Cross(Vector2 v, Vector2 w)
        {
            return v.X * w.Y - v.Y * w.X;
        }

        public static bool Equals(Vector2 v, Vector2 w)
        {
            return (IsZero(v.X - w.X) && IsZero(v.Y - w.Y));
        }

        public static bool IsZero(float v)
        {
            return (Math.Abs(v) < 1e-10);
        }
    }
}
