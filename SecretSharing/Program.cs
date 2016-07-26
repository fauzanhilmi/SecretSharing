using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretSharing
{
    class Field
    {
        public const int Order = 256;
        //irreducible polynomial used : x^8 + x^4 + x^3 + x^2 + 1 (0x11D)
        public const int Polynomial = 0x11D;
        //generator to be used in Exp & Log table generation
        public const byte Generator = 0x2;
        public static byte[] Exp;
        public static byte[] Log;

        private byte value;

        public Field()
        {
            value = 0;
        }

        public Field(byte _value)
        {
            value = _value;
        }

        //generates Exp & Log table for fast multiplication operator
        static Field()
        {
            Exp = new byte[Order];
            Log = new byte[Order];

            byte val = 0x01;
            for (int i = 0; i < Order; i++)
            {
                Exp[i] = val;
                if (i < Order - 1)
                {
                    Log[val] = (byte)i;
                }
                val = multiply(Generator, val);
            }
        }

        //getters and setters
        public byte ToByte()
        {
            return value;
        }

        public void SetValue(byte _value)
        {
            value = _value;
        }

        //operators
        public static explicit operator Field(byte b)
        {
            Field f = new Field(b);
            return f;
        }

        public static explicit operator byte (Field f)
        {
            return f.value;
        }

        public static Field operator +(Field Fa, Field Fb)
        {
            byte bres = (byte)(Fa.value ^ Fb.value);
            return new Field(bres);
        }

        public static Field operator -(Field Fa, Field Fb)
        {
            byte bres = (byte)(Fa.value ^ Fb.value);
            return new Field(bres);
        }

        public static Field operator *(Field Fa, Field Fb)
        {
            Field FRes = new Field(0);
            if (Fa.value != 0 && Fb.value != 0)
            {
                byte bres = (byte)((Log[Fa.value] + Log[Fb.value]) % (Order - 1));
                bres = Exp[bres];
                FRes.value = bres;
            }
            return FRes;
        }

        public static Field operator /(Field Fa, Field Fb)
        {
            if (Fb.value == 0)
            {
                throw new System.ArgumentException("Divisor cannot be 0", "Fb");
            }

            Field Fres = new Field(0);
            if (Fa.value != 0)
            {
                byte bres = (byte)(((Order - 1) + Log[Fa.value] - Log[Fb.value]) % (Order - 1));
                bres = Exp[bres];
                Fres.value = bres;
            }
            return Fres;
        }

        public static Field pow(Field f, byte exp)
        {
            Field fres = new Field(1);
            for (byte i=0; i<exp; i++)
            {
                fres *= f;
            }
            return fres;
        }

        public static bool operator ==(Field Fa, Field Fb)
        {
            return (Fa.value == Fb.value);
        }

        public static bool operator !=(Field Fa, Field Fb)
        {
            return !(Fa.value == Fb.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Field F = obj as Field;
            if ((System.Object)F == null)
            {
                return false;
            }
            return (value == F.value);
        }

        public override int GetHashCode()
        {
            return value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        //multiplication method which is only used in Exp & Log table generation
        //implemented with Russian Peasant Multiplication algorithm
        private static byte multiply(byte a, byte b)
        {
            byte result = 0;
            byte aa = a;
            byte bb = b;
            while (bb != 0)
            {
                if ((bb & 1) != 0)
                {
                    result ^= aa;
                }
                byte highest_bit = (byte)(aa & 0x80);
                aa <<= 1;
                if (highest_bit != 0)
                {
                    aa ^= (Polynomial & 0xFF);
                }
                bb >>= 1;
            }
            return result;
        }
    }

    class Share
    {
        private Tuple<Field,Field> t;

        public Share()
        {
            Field x = new Field(0);
            Field y = new Field(0);
            t = new Tuple<Field,Field>(x, y);
        }

        public Share(Field x, Field y)
        {
            t = new Tuple<Field, Field>(x, y);
        }

        //getters and setter
        public Field GetX()
        {
            return t.Item1;
        }

        public Field GetY()
        {
            return t.Item2;
        }

        public void Set(Field x, Field y)
        {
            t = new Tuple<Field, Field>(x, y);
        }
    }

    static class Operation
    {
        public static Share[] GenerateShares(byte k, byte n, byte S)
        //check exception?
        {
            Share[] shares = new Share[n];
            Field[] randPol = GeneratePolynomial(k, S);

            //iterate the shares
            for(byte i=0; i<n; i++)
            {
                Field x = new Field((byte) (i + 1));
                Field y = new Field(0);

                //iterate the coefficients
                for(byte j=0; j<k; j++)
                {
                    y += Field.pow(randPol[j], i);
                }
                shares[i] = new Share(x, y);
            }

            return shares;
        }

        public static Field ReconstructSecret(Share[] shares, byte k)
        {
            Field S = new Field(0);
            //....
            return S;
        }

        //generates coefficients of a random polynomial with degree = k-1 and a0 = S
        private static Field[] GeneratePolynomial(byte k, byte S)
        {
            if (k==0)
            {
                throw new System.ArgumentException("Length cannot be 0", "k");
            }
            Field[] fields = new Field[k];
            fields[0] = new Field(S);

            Random rnd = new Random();
            for (byte i=1; i<k; i++)
            {
                byte current = (byte)rnd.Next(1, Field.Order);
                Field f = new Field(current);
                fields[i] = f;
            }
            return fields;
        }
    }   

    class Program
    {
        static void Main(string[] args)
        {
            //TEST for generatePolynomial
            /*Field[] fields = Operation.generatePolynomial(255,255);
            for(int i=0; i<fields.Length; i++)
            {
                Console.Write(fields[i].getValue()+" ");
            }*/

            //Field f = new Field(5);
            //Console.WriteLine(Field.pow(f,0).GetValue());
            Share[] shares = Operation.GenerateShares(2, 3, 5);
            for(int i=0; i<shares.Length; i++)
            {
                Console.WriteLine(shares[i].GetX() + " " +shares[i].GetY());
            }
            Console.ReadLine();
        }
    }
}
