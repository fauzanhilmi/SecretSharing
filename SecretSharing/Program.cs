﻿using System;
using System.Collections.Generic;
using System.IO;
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
        //S is an integer in the range of 0 to 255
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

        //generates n files as shares (their locations are the return value) with reconstruction threshold = k and secret = S
        //SLocation is the location to the file that is given as the secret. The file's size must not exceed 2^32 bytes (~4.2 GB)
        ////TODO : Reformat share names
        public static string[] GenerateShares(byte k, byte n, string SLocation)
        {
            if (k == 0 || n == 0)
            {
                throw new System.ArgumentException("k and n cannot be 0", "k and n");
            }
            if (k > n)
            {
                throw new System.ArgumentException("k must be less or equal than n", "k and n");
            }

            string[] FileShareNames = new string[n];
            try
            {
                using (FileStream fs = new FileStream(SLocation, FileMode.Open, FileAccess.Read))
                {
                    byte[] FileArr = new byte[fs.Length];
                    int bytesLeft = (int)fs.Length;
                    int bytesRead = 0;
                    while(bytesLeft > 0)
                    {
                        int res = fs.Read(FileArr, bytesRead, bytesLeft);
                        if (res == 0)
                            break;
                        bytesRead += res;
                        bytesLeft -= res;
                    }

                    byte[][] byteShares = new byte[n][];
                    for (byte i = 0; i < n; i++)
                    {
                        byteShares[i] = new byte[FileArr.Length + 1];
                    }
                    
                    //fill byteShares, array of array of share
                    //byteShares[j][i] = share no. i-1 of player j
                    //byteshares[j][0] is reserved to store the absissca (X value) of player j
                    for (int i = 0; i < FileArr.Length; i++)
                    {
                        Share[] CurShares = GenerateShares(k, n, FileArr[i]);
                        for (byte j = 0; j < n; j++)
                        {
                            if (i == 0)
                            {
                                byteShares[j][0] = (byte)CurShares[j].GetX();
                            }
                            byteShares[j][i + 1] = (byte)CurShares[j].GetY();
                        }
                    }

                    //writing share files
                    for (byte i = 0; i < n; i++)
                    {
                        string ShareFileName = "output" + (i + 1) + ".share";
                        using (FileStream fsWrite = new FileStream(ShareFileName, FileMode.Create, FileAccess.Write))
                        {
                            fsWrite.Write(byteShares[i], 0, byteShares[i].Length);
                        }
                    }
                    
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return FileShareNames;
        }

        //reconstruct secret from first "k" shares from array of share (shares)
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

        //reconstruct secret from first "k" share files from the array of file locations (ShareFilesNames)
        //then writes the secret into SecretFileLocation
        ////TODO : Filter only .share files?
        public static void ReconstructSecret(string[] ShareFilesLocations, byte k, string SecretFileLocation)
        {
            if (ShareFilesLocations.Length == 0 || k == 0)
            {
                throw new System.ArgumentException("Share file locations cannot be empty and k cannot be 0", "ShareFilesNames and k");
            }
            if (ShareFilesLocations.Length < k)
            {
                throw new System.ArgumentException("The number of Share files cannot be less than k", "ShareFilesNames and k");
            }

            try
            {
                //byte[][] ShareBytes = new byte[k][];
                Share[][] Shares = new Share[k][];

                for(int i=0; i<k; i++)
                {
                    using (FileStream fs = new FileStream(ShareFilesLocations[i], FileMode.Open, FileAccess.Read))
                    {
                        byte X = (byte)i; //default value
                        Share[] CurShares = new Share[fs.Length - 1];

                        //Reading file with FileStream.Read --> too complex, cannot read only one byte at a time
                        /*int bytesLeft = (int)fs.Length;
                        int bytesRead = 0;
                        int it = 0;

                        while (bytesLeft > 0)
                        {
                            byte[] TempByte = new byte[1];

                            int res = fs.Read(TempByte, bytesRead, bytesLeft);
                            //getting the x value in front of the file
                            if (bytesRead == 0)
                            {                                
                                X = TempByte[0];
                            }
                            //convert the rest of file to Share
                            else
                            {
                                CurShares[it] = new Share((Field)X, (Field)TempByte[0]);
                                it++;
                            }
                            if (res == 0)
                                break;
                            bytesRead += res;
                            bytesLeft -= res;
                        }*/

                        //Reading file with FileStream.Readbyte
                        for (int j=0; j<fs.Length; j++)
                        {
                            //getting the x value in front of the file
                            if (j == 0)
                            {
                                X = (byte) fs.ReadByte();
                            }
                            //convert the rest of file to Share
                            else
                            {
                                CurShares[j-1] = new Share((Field) X, (Field) fs.ReadByte());
                            }
                        }
                        Shares[i] = CurShares;
                    }
                }

                //reconstruction process
                Field[] Secret = new Field[Shares[0].Length];
                byte[] SecretBytes = new byte[Secret.Length];
                for (int i = 0; i < Shares[0].Length; i++)
                {
                    Share[] CurShares = new Share[k];
                    for(int j=0; j<k; j++)
                    {
                        CurShares[j] = Shares[j][i];
                    }
                    Secret[i] = ReconstructSecret(CurShares, k);
                    SecretBytes[i] = (byte) Secret[i];
                }

                //writing secret to file
                using (FileStream fsWrite = new FileStream(SecretFileLocation, FileMode.Create, FileAccess.Write))
                {
                    fsWrite.Write(SecretBytes, 0, SecretBytes.Length);
                }

            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
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
            //string[] tests = Operation.GenerateShares(255, 255, "input.png");

            //TEST for ReconstructSecret
            /*string[] sharefiles = new string[255];
            for(int i=0; i<255; i++)
            {
                sharefiles[i] = "output" + (i+1) + ".share";
            }
            Operation.ReconstructSecret(sharefiles, 255, "output.png");*/
            //string[] sharefiles = new string [17]{ "output2.share", "output4.share", "output5.share", "output7.share", "output12.share", "output27.share", "output30.share", "output43.share", "output44.share", "output59.share", "output68.share", "output71.share", "output72.share", "output73.share", "output77.share", "output85.share", "output99.share" };
            //string[] sharefiles = new string[1] { "output165.share" };//, "output2.share", "output99.share" };//, "output5.share", "output1.share" };
            //Operation.ReconstructSecret(sharefiles, 1, "output.png");
            //Console.ReadLine();
        }
    }
}
