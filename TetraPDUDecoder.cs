using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


// Exemplo PDU:
// 0DEDA3EEF3C1420208100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
// 0D ED A3 EE F3 C1 42 02 08 10 ... só zeroes

// 2's complement convert calculator: http://www.convertforfree.com/twos-complement-calculator/
// ótima explicação para conversação mas de 1 byte: http://sandbox.mc.edu/~bennet/cs110/tc/tctod.html

namespace TetraPosTraccar
{
    class TetraPDUDecoder
    {
        struct PDU_Report           // como na norma ETSI TS 100 392
        {
            // containeres
            public byte[] PDU_type_b;
            public byte[] Time_Elapsed_b;
            public byte[] Longitude_b;
            public byte[] Latitude_b;
            public byte[] Position_error_b;
            public byte[] Horizontal_velocity_b;
            public byte[] Direction_of_travel_b;
            public byte[] Type_of_addt_data_b;
            public byte[] Reason_of_Send_b;
            public byte[] User_def_data_b;

            // off sets
            public int PDU_type_offset;
            public int Time_Elapsed_offset;
            public int Longitude_offset;
            public int Latitude_offset;
            public int Position_error_offset;
            public int Horizontal_velocity_offset;
            public int Direction_of_travel_offset;
            public int Type_of_addt_data_offset;
            public int Reason_of_Send_offset;
            public int User_def_data_offset;

            // results formato string mais adequado
            string PDU_type;
            string Time_Elapsed;
            string Longitude;
            string Latitude;
            string Position_error;
            string Horizontal_velocity;
            string Direction_of_travel;
            string Type_of_addt_data;
            string Reason_of_Send;
            string User_def_data;
        }

        PDU_Report pduReport = new PDU_Report();

        double LongitudeCoeficiente = (360 / Math.Pow(2, 25));
        double LatitudeCoeficiente = (180 / Math.Pow(2, 24));
        double HorizontalVelocity = 0;

        public bool TetraPDUDecoderInit()
        {
            // off sets
            pduReport.PDU_type_offset = 0;
            pduReport.Time_Elapsed_offset = 2;
            pduReport.Longitude_offset = 4;
            pduReport.Latitude_offset = 29;
            pduReport.Position_error_offset = 53;
            pduReport.Horizontal_velocity_offset = 56;
            pduReport.Direction_of_travel_offset = 63;
            pduReport.Type_of_addt_data_offset = 67;
            pduReport.Reason_of_Send_offset = 68;
            pduReport.User_def_data_offset = 76;


            pduReport.Longitude_b = new byte[4];
            pduReport.Latitude_b = new byte[4];

            return true;
        }

        public List<string> SLRPDUDecoder(string payload, string Ts)
        {
            double Longitude;
            double Latitude;
            int Pos_Error;
            double Direction_Travel; 

            List<string> result = new List<string>();

            // Gets a NumberFormatInfo associated with the en-US culture.
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

            byte[] payload_b = StringToByteArrayFastest(payload);

            Longitude = Decode_Longitude(payload_b);

            Latitude = Decode_Latitude(payload_b);

            Pos_Error = DecodePositionError(payload_b);

            HorizontalVelocity = Decode_HorizontalVelocity(payload_b);

            Direction_Travel = Decode_Direction_Travel(payload_b);

            result.Add(Latitude.ToString(nfi) );
            result.Add(Longitude.ToString( nfi));
            result.Add(HorizontalVelocity.ToString("N", nfi));
            result.Add(Direction_Travel.ToString("N", nfi));
            result.Add(Pos_Error.ToString(/*"N", nfi8*/));

            Debug.Print(Ts + "\t" + Latitude.ToString() + "\t" + Longitude.ToString() + "\t" + Pos_Error.ToString() + "\t" + HorizontalVelocity.ToString() + "\t" + Direction_Travel.ToString());

            return result;

        }

        private double Decode_Direction_Travel(byte[] payload_b)
        {
            double direc;
            int drt;

            try
            {
                byte[] temp = new byte[4];

                temp[0] = payload_b[7];
                temp[1] = payload_b[8];

                temp[0] &= 0x80;
                temp[1] &= 0x70;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(temp);

                drt = BitConverter.ToInt32(temp, 0);
                drt >>= 5;

                switch (drt)
                {
                    case 0:
                        direc = 0;      // North (N)
                        break;
                    case 1:
                        direc = 22.5;   // NNE
                        break;
                    case 2:
                        direc = 45;     // NE
                        break;
                    case 3:
                        direc = 67.5;   // ENE
                        break;
                    case 4:
                        direc = 90;     // East (E)
                        break;
                    case 5:
                        direc = 112.5;  // ESE
                        break;
                    case 6:
                        direc = 135;    // SE
                        break;
                    case 7:
                        direc = 157.5;  // SSE 
                        break;
                    case 8:
                        direc = 180;    // South (S)
                        break;
                    case 9:
                        direc = 202.5;  // SSW 
                        break;
                    case 10:
                        direc = 225;    // SW
                        break;
                    case 11:
                        direc = 247.5;  // WSW
                        break;
                    case 12:
                        direc = 270;    // West (W)
                        break;
                    case 13:
                        direc = 292.5;  // WNW
                        break;
                    case 14:
                        direc = 315;    // NW
                        break;
                    case 15:
                    default:
                        direc = 337.5; // segundo o manual 337.5 pode ser = direction unknown
                        break;
                }

                return direc;
            }
            catch (Exception dw)
            {

                return 0;
            }
            
        }

