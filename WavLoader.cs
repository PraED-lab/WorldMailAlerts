using System;
using System.IO;

namespace MailCountdown
{
    // Lecture de fichiers WAV (PCM 8/16/24/32 bits et float 32 bits, mono ou stereo) / WAV reader
    public static class WavLoader
    {
        public const long MaxBytes = 20L * 1024 * 1024;
        public const float MaxSeconds = 30f;

        public static bool TryLoad(string path, out float[] data, out int channels, out int rate, out string err)
        {
            data = null; channels = 0; rate = 0; err = null;
            try
            {
                var fi = new FileInfo(path);
                if (fi.Length > MaxBytes) { err = "fichier trop gros / file too big (max 20 MB)"; return false; }
                byte[] b = File.ReadAllBytes(path);
                if (b.Length < 44 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F' || b[8] != 'W' || b[9] != 'A' || b[10] != 'V' || b[11] != 'E')
                { err = "ce n'est pas un WAV / not a WAV file"; return false; }
                int fmtTag = 0, bits = 0, dataPos = -1, dataLen = 0;
                int pos = 12;
                while (pos + 8 <= b.Length)
                {
                    string id = "" + (char)b[pos] + (char)b[pos + 1] + (char)b[pos + 2] + (char)b[pos + 3];
                    int len = BitConverter.ToInt32(b, pos + 4);
                    int body = pos + 8;
                    if (id == "fmt " && body + 16 <= b.Length)
                    {
                        fmtTag = BitConverter.ToUInt16(b, body);
                        channels = BitConverter.ToUInt16(b, body + 2);
                        rate = BitConverter.ToInt32(b, body + 4);
                        bits = BitConverter.ToUInt16(b, body + 14);
                        if (fmtTag == 0xFFFE && len >= 26 && body + 26 <= b.Length) fmtTag = BitConverter.ToUInt16(b, body + 24);
                    }
                    else if (id == "data")
                    {
                        dataPos = body;
                        dataLen = (len < 0 || body + (long)len > b.Length) ? b.Length - body : len;
                        break;
                    }
                    if (len < 0) break;
                    long next = (long)body + len + (len & 1);
                    if (next > b.Length) break;
                    pos = (int)next;
                }
                if (dataPos < 0 || channels < 1 || rate < 8000) { err = "WAV illisible / unreadable WAV"; return false; }
                if (channels > 2) { err = "mono ou stereo seulement / mono or stereo only"; return false; }
                bool isFloat = fmtTag == 3;
                if (!(fmtTag == 1 || isFloat)) { err = "WAV compresse non gere, exporte en PCM / compressed WAV not supported, export as PCM"; return false; }
                int bps = bits / 8;
                if (!((!isFloat && (bits == 8 || bits == 16 || bits == 24 || bits == 32)) || (isFloat && bits == 32))) { err = bits + " bits non gere / unsupported"; return false; }
                int frames = dataLen / (bps * channels);
                if (frames <= 0) { err = "WAV vide / empty WAV"; return false; }
                if (frames / (float)rate > MaxSeconds) { err = "trop long / too long (max " + MaxSeconds + " s)"; return false; }
                int total = frames * channels;
                data = new float[total];
                for (int i = 0; i < total; i++)
                {
                    int o = dataPos + i * bps;
                    float v;
                    if (isFloat) v = BitConverter.ToSingle(b, o);
                    else if (bits == 8) v = (b[o] - 128) / 128f;
                    else if (bits == 16) v = BitConverter.ToInt16(b, o) / 32768f;
                    else if (bits == 24) v = (((b[o + 2] << 24) | (b[o + 1] << 16) | (b[o] << 8)) >> 8) / 8388608f;
                    else v = BitConverter.ToInt32(b, o) / 2147483648f;
                    if (v > 1f) v = 1f; else if (v < -1f) v = -1f;
                    data[i] = v;
                }
                return true;
            }
            catch (Exception e) { err = e.Message; return false; }
        }
    }
}
