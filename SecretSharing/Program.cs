﻿using System;
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

        public Share(Share S)
        {
            t = new Tuple<Field, Field>(S.GetX(), S.GetY());
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
        private static Random rnd = new Random();

        //generates n shares with reconstruction threshold = k and secret = S
        public static Share[] GenerateShares(byte k, byte n, byte S)
        {
            if(k==0 || n==0)
            {
                throw new System.ArgumentException("k and n cannot be 0", "k and n");
            }
            if(k>n)
            {
                throw new System.ArgumentException("k must be less or equal than n", "k and n");
            }

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
                    y += (randPol[j] * Field.pow((Field)(i+1), j));
                }
                shares[i] = new Share(x, y);
            }

            return shares;
        }

        //reconstruct secret from first "k" shares from array "shares"
        //using Lagrange Polynomial
        public static Field ReconstructSecret(Share[] shares, byte k)
        {
            if(shares.Length==0 || k==0)
            {
                throw new System.ArgumentException("Shares cannot be empty and k cannot be 0", "shares and k");
            }
            if(shares.Length<k)
            {
                throw new System.ArgumentException("Shares size cannot be less than k","shares and k");
            }

            Field S = new Field(0);
            for(byte i=0; i<k; i++)
            {
                Field CurShare = shares[i].GetY();
                for(byte j=0; j<k; j++)
                {
                    if(j!=i)
                    {
                        CurShare *= (shares[j].GetX() / (shares[j].GetX() - shares[i].GetX()));
                    }
                }
                S += CurShare;
            }
            return S;
        }

        //TODO
        //public static void UpdateShare()

        //generates coefficients of a random polynomial with degree = k-1 and a0 = S
        //private static Field[] GeneratePolynomial(byte k, byte S)
        public static Field[] GeneratePolynomial(byte k, byte S)
        {
            if (k==0)
            {
                throw new System.ArgumentException("Length cannot be 0", "k");
            }
            Field[] fields = new Field[k];
            fields[0] = new Field(S);

            for (byte i=1; i<k; i++)
            {
                byte current = (byte)rnd.Next(1, Field.Order);
                Field f = new Field(current);
                fields[i] = f;
            }
            return fields;
        }

        //generate subshares from a player. XArr is array of abscissa (x) of all players and k is the threshold number
        //private static Field[] GenerateSubshares(Field[] XArr, byte k)
        public static Field[] GenerateSubshares(Field[] XArr, byte k)
        {
            if((XArr.Length==0) || (k==0))
            {
                throw new System.ArgumentException("Array of players cannot be empty and k cannot be 0", "XArr and k");
            }
            if (XArr.Length < k)
            {
                throw new System.ArgumentException("Array of players' size cannot be less than k", "XArr and k");
            }
            Field[] RandPol = GeneratePolynomial(k, 0);
            Field[] subshares = new Field[XArr.Length];
            for(byte i=0; i<subshares.Length; i++)
            {
                Field curTotal = new Field(0);
                for(byte j=0; j<RandPol.Length; j++)
                {
                    curTotal += RandPol[j] * Field.pow(XArr[i], j);
                }
                subshares[i] = curTotal;
            }   
            return subshares;
        }

        //generate new share for a player according to his/her old share (CurShare) and list of subshare (subshares) from other players
        //private static Share GenerateNewShare(Share CurShare, Field[] subshares)
        public static Share GenerateNewShare(Share CurShare, Field[] subshares)
        {
            if(subshares.Length ==0)
            {
                throw new System.ArgumentException("Array subshares cannot be empty", "subshares");
            }
            Field NewX = CurShare.GetX();
            Field NewY = CurShare.GetY();
            for(byte i=0; i<subshares.Length; i++)
            {
                NewY += subshares[i];
            }
            Share NewShare = new Share(NewX, NewY);
            return NewShare;
        }
    }   

    class Program
    {
        static void Main(string[] args)
        {
            //TEST for GenerateShares
            /*Share[] shares = Operation.GenerateShares(3, 5, 17);
            for(int i=0; i<shares.Length; i++)
            {
                Console.WriteLine(shares[i].GetX() + " " +shares[i].GetY());
            }*/

            //TESTING FOR SHARE UPDATING
            //TEST for GenerateSubshares
            Field[] xs = new Field[5] { new Field(1), new Field(2), new Field(3), new Field(4), new Field(5) };
            Share[] shares = new Share[5] { new Share((Field)1, (Field)54), new Share((Field)2, (Field)91), new Share((Field)3, (Field)124), new Share((Field)4, (Field)149), new Share((Field)5, (Field)178)}; 
            Field[][] subs = new Field[5][];
            for(int i=0; i<5; i++)
            {
                subs[i] = Operation.GenerateSubshares(xs, 3);
            }

            for (int i = 0; i < 5; i++)
            {
                Console.Write((i + 1) + " : ");
                for (byte j = 0; j < 5; j++)
                {
                    Console.Write(subs[i][j] + " ");
                }
                Console.WriteLine();
            }

            //Collecting subshares
            Field[][] mysubs = new Field[5][];
            for(byte i=0; i<5; i++)
            {
                mysubs[i] = new Field[5];
                for (byte j=0; j<5; j++)
                {
                    mysubs[i][j] = subs[j][i];
                }
            }

            Console.WriteLine();
            Console.WriteLine("mysubs");
            for (int i = 0; i < 5; i++)
            {
                Console.Write((i + 1) + " : ");
                for (byte j = 0; j < 5; j++)
                {
                    Console.Write(mysubs[i][j] + " ");
                }
                Console.WriteLine();
            }

            //TEST for GenerateNewShare
            Share[] NewShares = new Share[5];
            for(int i=0; i<5; i++)
            {
                NewShares[i] = Operation.GenerateNewShare(shares[i], mysubs[i]);
            }

            Console.WriteLine();
            Console.WriteLine("Old Secret : "+ Operation.ReconstructSecret(shares, 3));
            Console.WriteLine("New Secret : "+ Operation.ReconstructSecret(NewShares, 3));
            Console.ReadLine();
        }
    }
}