        private double Decode_HorizontalVelocity(byte[] payload_b)
        {
            double hv= 0;
            double C = 16;
            double x = 0.038;
            double A = 13;
            int K = 0;

            byte[] temp = new byte[1];

            temp[0] = payload_b[7];

            K = temp[0] & 0x7F;        // reseta só o 7o bit
            K >>= 1;

            // Obtém velocidade conforme pág 56 manual ETSI 100 392-18-1
            if (K < 29)
                hv = K;
            else if (K == 29)
                hv = 29.1;
            else if (K > 29 && K < 124)
                hv = C * (Math.Pow(1 + x, K - A)) + 0;
            else if (K == 124)
                hv = 1005;
            else if (K == 125)
                hv = 1043;
            else if (K == 126)
                hv = 1044;          // segundo o manual "h speedy more than 1043km/h"
            else if (K == 127)      // segundo o manual "h speed is not known"
                hv = 1079252848.8;  // então = velocidade da luz! :-)
                     
            return Math.Round( hv, 1);
        }

        private int DecodePositionError(byte[] payload_b)
        {
            int rt = 0x07;              // valor já de "Position Error Not Known" caso dê erro

            byte[] temp = new byte[1];  // cuidado ao portar: 32bits

            try
            {
                temp[0] = payload_b[6];
                temp[0] &= 0x07; // era 0x07

                rt = temp[0] ;

            }
            catch (Exception d)
            {
                rt = 0x07;
                Debug.Print("ERRO DecodePosError() " /*+ d.ToString()*/ );
            }

            return rt;
        }

        private double Decode_Longitude(byte[] payload_b)
        {
            byte[] temp = new byte[4];// cuidado ao portar: 32bits
            int rotate = 0;
            double Longitude_dbl = 0;

            try
            {
                // Longitude
                for (int i = 0; i < 4; i++)
                    temp[i] = payload_b[i];

                temp[0] &= 0x0F;
                temp[3] &= 0xF8;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(temp);

                rotate = BitConverter.ToInt32(temp, 0);

                rotate >>= 3;

                if (rotate > 0xFFFFFF) // último bit = 1? então valor negativo e faz complemnto 2, referências: http://sandbox.mc.edu/~bennet/cs110/tc/tctod.html e http://stackoverflow.com/questions/1049722/what-is-2s-complement
                {
                    // Inverte pois útimo bit do segmento (bit 25) é 1
                    int rotate_not = ~rotate;

                    // zera outrou bits que setou
                    rotate_not &= 0x01FFFFFF;

                    rotate_not += 1;

                    Longitude_dbl = (LongitudeCoeficiente * rotate_not) * -1;
                }
                else
                {
                    // Não Inverte pois útimo bit do segmento (bit 25) é 0, ja usa ele direto...
                    Longitude_dbl = (LongitudeCoeficiente * rotate);
                }
            }
            catch (Exception e)
            {
                Debug.Print("ERRO -> Decode_Longitude: "  /* + e.ToString() */ );
            }

            return Math.Round(Longitude_dbl, 6);
        }

        private double Decode_Latitude(byte[] payload_b)
        {
            byte[] temp = new byte[4];// cuidado ao portar: 32bits
            int rotate = 0;
            double Latitude_dbl = 0;

            try
            {
                // Latitude
                for (int i = 3; i < 7; i++)
                    temp[i - 3] = payload_b[i];

                temp[0] &= 0x07;
                temp[3] &= 0xF8;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(temp);

                rotate = BitConverter.ToInt32(temp, 0);

                rotate >>= 3;

                if (rotate > 0x7FFFFF) // último bit = 1? então valor negativo e faz complemnto 2, referências: http://sandbox.mc.edu/~bennet/cs110/tc/tctod.html e http://stackoverflow.com/questions/1049722/what-is-2s-complement
                {
                    // Inverte pois útimo bit do segmento (bit 25) é 1
                    int rotate_not = ~rotate;

                    // zera outrou bits que setou
                    rotate_not &= 0x7FFFFF;

                    rotate_not += 1;

                    Latitude_dbl = (LatitudeCoeficiente * rotate_not) * -1;
                }
                else
                {
                    // Não Inverte pois útimo bit do segmento (bit 24) é 0, ja usa ele direto...
                    Latitude_dbl = (LatitudeCoeficiente * rotate);
                }

            }
            catch (Exception e)
            {
                Debug.Print("ERRO -> Decode_Longitude: " /* + e.ToString() */ );
            }

            return Math.Round(Latitude_dbl, 6);
        }

        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
            {
                Debug.Print("ERRO: numero impar bytes entrada ?");
                byte[] newc = new byte[10];
                return newc;
            }
            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
        
    }
}

