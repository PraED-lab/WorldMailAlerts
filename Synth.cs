using System;
using System.Collections.Generic;

namespace MailCountdown
{
    // Sons agreables generes par le code (synthese additive, aucun fichier) / pleasant generated sounds (additive synthesis, no files)
    public static class Synth
    {
        public const int SR = 44100;
        public static readonly string[] Names = {
            "None", "Owl double hoot", "Owl deep", "Owl night", "Owl screech", "Frog croak deep", "Frog chorus", "Tree frog", "Frog bubble", "Wolf howl long", "Wolf short howl", "Wolf pack", "Wolf distant", "Wolf growl", "Wolf and owl", "Bandage rip", "Zipper", "Pill bottle", "Gas mask", "Energy drink", "Campfire", "Ocean wave", "Door creak", "Helicopter", "Lightning", "Psi storm", "Radio static", "Metal steps", "Bolt throw", "Vodka clink", "Pistol shot", "Machine gun", "Sniper shot", "Suppressed shot", "Explosion", "AK reload", "Magazine click", "Bolt action", "Shell casing", "Grenade pin", "Stim inject", "Sword draw", "Electro anomaly", "Key tock", "Unlock click", "Xylophone", "Glissando", "Retro beep", "Pop", "Water drop", "Drum roll", "Doorbell", "Wind chimes", "Soft keys", "Piano", "Birdsong", "Quack", "Boing", "Whistle", "Cat sleepy mew", "Cat angry mew", "Cat call", "Kitten chirps", "Purr and mew", "Dog bark", "Dog double bark", "Puppy yip", "Wolf howl", "Owl hoot", "Frog ribbit", "Rooster", "Sheep", "Cricket", "Crow caw", "Dolphin", "Cat prrt", "Cat double prrt", "Cat chirp", "Cat mew", "Kitten mew", "Cat trill up", "Cat greeting", "Purr", "Purr deep", "Purr soft", "Purr and prrt", "Kitten purr", "Soft ping", "Bell", "Chime", "Marimba", "Harp", "Crystal", "Arpeggio",
            "Coin", "Level up", "Cash register", "Sonar", "Music box", "Fanfare", "Laser", "Sci-fi",
            "Morse SOS", "Heartbeat", "Gong", "Sad trombone", "Old phone", "Radio squelch", "Radio call", "Extract beeps",
            "Geiger", "Tinnitus", "Task complete", "Flashbang", "Keycard OK", "Keycard denied", "Klaxon", "Intercom",
            "Power up", "Sparkle", "Cuckoo", "Knock knock", "Camera shutter", "Slot machine", "Generator", "Trader message",
            "Raid horn", "Armor hit", "Epic boom", "Cinema braam", "Power chord", "Bass drop", "Taiko drums", "Mech boot-up",
            "Boss warning", "Marimba up", "Marimba down", "Marimba bounce", "Vibraphone", "Kalimba", "Steel drum", "Coin x2",
            "Coin cascade", "Coin drop", "Coin power-up", "Sad tuba", "Womp womp", "Sad violin", "Wrong buzzer", "Falling whistle",
            "Trombone slide", "Bullet on armor", "Armor ricochet", "Ceramic plate", "Armor tank", "Trader ping x3", "Trader deal", "Incoming message",
            "Encrypted message", "Radio trader", "Secure line", "Mail drop", "Black market", "Urgent message", "Hacked message", "Kick ping",
            "Fire bell", "Smoke detector", "Alarm clock", "Car alarm", "Air raid siren", "Burglar alarm", "Lab alarm", "Lockdown",
            "Evacuation horn", "Countdown alarm", "Radiation alert", "Submarine dive", "Siren deep", "Double siren", "Siren rise", "Siren pulse",
            "Wartime siren", "Siren far", "Black market deep", "Black market door", "Black market radio", "Shadow deal", "Dark ping", "Underground",
            "Decrypting", "Encrypted burst", "Data transfer", "Access granted", "Signal lock", "Cipher", "Blowout siren", "Anomaly detector",
            "Artifact found", "PDA message", "PDA ping", "Numbers station", "Guitar strum", "Singing bowl", "Zen bell", "Cotton ping",
            "Soft piano", "Dream pad", "Lullaby", "Rain drops", "Bubbles", "Wind bells soft", "Harp lullaby", "Celesta",
            "Feather chime", "Soft heartbeat", "Triple chime", "Glass tap", "Swoosh", "Plug-in chime", "Success tick", "Beam",
            "Listening blip", "Bloom", "Low battery", "Radar pulse", "Glass chord", "Ding" };
        static Random rng = new Random(1234);

        // ratios des partiels, amplitudes, vitesse de decroissance / partial ratios, amplitudes, decay rate
        static readonly float[] BellR = { 1f, 2.0f, 2.76f, 5.4f }, BellA = { 1f, 0.45f, 0.22f, 0.08f };
        static readonly float[] MarR = { 1f, 4f, 10f }, MarA = { 1f, 0.28f, 0.06f };
        static readonly float[] HarpR = { 1f, 2f, 3f, 4f, 5f }, HarpA = { 1f, 0.5f, 0.28f, 0.14f, 0.07f };
        static readonly float[] CryR = { 1f, 2.01f, 3.02f, 4.97f }, CryA = { 1f, 0.35f, 0.2f, 0.1f };

        static void Note(float[] buf, double start, float hz, float dur, float amp, float[] R, float[] A, float decay)
        {
            int s0 = (int)(start * SR);
            int n = (int)(dur * SR);
            for (int p = 0; p < R.Length; p++)
            {
                float f = hz * R[p];
                if (f > 9000f) continue;
                float pd = decay * (1f + p * 0.9f);
                double w = 2.0 * Math.PI * f / SR;
                for (int i = 0; i < n; i++)
                {
                    int k = s0 + i;
                    if (k >= buf.Length) break;
                    float t = (float)i / SR;
                    float env = (float)Math.Exp(-t * pd) * Math.Min(1f, i / (SR * 0.006f));
                    buf[k] += (float)Math.Sin(w * i) * A[p] * amp * env;
                }
            }
        }

        static void Bell(float[] b, double t, float hz, float dur, float amp) { Note(b, t, hz, dur, amp, BellR, BellA, 3.2f); }
        static void Mar(float[] b, double t, float hz, float amp) { Note(b, t, hz, 0.5f, amp, MarR, MarA, 9f); }
        static void Harp(float[] b, double t, float hz, float amp) { Note(b, t, hz, 1.1f, amp, HarpR, HarpA, 4.2f); }
        static void Cry(float[] b, double t, float hz, float amp) { Note(b, t, hz, 1.0f, amp, CryR, CryA, 4.5f); }


        // oscillateur : 0 sinus, 1 carre doux, 2 dent de scie, 3 triangle / oscillator: 0 sine, 1 soft square, 2 saw, 3 triangle
        static void Osc(float[] buf, double start, float f0, float f1, float dur, float amp, int wave, float atk, float dec, float vibHz = 0f, float vibDepth = 0f)
        {
            int s0 = (int)(start * SR), n = (int)(dur * SR);
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                int k = s0 + i; if (k >= buf.Length) break; if (k < 0) continue;
                float t = (float)i / SR, u = (float)i / n;
                float f = f0 + (f1 - f0) * u;
                if (vibHz > 0f) f *= 1f + vibDepth * (float)Math.Sin(2.0 * Math.PI * vibHz * t);
                ph += 2.0 * Math.PI * f / SR;
                float v;
                switch (wave)
                {
                    case 1: v = (float)Math.Tanh(3.0 * Math.Sin(ph)) * 0.7f; break;
                    case 2: { double a = 0; for (int h = 1; h <= 10; h++) { if (f * h > 10000f) break; a += Math.Sin(h * ph) / h; } v = (float)(a * 0.55); break; }
                    case 3: v = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(ph))); break;
                    default: v = (float)Math.Sin(ph); break;
                }
                float env = Math.Min(1f, i / (SR * Math.Max(atk, 0.002f))) * (float)Math.Exp(-t * dec) * Math.Min(1f, (n - i) / (SR * 0.015f));
                buf[k] += v * amp * env;
            }
        }

        // bruit filtre / filtered noise (hp = 0..1, plus haut = plus aigu)
        static void Noise(float[] buf, double start, float dur, float amp, float dec, float hp)
        {
            int s0 = (int)(start * SR), n = (int)(dur * SR);
            float lp = 0f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                int k = s0 + i; if (k >= buf.Length) break;
                float w = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (w - lp) * 0.5f;
                float hpv = lp - prev * (1f - hp); prev = lp;
                float t = (float)i / SR;
                buf[k] += hpv * amp * (float)Math.Exp(-t * dec) * Math.Min(1f, (n - i) / (SR * 0.01f));
            }
        }

        static readonly float[] GongR = { 1f, 1.47f, 2.09f, 2.56f, 3.4f }, GongA = { 1f, 0.7f, 0.5f, 0.35f, 0.2f };
        static readonly float[] PiaR = { 1f, 2f, 3f, 4f, 6f }, PiaA = { 1f, 0.6f, 0.3f, 0.2f, 0.08f };
        static readonly float[] BoxR = { 1f, 3f, 6.1f }, BoxA = { 1f, 0.3f, 0.12f };


        // distorsion (guitare, basses) : on rend dans un tampon puis on sature / distortion: render into a buffer, then saturate
        static void AddDriven(float[] dst, float[] tmp, float drive, float amp)
        {
            for (int i = 0; i < dst.Length && i < tmp.Length; i++) dst[i] += (float)Math.Tanh(tmp[i] * drive) * amp;
        }


        static void Coin(float[] b, double t, float scale, float tail)
        {
            Osc(b, t, 987.8f * scale, 987.8f * scale, 0.09f, 0.6f, 1, 0.003f, 2f);
            Osc(b, t + 0.09, 1318.5f * scale, 1318.5f * scale, tail, 0.6f, 1, 0.003f, 5f);
        }


        // impact d'une balle sur une plaque / a bullet hitting a plate
        static void ArmorHit(float[] b, double t, float f, float a)
        {
            Noise(b, t, 0.012f, 0.8f * a, 150f, 0.95f);
            Osc(b, t, 150f, 70f, 0.12f, 0.9f * a, 0, 0.001f, 20f);
            Note(b, t, f, 0.5f, 0.8f * a, new[] { 1f, 2.76f, 5.4f, 8.9f }, new[] { 1f, 0.6f, 0.35f, 0.2f }, 14f);
            Note(b, t, f * 1.55f, 0.35f, 0.35f * a, new[] { 1f, 2.3f }, new[] { 1f, 0.5f }, 18f);
            Noise(b, t, 0.08f, 0.35f * a, 32f, 0.2f);
        }


        // le "pip-pop" de la messagerie du marchand / the trader messenger "pip-pop"
        static void Ping(float[] b, double t, float f1, float f2, float a)
        {
            Osc(b, t, f1, f1, 0.08f, a, 0, 0.003f, 10f); Osc(b, t, f1 * 2f, f1 * 2f, 0.06f, a * 0.15f, 0, 0.003f, 14f);
            Osc(b, t + 0.09, f2, f2, 0.14f, a, 0, 0.003f, 9f); Osc(b, t + 0.09, f2 * 2f, f2 * 2f, 0.1f, a * 0.15f, 0, 0.003f, 12f);
        }


        // corde pincee (Karplus-Strong) / plucked string (Karplus-Strong)
        static void Pluck(float[] buf, double start, float hz, float dur, float amp)
        {
            int N = Math.Max(2, (int)(SR / hz));
            var line = new float[N];
            for (int i = 0; i < N; i++) line[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            int s0 = (int)(start * SR), n = (int)(dur * SR), p = 0;
            for (int i = 0; i < n; i++)
            {
                int k = s0 + i; if (k >= buf.Length) break;
                int q = (p + 1) % N;
                float v = line[p];
                line[p] = (line[p] + line[q]) * 0.4975f;
                p = q;
                buf[k] += v * amp * Math.Min(1f, i / (SR * 0.002f)) * Math.Min(1f, (n - i) / (SR * 0.05f));
            }
        }


        static readonly float[] GlR = { 1f, 2.0f, 3.0f, 4.2f }, GlA = { 1f, 0.35f, 0.15f, 0.06f };
        static void Gl(float[] b, double t, float hz, float dur, float amp) { Note(b, t, hz, dur, amp, GlR, GlA, 6f); }

        // voix de chat : onde riche en harmoniques, balayage de hauteur, roulement (AM) / cat voice: harmonic-rich tone, pitch sweep, trill (AM)
        static void CatVoice(float[] buf, double start, float dur, float amp, float fa, float fb, float fc, float trillHz, float trillDepth, float vib = 0f)
        {
            int s0 = (int)(start * SR), n = (int)(dur * SR);
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                int k = s0 + i; if (k >= buf.Length) break;
                float u = (float)i / n, t = (float)i / SR;
                float f = u < 0.5f ? fa + (fb - fa) * (u * 2f) : fb + (fc - fb) * ((u - 0.5f) * 2f);
                if (vib > 0f) f *= 1f + 0.03f * (float)Math.Sin(2.0 * Math.PI * vib * t);
                ph += 2.0 * Math.PI * f / SR;
                float v = (float)(Math.Sin(ph) + 0.55 * Math.Sin(2 * ph) + 0.3 * Math.Sin(3 * ph) + 0.15 * Math.Sin(4 * ph));
                float am = 1f;
                if (trillHz > 0f) am = 1f - trillDepth + trillDepth * (0.5f + 0.5f * (float)Math.Sin(2.0 * Math.PI * trillHz * t - 1.6));
                float env = (float)Math.Sin(Math.PI * Math.Pow(u, 0.7)) ;
                buf[k] += v * 0.45f * amp * am * env;
            }
        }

        // ronron : impulsions graves ~25 Hz + souffle doux, gonfle puis retombe / purr: ~25 Hz low pulses + soft breath, swells then fades
        static void Purr(float[] buf, double start, float dur, float amp, float rate, float tone)
        {
            int s0 = (int)(start * SR), n = (int)(dur * SR);
            float lp = 0f;
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                int k = s0 + i; if (k >= buf.Length) break;
                float t = (float)i / SR, u = (float)i / n;
                float r = rate * (1f + 0.04f * (float)Math.Sin(2.0 * Math.PI * 0.7 * t));
                ph += 2.0 * Math.PI * r / SR;
                float pulse = (float)Math.Pow(0.5 + 0.5 * Math.Sin(ph), 3.0);
                float w = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (w - lp) * tone;
                float body = (float)(Math.Sin(2.0 * Math.PI * 95.0 * t) * 0.5 + Math.Sin(2.0 * Math.PI * 190.0 * t) * 0.25);
                float env = (float)Math.Sin(Math.PI * Math.Pow(u, 0.6));
                buf[k] += (lp * 2.2f + body * 0.6f) * pulse * amp * env;
            }
        }

        // petites bulles qui petillent / tiny crackling bubbles
        static void Fizz(float[] b, double t0, double t1, int count, float amp, float decayPow)
        {
            for (int i = 0; i < count; i++)
            {
                double u = Math.Pow(rng.NextDouble(), decayPow);
                double t = t0 + (t1 - t0) * u;
                float a = amp * (0.4f + (float)rng.NextDouble() * 0.6f) * (float)(1.0 - 0.7 * u);
                Noise(b, t, 0.003f + (float)rng.NextDouble() * 0.004f, a, 250f, 0.08f);
                if (rng.NextDouble() < 0.35) Osc(b, t, 2500f + (float)rng.NextDouble() * 2500f, 1500f, 0.008f, a * 0.35f, 0, 0.001f, 150f);
            }
        }
        // petite piece metallique (capsule) / small metal piece (cap) ring
        static void MetalPing(float[] b, double t, float hz, float amp)
        {
            Note(b, t, hz, 0.35f, amp, new[] { 1f, 2.76f, 5.4f, 8.9f }, new[] { 1f, 0.6f, 0.35f, 0.15f }, 14f);
            Noise(b, t, 0.003f, amp * 0.5f, 400f, 0.2f);
        }

        // petite reverb : deux echos doux / light reverb: two soft echoes
        static float[] Finish(float[] raw)
        {
            int extra = (int)(SR * 0.35);
            var o = new float[raw.Length + extra];
            Array.Copy(raw, o, raw.Length);
            int[] d = { (int)(SR * 0.083), (int)(SR * 0.131), (int)(SR * 0.197) };
            float[] g = { 0.28f, 0.18f, 0.10f };
            for (int j = 0; j < d.Length; j++)
                for (int i = 0; i + d[j] < o.Length && i < raw.Length; i++) o[i + d[j]] += raw[i] * g[j];
            float peak = 0f;
            for (int i = 0; i < o.Length; i++) { float a = Math.Abs(o[i]); if (a > peak) peak = a; }
            float k = peak > 0f ? 0.85f / peak : 1f;
            for (int i = 0; i < o.Length; i++) o[i] *= k;
            // fondu de fin pour eviter un clic / fade-out to avoid a click
            int fade = (int)(SR * 0.05);
            for (int i = 0; i < fade && i < o.Length; i++) o[o.Length - 1 - i] *= (float)i / fade;
            return o;
        }

        public static float[] Render(string name)
        {
            float len;
            switch (name)
            {
                case "Cat sleepy mew": len = 1.4f; break;
                case "Cat angry mew": len = 1.0f; break;
                case "Cat call": len = 2.0f; break;
                case "Kitten chirps": len = 1.0f; break;
                case "Purr and mew": len = 3.6f; break;
                case "Dog bark": len = 0.6f; break;
                case "Dog double bark": len = 1.0f; break;
                case "Puppy yip": len = 0.7f; break;
                case "Wolf howl": len = 2.6f; break;
                case "Owl hoot": len = 1.4f; break;
                case "Frog ribbit": len = 1.0f; break;
                case "Rooster": len = 1.6f; break;
                case "Sheep": len = 1.2f; break;
                case "Cricket": len = 1.0f; break;
                case "Crow caw": len = 1.2f; break;
                case "Dolphin": len = 1.2f; break;
                case "Owl double hoot": len = 1.6f; break;
                case "Owl deep": len = 1.8f; break;
                case "Owl night": len = 3.0f; break;
                case "Owl screech": len = 1.3f; break;
                case "Frog croak deep": len = 1.2f; break;
                case "Frog chorus": len = 2.5f; break;
                case "Tree frog": len = 1.0f; break;
                case "Frog bubble": len = 1.0f; break;
                case "Wolf howl long": len = 4.0f; break;
                case "Wolf short howl": len = 1.6f; break;
                case "Wolf pack": len = 4.0f; break;
                case "Wolf distant": len = 3.5f; break;
                case "Wolf growl": len = 1.5f; break;
                case "Wolf and owl": len = 4.0f; break;
                case "Beer bottle opening": len = 2.2f; break;
                case "Beer cap drop": len = 1.0f; break;
                case "Can opening": len = 2.2f; break;
                case "Beer cheers": len = 1.6f; break;
                case "Cat prrt": len = 0.7f; break;
                case "Cat double prrt": len = 1.2f; break;
                case "Cat chirp": len = 0.8f; break;
                case "Cat mew": len = 1.2f; break;
                case "Kitten mew": len = 0.9f; break;
                case "Cat trill up": len = 0.9f; break;
                case "Cat greeting": len = 1.5f; break;
                case "Purr": len = 3.0f; break;
                case "Purr deep": len = 3.2f; break;
                case "Purr soft": len = 3.0f; break;
                case "Purr and prrt": len = 3.4f; break;
                case "Kitten purr": len = 2.6f; break;
                case "Soft ping": len = 1.6f; break;
                case "Bell": len = 2.0f; break;
                case "Chime": len = 1.9f; break;
                case "Marimba": len = 1.5f; break;
                case "Harp": len = 1.8f; break;
                case "Crystal": len = 1.8f; break;
                case "Arpeggio": len = 3.0f; break;
                case "Coin": len = 1.2f; break;
                case "Level up": len = 1.6f; break;
                case "Cash register": len = 1.6f; break;
                case "Sonar": len = 2.6f; break;
                case "Doorbell": len = 2.4f; break;
                case "Pop": len = 0.6f; break;
                case "Water drop": len = 1.0f; break;
                case "Xylophone": len = 1.4f; break;
                case "Music box": len = 2.4f; break;
                case "Fanfare": len = 2.0f; break;
                case "Laser": len = 1.0f; break;
                case "Sci-fi": len = 1.0f; break;
                case "Morse SOS": len = 3.4f; break;
                case "Heartbeat": len = 2.6f; break;
                case "Gong": len = 3.6f; break;
                case "Wind chimes": len = 2.6f; break;
                case "Sad trombone": len = 2.6f; break;
                case "Boing": len = 1.0f; break;
                case "Quack": len = 1.0f; break;
                case "Retro beep": len = 1.0f; break;
                case "Drum roll": len = 2.2f; break;
                case "Glissando": len = 2.2f; break;
                case "Piano": len = 3.0f; break;
                case "Old phone": len = 2.4f; break;
                case "Radio squelch": len = 0.7f; break;
                case "Radio call": len = 1.6f; break;
                case "Extract beeps": len = 2.2f; break;
                case "Geiger": len = 2.4f; break;
                case "Shell casing": len = 1.4f; break;
                case "Magazine click": len = 0.7f; break;
                case "Pistol shot": len = 1.6f; break;
                case "Bolt action": len = 1.3f; break;
                case "Tinnitus": len = 2.6f; break;
                case "Helicopter": len = 2.8f; break;
                case "Door creak": len = 1.6f; break;
                case "Task complete": len = 1.6f; break;
                case "Flashbang": len = 2.4f; break;
                case "Grenade pin": len = 1.2f; break;
                case "Suppressed shot": len = 1.2f; break;
                case "AK reload": len = 1.5f; break;
                case "Keycard OK": len = 0.9f; break;
                case "Keycard denied": len = 0.9f; break;
                case "Metal steps": len = 2.2f; break;
                case "Zipper": len = 1.0f; break;
                case "Pill bottle": len = 1.2f; break;
                case "Klaxon": len = 2.2f; break;
                case "Intercom": len = 1.9f; break;
                case "Power up": len = 1.0f; break;
                case "Sparkle": len = 2.0f; break;
                case "Cuckoo": len = 1.6f; break;
                case "Whistle": len = 1.0f; break;
                case "Knock knock": len = 1.0f; break;
                case "Camera shutter": len = 0.6f; break;
                case "Slot machine": len = 2.6f; break;
                case "Stim inject": len = 0.9f; break;
                case "Bandage rip": len = 0.9f; break;
                case "Generator": len = 2.6f; break;
                case "Trader message": len = 0.8f; break;
                case "Raid horn": len = 2.0f; break;
                case "Armor hit": len = 1.1f; break;
                case "Epic boom": len = 2.6f; break;
                case "Cinema braam": len = 2.8f; break;
                case "Power chord": len = 2.2f; break;
                case "Bass drop": len = 1.6f; break;
                case "Sniper shot": len = 2.2f; break;
                case "Machine gun": len = 1.4f; break;
                case "Explosion": len = 2.4f; break;
                case "Taiko drums": len = 2.2f; break;
                case "Lightning": len = 2.4f; break;
                case "Sword draw": len = 1.6f; break;
                case "Mech boot-up": len = 1.8f; break;
                case "Boss warning": len = 2.4f; break;
                case "Marimba up": len = 1.6f; break;
                case "Marimba down": len = 1.6f; break;
                case "Marimba bounce": len = 1.7f; break;
                case "Vibraphone": len = 2.4f; break;
                case "Kalimba": len = 1.6f; break;
                case "Steel drum": len = 2.0f; break;
                case "Coin x2": len = 1.4f; break;
                case "Coin cascade": len = 1.8f; break;
                case "Coin drop": len = 2.0f; break;
                case "Coin power-up": len = 1.2f; break;
                case "Sad tuba": len = 3.0f; break;
                case "Womp womp": len = 2.0f; break;
                case "Sad violin": len = 3.0f; break;
                case "Wrong buzzer": len = 1.2f; break;
                case "Falling whistle": len = 1.8f; break;
                case "Trombone slide": len = 1.8f; break;
                case "Bullet on armor": len = 1.0f; break;
                case "Armor ricochet": len = 1.3f; break;
                case "Ceramic plate": len = 1.0f; break;
                case "Armor tank": len = 1.7f; break;
                case "Trader ping x3": len = 1.0f; break;
                case "Trader deal": len = 1.6f; break;
                case "Incoming message": len = 1.4f; break;
                case "Encrypted message": len = 1.4f; break;
                case "Radio trader": len = 1.3f; break;
                case "Secure line": len = 1.5f; break;
                case "Mail drop": len = 1.3f; break;
                case "Black market": len = 2.0f; break;
                case "Urgent message": len = 1.1f; break;
                case "Hacked message": len = 1.2f; break;
                case "Kick ping": len = 0.8f; break;
                case "Fire bell": len = 1.9f; break;
                case "Smoke detector": len = 1.7f; break;
                case "Alarm clock": len = 1.7f; break;
                case "Car alarm": len = 2.2f; break;
                case "Air raid siren": len = 3.0f; break;
                case "Burglar alarm": len = 1.8f; break;
                case "Lab alarm": len = 2.0f; break;
                case "Lockdown": len = 2.2f; break;
                case "Evacuation horn": len = 2.6f; break;
                case "Countdown alarm": len = 3.2f; break;
                case "Radiation alert": len = 2.4f; break;
                case "Submarine dive": len = 1.8f; break;
                case "Siren deep": len = 3.2f; break;
                case "Double siren": len = 3.4f; break;
                case "Siren rise": len = 3.2f; break;
                case "Siren pulse": len = 2.8f; break;
                case "Wartime siren": len = 3.4f; break;
                case "Siren far": len = 3.6f; break;
                case "Black market deep": len = 2.4f; break;
                case "Black market door": len = 1.8f; break;
                case "Black market radio": len = 1.8f; break;
                case "Shadow deal": len = 2.2f; break;
                case "Dark ping": len = 1.6f; break;
                case "Underground": len = 2.3f; break;
                case "Decrypting": len = 2.4f; break;
                case "Encrypted burst": len = 1.2f; break;
                case "Data transfer": len = 1.6f; break;
                case "Access granted": len = 1.5f; break;
                case "Signal lock": len = 1.8f; break;
                case "Cipher": len = 1.5f; break;
                case "Blowout siren": len = 3.6f; break;
                case "Psi storm": len = 2.6f; break;
                case "Anomaly detector": len = 2.8f; break;
                case "Artifact found": len = 2.2f; break;
                case "Electro anomaly": len = 1.4f; break;
                case "PDA message": len = 0.9f; break;
                case "PDA ping": len = 0.6f; break;
                case "Radio static": len = 1.8f; break;
                case "Numbers station": len = 3.0f; break;
                case "Bolt throw": len = 1.2f; break;
                case "Campfire": len = 2.8f; break;
                case "Guitar strum": len = 2.6f; break;
                case "Vodka clink": len = 1.4f; break;
                case "Gas mask": len = 3.4f; break;
                case "Energy drink": len = 1.8f; break;
                case "Singing bowl": len = 4.0f; break;
                case "Zen bell": len = 3.0f; break;
                case "Cotton ping": len = 1.6f; break;
                case "Soft piano": len = 3.0f; break;
                case "Dream pad": len = 3.4f; break;
                case "Lullaby": len = 3.2f; break;
                case "Rain drops": len = 2.8f; break;
                case "Bubbles": len = 1.6f; break;
                case "Wind bells soft": len = 3.4f; break;
                case "Harp lullaby": len = 2.8f; break;
                case "Celesta": len = 2.4f; break;
                case "Feather chime": len = 1.8f; break;
                case "Soft heartbeat": len = 2.6f; break;
                case "Ocean wave": len = 3.2f; break;
                case "Birdsong": len = 2.4f; break;
                case "Soft keys": len = 3.2f; break;
                case "Triple chime": len = 1.6f; break;
                case "Glass tap": len = 1.0f; break;
                case "Swoosh": len = 0.9f; break;
                case "Key tock": len = 0.5f; break;
                case "Plug-in chime": len = 1.4f; break;
                case "Unlock click": len = 0.7f; break;
                case "Success tick": len = 0.9f; break;
                case "Beam": len = 1.2f; break;
                case "Listening blip": len = 0.7f; break;
                case "Bloom": len = 2.2f; break;
                case "Low battery": len = 1.8f; break;
                case "Radar pulse": len = 2.4f; break;
                case "Glass chord": len = 1.8f; break;
                case "Ding": len = 1.2f; break;
                default: return null;
            }
            rng = new Random(1234);
            var b = new float[(int)(SR * len)];
            switch (name)
            {
                case "Soft ping": Bell(b, 0.0, 1318.5f, 1.2f, 0.7f); Bell(b, 0.14, 1975.5f, 1.2f, 0.5f); break;
                case "Bell": Bell(b, 0.0, 1046.5f, 1.8f, 0.8f); break;
                case "Chime": Bell(b, 0.0, 784f, 1.5f, 0.6f); Bell(b, 0.2, 1046.5f, 1.5f, 0.6f); Bell(b, 0.4, 1318.5f, 1.5f, 0.6f); break;
                case "Marimba":
                    { float[] n = { 523.3f, 659.3f, 784f, 1046.5f, 784f, 1046.5f }; for (int i = 0; i < n.Length; i++) Mar(b, i * 0.1, n[i], 0.7f); break; }
                case "Harp":
                    { float[] n = { 523.3f, 659.3f, 784f, 1046.5f }; for (int i = 0; i < n.Length; i++) Harp(b, i * 0.13, n[i], 0.6f); break; }
                case "Crystal":
                    { float[] n = { 1318.5f, 1661.2f, 1975.5f, 2637f }; for (int i = 0; i < n.Length; i++) Cry(b, i * 0.1, n[i], 0.45f); break; }
                case "Arpeggio":
                    { float[] n = { 440f, 523.3f, 659.3f, 880f, 1046.5f, 1318.5f }; for (int r = 0; r < 2; r++) for (int i = 0; i < n.Length; i++) Harp(b, r * 1.25 + i * 0.09, n[i], 0.55f); break; }
                case "Coin": Osc(b, 0.0, 987.8f, 987.8f, 0.09f, 0.6f, 1, 0.003f, 2f); Osc(b, 0.09, 1318.5f, 1318.5f, 0.6f, 0.6f, 1, 0.003f, 5f); break;
                case "Level up":
                    { float[] n = { 523.3f, 659.3f, 784f, 1046.5f, 1318.5f, 1568f }; for (int i = 0; i < n.Length; i++) Osc(b, i * 0.07, n[i], n[i], i == n.Length - 1 ? 0.7f : 0.09f, 0.45f, 1, 0.003f, i == n.Length - 1 ? 3.5f : 1f); break; }
                case "Cash register":
                    Noise(b, 0.0, 0.05f, 0.5f, 40f, 0.8f); Noise(b, 0.07, 0.04f, 0.4f, 50f, 0.8f);
                    Bell(b, 0.12, 2093f, 1.2f, 0.7f); Bell(b, 0.12, 2637f, 1.2f, 0.45f); break;
                case "Sonar": Osc(b, 0.0, 1100f, 1100f, 1.3f, 0.7f, 0, 0.01f, 3.5f); Osc(b, 1.1, 1100f, 1100f, 1.3f, 0.25f, 0, 0.01f, 3.5f); break;
                case "Doorbell": Note(b, 0.0, 659.3f, 1.3f, 0.7f, BellR, BellA, 2.4f); Note(b, 0.55, 523.3f, 1.8f, 0.7f, BellR, BellA, 1.8f); break;
                case "Pop": Osc(b, 0.0, 500f, 1100f, 0.09f, 0.7f, 0, 0.004f, 12f); Osc(b, 0.14, 700f, 1500f, 0.09f, 0.55f, 0, 0.004f, 12f); break;
                case "Water drop": Osc(b, 0.0, 1800f, 500f, 0.14f, 0.7f, 0, 0.003f, 10f); Osc(b, 0.35, 1500f, 400f, 0.14f, 0.4f, 0, 0.003f, 10f); break;
                case "Xylophone":
                    { float[] n = { 523.3f, 587.3f, 659.3f, 784f, 880f, 1046.5f }; for (int i = 0; i < n.Length; i++) Mar(b, i * 0.075, n[i], 0.6f); Mar(b, 6 * 0.075, 1318.5f, 0.7f); break; }
                case "Music box":
                    { float[] n = { 1046.5f, 1046.5f, 1568f, 1568f, 1760f, 1760f, 1568f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.24, n[i], 0.9f, 0.55f, BoxR, BoxA, 4.5f); break; }
                case "Fanfare":
                    { float[] n = { 523.3f, 523.3f, 523.3f, 659.3f }; double[] t = { 0.0, 0.17, 0.34, 0.55 }; float[] d = { 0.15f, 0.15f, 0.15f, 1.2f };
                      for (int i = 0; i < 4; i++) { Osc(b, t[i], n[i], n[i], d[i], 0.4f, 2, 0.02f, i == 3 ? 1.5f : 3f); Osc(b, t[i], n[i] * 1.5f, n[i] * 1.5f, d[i], 0.25f, 2, 0.02f, i == 3 ? 1.5f : 3f); } break; }
                case "Laser": Osc(b, 0.0, 2400f, 250f, 0.25f, 0.5f, 2, 0.002f, 3f); Osc(b, 0.35, 2000f, 300f, 0.25f, 0.4f, 2, 0.002f, 3f); break;
                case "Sci-fi": { float[] n = { 1200f, 1600f, 2000f, 1600f }; for (int i = 0; i < n.Length; i++) Osc(b, i * 0.1, n[i], n[i], 0.08f, 0.4f, 1, 0.004f, 4f, 30f, 0.02f); break; }
                case "Morse SOS":
                    {
                        double t = 0; string pat = "...---...";
                        for (int i = 0; i < pat.Length; i++) { float d = pat[i] == '.' ? 0.09f : 0.27f; Osc(b, t, 800f, 800f, d, 0.5f, 0, 0.008f, 0f); t += d + 0.09; if (i == 2 || i == 5) t += 0.18; }
                        break;
                    }
                case "Heartbeat":
                    for (int r = 0; r < 2; r++) { double o = r * 1.1; Osc(b, o, 95f, 45f, 0.22f, 0.9f, 0, 0.005f, 12f); Osc(b, o + 0.26, 85f, 42f, 0.22f, 0.65f, 0, 0.005f, 12f); }
                    break;
                case "Gong": Note(b, 0.0, 110f, 3.2f, 0.8f, GongR, GongA, 1.1f); break;
                case "Wind chimes":
                    { float[] pen = { 1046.5f, 1174.7f, 1318.5f, 1568f, 1760f, 2093f }; double t = 0; for (int i = 0; i < 9; i++) { Bell(b, t, pen[rng.Next(pen.Length)], 1.1f, 0.4f); t += 0.07 + rng.NextDouble() * 0.12; } break; }
                case "Sad trombone":
                    { float[] n = { 233f, 220f, 208f, 196f }; double[] t = { 0.0, 0.5, 1.0, 1.5 }; float[] d = { 0.42f, 0.42f, 0.42f, 0.95f };
                      for (int i = 0; i < 4; i++) Osc(b, t[i], n[i], i == 3 ? 175f : n[i], d[i], 0.55f, 2, 0.04f, 1.2f, i == 3 ? 5.5f : 0f, 0.02f); break; }
                case "Boing":
                    { int n = (int)(SR * 0.7); double ph = 0; for (int i = 0; i < n; i++) { float t = (float)i / SR; float f = 500f + 350f * (float)(Math.Sin(2.0 * Math.PI * 9.0 * t) * Math.Exp(-t * 3.0)); ph += 2.0 * Math.PI * f / SR; b[i] += (float)Math.Sin(ph) * 0.7f * (float)Math.Exp(-t * 3.0) * Math.Min(1f, i / (SR * 0.004f)); } break; }
                case "Quack": Osc(b, 0.0, 420f, 300f, 0.17f, 0.6f, 2, 0.01f, 2.5f, 40f, 0.03f); Osc(b, 0.26, 400f, 280f, 0.2f, 0.55f, 2, 0.01f, 2.5f, 40f, 0.03f); break;
                case "Retro beep": for (int i = 0; i < 4; i++) Osc(b, i * 0.16, 1800f, 1800f, 0.09f, 0.5f, 1, 0.004f, 1f); break;
                case "Drum roll":
                    for (int i = 0; i < 28; i++) { double t = i * 0.045; Noise(b, t, 0.04f, 0.3f + 0.7f * i / 28f, 35f, 0.35f); }
                    Noise(b, 1.3, 0.9f, 0.9f, 3.2f, 0.9f); Osc(b, 1.3, 90f, 60f, 0.4f, 0.8f, 0, 0.004f, 8f); break;
                case "Glissando":
                    { float[] sc = { 523.3f, 587.3f, 659.3f, 698.5f, 784f, 880f, 987.8f, 1046.5f, 1174.7f, 1318.5f, 1396.9f, 1568f }; for (int i = 0; i < sc.Length; i++) Harp(b, i * 0.05, sc[i], 0.4f); break; }
                case "Piano":
                    { float[] n = { 261.6f, 329.6f, 392f, 523.3f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.03, n[i], 2.6f, 0.55f, PiaR, PiaA, 1.6f); break; }
                case "Radio squelch": Noise(b, 0.0, 0.07f, 0.6f, 20f, 0.7f); Osc(b, 0.08, 1500f, 1500f, 0.07f, 0.4f, 1, 0.003f, 2f); Noise(b, 0.17, 0.25f, 0.45f, 9f, 0.6f); break;
                case "Radio call":
                    Noise(b, 0.0, 0.05f, 0.5f, 25f, 0.7f); Osc(b, 0.07, 1200f, 1200f, 0.12f, 0.45f, 1, 0.004f, 2f); Osc(b, 0.22, 1800f, 1800f, 0.12f, 0.45f, 1, 0.004f, 2f);
                    Osc(b, 0.37, 1200f, 1200f, 0.12f, 0.45f, 1, 0.004f, 2f); Noise(b, 0.55, 0.45f, 0.4f, 6f, 0.6f); break;
                case "Extract beeps":
                    for (int i = 0; i < 3; i++) Osc(b, i * 0.4, 1000f, 1000f, 0.15f, 0.55f, 1, 0.004f, 2f);
                    Osc(b, 1.3, 1400f, 1400f, 0.6f, 0.6f, 1, 0.004f, 1.5f); break;
                case "Geiger":
                    { double t = 0; double gap = 0.28; while (t < 2.2) { Noise(b, t, 0.004f, 0.9f, 400f, 0.95f); Osc(b, t, 2400f, 2400f, 0.006f, 0.3f, 0, 0.001f, 0f); t += gap * (0.3 + rng.NextDouble() * 1.4); gap = Math.Max(0.03, gap * 0.93); } break; }
                case "Shell casing":
                    { double t = 0; float a = 0.8f; double g = 0.28; for (int i = 0; i < 5; i++) { Note(b, t, 3800f, 0.3f, a, new[] { 1f, 1.52f, 2.31f }, new[] { 1f, 0.6f, 0.3f }, 22f); t += g; g *= 0.62; a *= 0.65f; } break; }
                case "Magazine click":
                    Noise(b, 0.0, 0.012f, 0.8f, 150f, 0.9f); Osc(b, 0.0, 300f, 120f, 0.04f, 0.5f, 0, 0.001f, 40f);
                    Noise(b, 0.14, 0.02f, 1.0f, 120f, 0.85f); Osc(b, 0.14, 220f, 90f, 0.06f, 0.8f, 0, 0.001f, 30f); break;
                case "Pistol shot":
                    Noise(b, 0.0, 0.25f, 1.0f, 18f, 0.8f); Osc(b, 0.0, 140f, 45f, 0.25f, 0.9f, 0, 0.001f, 14f); Noise(b, 0.0, 0.03f, 0.9f, 150f, 0.95f); break;
                case "Bolt action":
                    Noise(b, 0.0, 0.015f, 0.6f, 130f, 0.9f); Osc(b, 0.0, 200f, 100f, 0.05f, 0.5f, 0, 0.001f, 30f);
                    Noise(b, 0.15, 0.3f, 0.25f, 5f, 0.3f);
                    Noise(b, 0.55, 0.012f, 0.7f, 140f, 0.9f); Osc(b, 0.55, 260f, 110f, 0.05f, 0.6f, 0, 0.001f, 30f);
                    Noise(b, 0.72, 0.02f, 1.0f, 120f, 0.85f); Osc(b, 0.72, 200f, 80f, 0.07f, 0.9f, 0, 0.001f, 25f); break;
                case "Tinnitus":
                    Osc(b, 0.0, 4000f, 4000f, 2.2f, 0.25f, 0, 0.2f, 0.9f); Osc(b, 0.0, 4120f, 4120f, 2.2f, 0.12f, 0, 0.2f, 0.9f); break;
                case "Helicopter":
                    { int n = (int)(SR * 2.4); float lp = 0f; for (int i = 0; i < n; i++) { float t = (float)i / SR; float w = (float)(rng.NextDouble() * 2.0 - 1.0); lp += (w - lp) * 0.06f; float m = (float)Math.Sin(Math.PI * 13.0 * t); m = m * m * m * m; float env = Math.Min(1f, t / 0.5f) * Math.Min(1f, (2.4f - t) / 0.8f); b[i] += lp * 6f * m * env * 0.8f + (float)Math.Sin(2.0 * Math.PI * 55.0 * t) * 0.25f * m * env; } break; }
                case "Door creak":
                    { int n = (int)(SR * 1.2); double ph = 0; for (int i = 0; i < n; i++) { float t = (float)i / SR; float f = 180f + 90f * (float)Math.Sin(Math.PI * t / 1.2) + 12f * (float)Math.Sin(2.0 * Math.PI * 24.0 * t); ph += 2.0 * Math.PI * f / SR; double a = 0; for (int h = 1; h <= 8; h++) a += Math.Sin(h * ph) / h; float env = Math.Min(1f, t / 0.1f) * Math.Min(1f, (1.2f - t) / 0.2f); b[i] += (float)a * 0.35f * env; } break; }
                case "Task complete":
                    Osc(b, 0.0, 880f, 880f, 0.5f, 0.45f, 3, 0.006f, 5f); Osc(b, 0.0, 1760f, 1760f, 0.5f, 0.15f, 0, 0.006f, 6f);
                    Osc(b, 0.18, 1318.5f, 1318.5f, 0.9f, 0.5f, 3, 0.006f, 3.5f); Osc(b, 0.18, 2637f, 2637f, 0.9f, 0.15f, 0, 0.006f, 5f); break;
                case "Flashbang":
                    Noise(b, 0.0, 0.35f, 1.0f, 12f, 0.7f); Osc(b, 0.0, 70f, 35f, 0.4f, 0.9f, 0, 0.002f, 8f);
                    Osc(b, 0.1, 4500f, 4500f, 2.0f, 0.3f, 0, 0.05f, 1.3f); Osc(b, 0.1, 4630f, 4630f, 2.0f, 0.15f, 0, 0.05f, 1.3f); break;
                case "Grenade pin":
                    Noise(b, 0.0, 0.01f, 0.6f, 150f, 0.9f);
                    Note(b, 0.0, 2800f, 0.5f, 0.8f, new[] { 1f, 2.4f, 4.1f }, new[] { 1f, 0.6f, 0.3f }, 12f);
                    Note(b, 0.5, 3500f, 0.5f, 0.6f, new[] { 1f, 2.4f, 4.1f }, new[] { 1f, 0.6f, 0.3f }, 10f); break;
                case "Suppressed shot":
                    Osc(b, 0.0, 120f, 55f, 0.12f, 0.8f, 0, 0.002f, 22f); Noise(b, 0.0, 0.14f, 0.5f, 26f, 0.3f);
                    Noise(b, 0.06, 0.012f, 0.3f, 130f, 0.9f); break;
                case "AK reload":
                    Noise(b, 0.0, 0.012f, 0.7f, 140f, 0.9f); Osc(b, 0.0, 280f, 110f, 0.05f, 0.6f, 0, 0.001f, 30f);
                    Noise(b, 0.2, 0.25f, 0.2f, 5f, 0.3f);
                    Noise(b, 0.55, 0.02f, 1.0f, 110f, 0.85f); Osc(b, 0.55, 220f, 80f, 0.08f, 0.9f, 0, 0.001f, 25f);
                    Noise(b, 0.9, 0.03f, 0.9f, 80f, 0.6f); Osc(b, 0.9, 160f, 70f, 0.09f, 0.7f, 0, 0.001f, 22f); break;
                case "Keycard OK": Osc(b, 0.0, 1500f, 1500f, 0.1f, 0.5f, 1, 0.004f, 2f); Osc(b, 0.14, 2000f, 2000f, 0.16f, 0.5f, 1, 0.004f, 2.5f); break;
                case "Keycard denied": Osc(b, 0.0, 220f, 220f, 0.4f, 0.55f, 2, 0.005f, 1f); Osc(b, 0.0, 233f, 233f, 0.4f, 0.4f, 2, 0.005f, 1f); break;
                case "Metal steps":
                    for (int i = 0; i < 6; i++) { double t = i * 0.34; Noise(b, t, 0.01f, 0.5f, 150f, 0.9f); Note(b, t, i % 2 == 0 ? 900f : 820f, 0.25f, 0.6f, new[] { 1f, 2.7f, 5.1f }, new[] { 1f, 0.5f, 0.2f }, 28f); Osc(b, t, 120f, 60f, 0.08f, 0.5f, 0, 0.002f, 25f); } break;
                case "Zipper":
                    { double t = 0; double gap = 0.035; int i = 0; while (t < 0.75) { Noise(b, t, 0.006f, 0.5f, 250f, 0.9f); t += gap; gap = Math.Max(0.012, gap * 0.92); i++; } Osc(b, 0.0, 1500f, 3200f, 0.75f, 0.06f, 0, 0.05f, 0f); break; }
                case "Pill bottle":
                    Noise(b, 0.0, 0.03f, 0.6f, 60f, 0.5f); Osc(b, 0.0, 200f, 100f, 0.05f, 0.5f, 0, 0.002f, 25f);
                    for (int i = 0; i < 22; i++) { double t = 0.12 + i * 0.035 + rng.NextDouble() * 0.02; Noise(b, t, 0.008f, 0.25f + (float)rng.NextDouble() * 0.3f, 200f, 0.9f); } break;
                case "Klaxon":
                    for (int r = 0; r < 2; r++) { Osc(b, r * 1.0, 300f, 480f, 0.9f, 0.5f, 2, 0.02f, 0.6f); Osc(b, r * 1.0 + 0.0, 150f, 240f, 0.9f, 0.3f, 2, 0.02f, 0.6f); } break;
                case "Intercom":
                    Noise(b, 0.0, 0.012f, 0.3f, 150f, 0.9f);
                    { float[] n = { 784f, 659.3f, 523.3f }; for (int i = 0; i < 3; i++) { Osc(b, 0.08 + i * 0.35, n[i], n[i], 0.7f, 0.5f, 3, 0.01f, 3.5f); Osc(b, 0.08 + i * 0.35, n[i] * 2f, n[i] * 2f, 0.7f, 0.12f, 0, 0.01f, 4f); } break; }
                case "Power up":
                    Osc(b, 0.0, 300f, 1500f, 0.4f, 0.4f, 1, 0.01f, 1f); Osc(b, 0.4, 1500f, 1500f, 0.1f, 0.4f, 1, 0.004f, 3f); Osc(b, 0.5, 2000f, 2000f, 0.3f, 0.4f, 1, 0.004f, 4f); break;
                case "Sparkle":
                    { float[] pen = { 2093f, 2349f, 2637f, 3136f, 3520f, 4186f }; double t = 0; for (int i = 0; i < 14; i++) { Bell(b, t, pen[rng.Next(pen.Length)], 0.6f, 0.3f + (float)rng.NextDouble() * 0.2f); t += 0.04 + rng.NextDouble() * 0.07; } break; }
                case "Cuckoo":
                    for (int r = 0; r < 2; r++) { double o = r * 0.55; Osc(b, o, 784f, 770f, 0.2f, 0.55f, 0, 0.01f, 3f); Osc(b, o + 0.22, 659.3f, 650f, 0.26f, 0.55f, 0, 0.01f, 3f); } break;
                case "Whistle":
                    Osc(b, 0.0, 1600f, 2300f, 0.3f, 0.5f, 0, 0.03f, 1f, 6f, 0.012f); Noise(b, 0.0, 0.3f, 0.05f, 3f, 0.9f); Osc(b, 0.35, 2300f, 2500f, 0.4f, 0.5f, 0, 0.03f, 1.5f, 6f, 0.012f); break;
                case "Knock knock":
                    for (int i = 0; i < 3; i++) { double t = i * 0.25; Osc(b, t, 160f, 90f, 0.07f, 0.9f, 0, 0.001f, 30f); Noise(b, t, 0.02f, 0.4f, 90f, 0.4f); } break;
                case "Camera shutter":
                    Noise(b, 0.0, 0.012f, 0.8f, 130f, 0.9f); Osc(b, 0.0, 220f, 110f, 0.03f, 0.5f, 0, 0.001f, 40f); Noise(b, 0.07, 0.015f, 0.6f, 110f, 0.9f); Osc(b, 0.07, 180f, 90f, 0.04f, 0.4f, 0, 0.001f, 40f); break;
                case "Slot machine":
                    { double t = 0; double gap = 0.04; for (int i = 0; i < 14; i++) { Noise(b, t, 0.01f, 0.45f, 160f, 0.9f); Osc(b, t, 900f + i * 40f, 900f + i * 40f, 0.015f, 0.2f, 0, 0.001f, 0f); t += gap; gap += 0.008; }
                      Bell(b, 1.1, 1318.5f, 1.0f, 0.6f); Bell(b, 1.25, 1568f, 1.0f, 0.6f); Bell(b, 1.4, 2093f, 1.0f, 0.7f); break; }
                case "Stim inject":
                    Noise(b, 0.0, 0.012f, 0.7f, 140f, 0.9f); Osc(b, 0.0, 300f, 140f, 0.05f, 0.5f, 0, 0.001f, 30f);
                    Noise(b, 0.08, 0.35f, 0.45f, 5f, 0.85f); Noise(b, 0.5, 0.012f, 0.5f, 140f, 0.9f); break;
                case "Bandage rip":
                    { int n = (int)(SR * 0.4); for (int i = 0; i < n; i++) { float t = (float)i / n; float w = (float)(rng.NextDouble() * 2.0 - 1.0); float crack = rng.NextDouble() < 0.02 ? 1f : 0.35f; b[i] += w * crack * 0.5f * (float)Math.Sin(Math.PI * t); } Noise(b, 0.45, 0.02f, 0.5f, 100f, 0.9f); break; }
                case "Generator":
                    Noise(b, 0.0, 0.05f, 0.5f, 60f, 0.4f); Noise(b, 0.25, 0.04f, 0.4f, 60f, 0.4f);
                    Osc(b, 0.3, 55f, 62f, 2.2f, 0.5f, 2, 0.4f, 0.3f, 7f, 0.03f); Osc(b, 0.3, 110f, 124f, 2.2f, 0.3f, 2, 0.4f, 0.3f, 7f, 0.03f); break;
                case "Trader message": Osc(b, 0.0, 1200f, 1200f, 0.08f, 0.55f, 0, 0.003f, 10f); Osc(b, 0.09, 1800f, 1800f, 0.14f, 0.55f, 0, 0.003f, 9f); break;
                case "Raid horn":
                    Osc(b, 0.0, 110f, 108f, 1.6f, 0.5f, 2, 0.12f, 1.2f); Osc(b, 0.0, 165f, 162f, 1.6f, 0.35f, 2, 0.12f, 1.2f); Osc(b, 0.0, 220f, 216f, 1.6f, 0.25f, 2, 0.12f, 1.2f); break;
                case "Armor hit":
                    Noise(b, 0.0, 0.02f, 0.7f, 120f, 0.9f); Osc(b, 0.0, 120f, 60f, 0.1f, 0.8f, 0, 0.002f, 25f);
                    Note(b, 0.0, 600f, 0.7f, 0.8f, new[] { 1f, 2.3f, 3.7f }, new[] { 1f, 0.6f, 0.3f }, 12f); break;
                case "Epic boom":
                    Noise(b, 0.0, 0.6f, 1.0f, 6f, 0.5f); Osc(b, 0.0, 90f, 28f, 0.9f, 1.0f, 0, 0.002f, 3.5f); Osc(b, 0.0, 55f, 50f, 2.0f, 0.6f, 0, 0.01f, 2f); break;
                case "Cinema braam":
                    { float[] fr = { 55f, 55.5f, 110f, 111f, 164.8f, 220f }; for (int i = 0; i < fr.Length; i++) Osc(b, 0.0, fr[i], fr[i] * 0.97f, 2.2f, 0.28f, 2, 0.18f, 0.9f); break; }
                case "Power chord":
                    { var tmp = new float[b.Length]; float[] fr = { 82.4f, 123.5f, 164.8f, 246.9f }; for (int i = 0; i < fr.Length; i++) Osc(tmp, 0.0, fr[i], fr[i], 1.6f, 0.5f, 2, 0.005f, 1.6f); AddDriven(b, tmp, 4f, 0.7f); break; }
                case "Bass drop":
                    { var tmp = new float[b.Length]; Osc(tmp, 0.0, 160f, 30f, 0.8f, 1.0f, 0, 0.002f, 1.5f); AddDriven(b, tmp, 2.5f, 0.8f); Noise(b, 0.0, 0.05f, 0.4f, 60f, 0.5f); break; }
                case "Sniper shot":
                    Noise(b, 0.0, 0.3f, 1.0f, 14f, 0.8f); Osc(b, 0.0, 170f, 40f, 0.3f, 0.9f, 0, 0.001f, 12f);
                    Noise(b, 0.5, 0.4f, 0.25f, 9f, 0.5f); Noise(b, 0.95, 0.5f, 0.12f, 7f, 0.4f); break;
                case "Machine gun":
                    for (int i = 0; i < 9; i++) { double t = i * 0.075; Noise(b, t, 0.09f, 0.9f, 38f, 0.8f); Osc(b, t, 150f, 60f, 0.06f, 0.6f, 0, 0.001f, 30f); } break;
                case "Explosion":
                    Noise(b, 0.0, 1.3f, 1.0f, 3.2f, 0.15f); Osc(b, 0.0, 75f, 25f, 1.2f, 1.0f, 0, 0.002f, 3f); Noise(b, 0.0, 0.1f, 0.8f, 40f, 0.8f); break;
                case "Taiko drums":
                    { double[] t = { 0.0, 0.3, 0.6, 0.78, 1.0 }; for (int i = 0; i < t.Length; i++) { bool last = i == t.Length - 1; Osc(b, t[i], last ? 100f : 125f, last ? 45f : 55f, last ? 0.9f : 0.4f, last ? 1.0f : 0.8f, 0, 0.002f, last ? 4f : 7f); Noise(b, t[i], 0.025f, 0.4f, 90f, 0.35f); } break; }
                case "Lightning":
                    Noise(b, 0.0, 0.06f, 1.0f, 60f, 0.95f);
                    { int n = (int)(SR * 1.8); float lp = 0f; for (int i = 0; i < n; i++) { float t = (float)i / SR; float w = (float)(rng.NextDouble() * 2.0 - 1.0); lp += (w - lp) * 0.08f; float m = 0.6f + 0.4f * (float)Math.Sin(2.0 * Math.PI * 9.0 * t); int k = (int)(0.12 * SR) + i; if (k < b.Length) b[k] += lp * 5f * m * (float)Math.Exp(-t * 1.8) * 0.7f; } }
                    Osc(b, 0.1, 55f, 40f, 1.5f, 0.6f, 0, 0.05f, 2f); break;
                case "Sword draw":
                    { int n = (int)(SR * 0.4); float lp = 0f; for (int i = 0; i < n; i++) { float t = (float)i / n; float w = (float)(rng.NextDouble() * 2.0 - 1.0); lp += (w - lp) * 0.7f; float h = w - lp; b[i] += h * 0.5f * (float)Math.Sin(Math.PI * t) * (0.4f + t); } }
                    Note(b, 0.3, 3000f, 1.2f, 0.5f, new[] { 1f, 2.4f, 3.9f, 6f }, new[] { 1f, 0.6f, 0.35f, 0.2f }, 4f); break;
                case "Mech boot-up":
                    Osc(b, 0.0, 80f, 420f, 0.8f, 0.3f, 2, 0.05f, 0.5f);
                    for (int i = 0; i < 3; i++) Osc(b, 0.9 + i * 0.1, 1200f + i * 400f, 1200f + i * 400f, 0.07f, 0.4f, 1, 0.003f, 3f);
                    Osc(b, 0.9, 60f, 60f, 0.8f, 0.4f, 0, 0.1f, 2f); break;
                case "Boss warning":
                    for (int i = 0; i < 3; i++) { double t = i * 0.6; Osc(b, t, 110f, 110f, 0.45f, 0.45f, 2, 0.02f, 1.5f); Osc(b, t, 116.5f, 116.5f, 0.45f, 0.4f, 2, 0.02f, 1.5f); }
                    Osc(b, 0.0, 41f, 41f, 2.0f, 0.5f, 0, 0.1f, 0.8f); break;
                case "Marimba up":
                    { float[] n = { 261.6f, 293.7f, 329.6f, 392f, 440f, 523.3f, 587.3f, 659.3f, 784f, 1046.5f }; for (int i = 0; i < n.Length; i++) Mar(b, i * 0.06, n[i], 0.7f); Mar(b, n.Length * 0.06, 1318.5f, 0.8f); break; }
                case "Marimba down":
                    { float[] n = { 1046.5f, 784f, 659.3f, 587.3f, 523.3f, 440f, 392f, 329.6f, 293.7f, 261.6f }; for (int i = 0; i < n.Length; i++) Mar(b, i * 0.07, n[i], 0.7f); Mar(b, n.Length * 0.07, 196f, 0.8f); break; }
                case "Marimba bounce":
                    { float[] n = { 523.3f, 784f, 659.3f, 784f, 1046.5f, 784f, 659.3f, 523.3f }; for (int i = 0; i < n.Length; i++) Mar(b, i * 0.15, n[i], i % 2 == 0 ? 0.8f : 0.55f); break; }
                case "Vibraphone":
                    { float[] n = { 698.5f, 880f, 1046.5f, 1318.5f }; for (int i = 0; i < n.Length; i++) { Note(b, i * 0.2, n[i], 1.6f, 0.6f, new[] { 1f, 4f, 9.2f }, new[] { 1f, 0.3f, 0.08f }, 1.8f); Note(b, i * 0.2, n[i] * 1.004f, 1.6f, 0.4f, new[] { 1f, 4f }, new[] { 1f, 0.2f }, 1.8f); } break; }
                case "Kalimba":
                    { float[] n = { 1046.5f, 1318.5f, 1568f, 1760f, 1568f, 1318.5f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.13, n[i], 0.8f, 0.6f, new[] { 1f, 3.1f, 6.2f }, new[] { 1f, 0.35f, 0.1f }, 7f); break; }
                case "Steel drum":
                    { float[] n = { 523.3f, 659.3f, 784f, 659.3f, 523.3f, 659.3f, 784f, 1046.5f }; for (int i = 0; i < n.Length; i++) { Note(b, i * 0.2, n[i], 0.9f, 0.55f, new[] { 1f, 2f, 3.01f, 4f }, new[] { 1f, 0.6f, 0.4f, 0.3f }, 3.5f); Note(b, i * 0.2, n[i] * 1.005f, 0.9f, 0.35f, new[] { 1f, 2f }, new[] { 1f, 0.5f }, 3.5f); } break; }
                case "Coin x2": Coin(b, 0.0, 1f, 0.3f); Coin(b, 0.22, 1.122f, 0.6f); break;
                case "Coin cascade":
                    for (int i = 0; i < 8; i++) Coin(b, i * 0.1, 1f + i * 0.05f, i == 7 ? 0.7f : 0.15f); break;
                case "Coin drop":
                    { double t = 0; float a = 0.8f; double g = 0.3; for (int i = 0; i < 6; i++) { Note(b, t, 4200f, 0.4f, a, new[] { 1f, 2.8f, 5.2f }, new[] { 1f, 0.5f, 0.25f }, 10f); t += g; g *= 0.68; a *= 0.7f; }
                      Note(b, t, 4200f, 0.9f, 0.3f, new[] { 1f, 2.8f }, new[] { 1f, 0.4f }, 3f); break; }
                case "Coin power-up":
                    Osc(b, 0.0, 987.8f, 987.8f, 0.08f, 0.5f, 1, 0.003f, 2f); Osc(b, 0.09, 1318.5f, 1318.5f, 0.08f, 0.5f, 1, 0.003f, 2f); Osc(b, 0.18, 1975.5f, 1975.5f, 0.7f, 0.55f, 1, 0.003f, 4f); break;
                case "Sad tuba":
                    { float[] n = { 117f, 110f, 104f, 98f }; double[] t = { 0.0, 0.65, 1.3, 1.95 }; float[] d = { 0.55f, 0.55f, 0.55f, 0.95f };
                      for (int i = 0; i < 4; i++) { Osc(b, t[i], n[i], i == 3 ? 82f : n[i], d[i], 0.6f, 2, 0.05f, 1.2f, i == 3 ? 5f : 0f, 0.025f); Osc(b, t[i], n[i] * 2f, i == 3 ? 164f : n[i] * 2f, d[i], 0.2f, 0, 0.05f, 1.5f); } break; }
                case "Womp womp":
                    Osc(b, 0.0, 175f, 165f, 0.55f, 0.6f, 2, 0.03f, 1.8f, 7f, 0.03f); Osc(b, 0.7, 156f, 130f, 1.0f, 0.6f, 2, 0.03f, 1.2f, 6f, 0.05f); break;
                case "Sad violin":
                    { float[] n = { 440f, 392f, 349.2f, 329.6f }; for (int i = 0; i < 4; i++) Osc(b, i * 0.65, n[i], i == 3 ? 311f : n[i], i == 3 ? 1.1f : 0.7f, 0.35f, 2, 0.1f, 0.8f, 5.5f, 0.015f); break; }
                case "Wrong buzzer":
                    Osc(b, 0.0, 150f, 150f, 0.7f, 0.5f, 2, 0.004f, 0f); Osc(b, 0.0, 157f, 157f, 0.7f, 0.5f, 2, 0.004f, 0f); break;
                case "Falling whistle":
                    Osc(b, 0.0, 1600f, 220f, 1.2f, 0.5f, 0, 0.02f, 0.3f); Osc(b, 1.25, 110f, 45f, 0.25f, 0.9f, 0, 0.002f, 12f); Noise(b, 1.25, 0.12f, 0.5f, 25f, 0.4f); break;
                case "Trombone slide":
                    Osc(b, 0.0, 200f, 520f, 0.6f, 0.55f, 2, 0.05f, 0.5f, 5f, 0.02f); Osc(b, 0.6, 520f, 520f, 0.5f, 0.55f, 2, 0.02f, 1.5f, 6f, 0.03f); break;
                case "Bullet on armor": ArmorHit(b, 0.0, 1100f, 1f); break;
                case "Armor ricochet":
                    ArmorHit(b, 0.0, 1300f, 0.8f); Osc(b, 0.01, 4200f, 1400f, 0.4f, 0.3f, 0, 0.002f, 6f); Noise(b, 0.01, 0.2f, 0.12f, 12f, 0.9f); break;
                case "Ceramic plate":
                    Noise(b, 0.0, 0.05f, 1.0f, 50f, 0.95f);
                    { double[] c = { 0.03, 0.06, 0.1, 0.15, 0.21 }; for (int i = 0; i < c.Length; i++) Noise(b, c[i], 0.01f, 0.5f - i * 0.07f, 140f, 0.9f); }
                    Osc(b, 0.0, 120f, 55f, 0.2f, 1.0f, 0, 0.001f, 14f); Note(b, 0.0, 2500f, 0.3f, 0.5f, new[] { 1f, 3.3f, 6.1f }, new[] { 1f, 0.5f, 0.25f }, 25f); break;
                case "Armor tank":
                    ArmorHit(b, 0.0, 1100f, 1f); ArmorHit(b, 0.19, 1000f, 0.85f); ArmorHit(b, 0.36, 950f, 0.75f); break;
                case "Trader ping x3":
                    Osc(b, 0.0, 1200f, 1200f, 0.07f, 0.55f, 0, 0.003f, 10f); Osc(b, 0.09, 1600f, 1600f, 0.07f, 0.55f, 0, 0.003f, 10f); Osc(b, 0.18, 2000f, 2000f, 0.14f, 0.55f, 0, 0.003f, 9f); break;
                case "Trader deal":
                    Ping(b, 0.0, 1200f, 1800f, 0.55f); Noise(b, 0.3, 0.02f, 0.3f, 100f, 0.9f); Bell(b, 0.3, 2093f, 1.0f, 0.5f); Bell(b, 0.3, 2637f, 1.0f, 0.3f); break;
                case "Incoming message":
                    Osc(b, 0.0, 90f, 50f, 0.14f, 0.9f, 0, 0.002f, 20f); Ping(b, 0.0, 1500f, 2000f, 0.5f); Ping(b, 0.28, 1500f, 2000f, 0.5f); break;
                case "Encrypted message":
                    { double t = 0; for (int i = 0; i < 7; i++) { float f = 800f + (float)rng.NextDouble() * 2200f; Osc(b, t, f, f, 0.03f, 0.35f, 1, 0.002f, 0f); Noise(b, t, 0.01f, 0.15f, 100f, 0.9f); t += 0.05; } Ping(b, 0.42, 1200f, 1800f, 0.55f); break; }
                case "Radio trader":
                    Noise(b, 0.0, 0.07f, 0.55f, 20f, 0.7f); Ping(b, 0.1, 1200f, 1800f, 0.55f); Noise(b, 0.4, 0.3f, 0.3f, 8f, 0.6f); break;
                case "Secure line":
                    Osc(b, 0.0, 220f, 220f, 0.4f, 0.5f, 0, 0.01f, 4f); Bell(b, 0.15, 1568f, 0.9f, 0.5f); Ping(b, 0.4, 1500f, 2250f, 0.55f); break;
                case "Mail drop":
                    Ping(b, 0.0, 1200f, 1800f, 0.55f); Osc(b, 0.35, 130f, 60f, 0.14f, 0.9f, 0, 0.002f, 18f); Noise(b, 0.35, 0.02f, 0.5f, 100f, 0.6f);
                    Note(b, 0.35, 700f, 0.5f, 0.5f, new[] { 1f, 2.76f, 5.4f }, new[] { 1f, 0.5f, 0.25f }, 16f); break;
                case "Black market":
                    Osc(b, 0.0, 55f, 55f, 1.5f, 0.6f, 0, 0.12f, 1.8f); Ping(b, 0.3, 1108.7f, 1318.5f, 0.5f); Ping(b, 0.75, 830.6f, 1108.7f, 0.4f); break;
                case "Urgent message":
                    Osc(b, 0.0, 100f, 50f, 0.12f, 0.9f, 0, 0.002f, 20f);
                    for (int i = 0; i < 3; i++) Osc(b, 0.03 + i * 0.09, 2000f, 2000f, 0.05f, 0.5f, 0, 0.002f, 8f);
                    Osc(b, 0.34, 600f, 600f, 0.4f, 0.45f, 1, 0.005f, 3f); break;
                case "Hacked message":
                    for (int i = 0; i < 8; i++) Osc(b, i * 0.04, 600f + i * 260f, 600f + i * 260f, 0.035f, 0.4f, 1, 0.002f, 0f);
                    Ping(b, 0.4, 1200f, 1800f, 0.55f); break;
                case "Kick ping":
                    Osc(b, 0.0, 120f, 45f, 0.2f, 1.0f, 0, 0.001f, 14f); Ping(b, 0.0, 1200f, 1800f, 0.6f); break;
                case "Fire bell":
                    for (int i = 0; i < 36; i++) Note(b, i * 0.045, 1400f, 0.14f, 0.5f, new[] { 1f, 2.4f, 5.2f }, new[] { 1f, 0.5f, 0.2f }, 30f); break;
                case "Smoke detector":
                    for (int g = 0; g < 2; g++) for (int i = 0; i < 3; i++) { Osc(b, g * 0.95 + i * 0.22, 3100f, 3100f, 0.14f, 0.55f, 0, 0.002f, 0f); Osc(b, g * 0.95 + i * 0.22, 3120f, 3120f, 0.14f, 0.3f, 0, 0.002f, 0f); } break;
                case "Alarm clock":
                    for (int g = 0; g < 2; g++) for (int i = 0; i < 4; i++) Osc(b, g * 0.75 + i * 0.1, 1800f, 1800f, 0.06f, 0.5f, 1, 0.002f, 0f); break;
                case "Car alarm":
                    for (int i = 0; i < 6; i++) Osc(b, i * 0.3, 700f, 1500f, 0.28f, 0.45f, 1, 0.005f, 0f); break;
                case "Air raid siren":
                    Osc(b, 0.0, 350f, 750f, 1.3f, 0.45f, 2, 0.3f, 0f); Osc(b, 0.0, 353f, 757f, 1.3f, 0.35f, 2, 0.3f, 0f);
                    Osc(b, 1.3, 750f, 350f, 1.3f, 0.45f, 2, 0.01f, 0.4f); Osc(b, 1.3, 757f, 353f, 1.3f, 0.35f, 2, 0.01f, 0.4f); break;
                case "Burglar alarm":
                    for (int i = 0; i < 12; i++) Osc(b, i * 0.12, i % 2 == 0 ? 960f : 770f, i % 2 == 0 ? 960f : 770f, 0.115f, 0.45f, 1, 0.003f, 0f); break;
                case "Lab alarm":
                    for (int i = 0; i < 3; i++) { Osc(b, i * 0.5, 300f, 800f, 0.38f, 0.5f, 2, 0.01f, 1.5f); Osc(b, i * 0.5, 80f, 80f, 0.3f, 0.5f, 0, 0.005f, 4f); } break;
                case "Lockdown":
                    for (int i = 0; i < 8; i++) { Osc(b, i * 0.24, 440f, 440f, 0.12f, 0.4f, 1, 0.003f, 0f); Osc(b, i * 0.24, 466f, 466f, 0.12f, 0.35f, 1, 0.003f, 0f); }
                    Osc(b, 0.0, 55f, 55f, 1.9f, 0.5f, 0, 0.1f, 0.3f); break;
                case "Evacuation horn":
                    for (int i = 0; i < 3; i++) { Osc(b, i * 0.8, 220f, 220f, 0.6f, 0.45f, 2, 0.03f, 0.8f); Osc(b, i * 0.8, 330f, 330f, 0.6f, 0.3f, 2, 0.03f, 0.8f); Osc(b, i * 0.8, 110f, 110f, 0.6f, 0.3f, 0, 0.03f, 0.8f); } break;
                case "Countdown alarm":
                    { double t = 0; double gap = 0.5; while (gap > 0.09) { Osc(b, t, 1200f, 1200f, 0.06f, 0.5f, 1, 0.003f, 0f); t += gap; gap *= 0.8; } Osc(b, t + 0.05, 1000f, 1000f, 0.9f, 0.5f, 1, 0.003f, 0.5f); break; }
                case "Radiation alert":
                    { double t = 0; while (t < 2.2) { Noise(b, t, 0.004f, 0.8f, 400f, 0.95f); t += 0.025 + rng.NextDouble() * 0.09; } for (int i = 0; i < 4; i++) Osc(b, i * 0.55, 1000f, 1000f, 0.12f, 0.45f, 1, 0.003f, 2f); break; }
                case "Submarine dive":
                    Osc(b, 0.0, 400f, 250f, 0.55f, 0.5f, 2, 0.02f, 0.5f); Osc(b, 0.0, 200f, 125f, 0.55f, 0.4f, 0, 0.02f, 0.5f);
                    Osc(b, 0.7, 250f, 420f, 0.6f, 0.5f, 2, 0.02f, 0.5f); Osc(b, 0.7, 125f, 210f, 0.6f, 0.4f, 0, 0.02f, 0.5f); break;
                case "Siren deep":
                    Osc(b, 0.0, 200f, 450f, 1.4f, 0.5f, 2, 0.3f, 0f); Osc(b, 0.0, 202f, 455f, 1.4f, 0.4f, 2, 0.3f, 0f);
                    Osc(b, 1.4, 450f, 200f, 1.4f, 0.5f, 2, 0.01f, 0.4f); Osc(b, 1.4, 455f, 202f, 1.4f, 0.4f, 2, 0.01f, 0.4f); break;
                case "Double siren":
                    Osc(b, 0.0, 350f, 750f, 1.3f, 0.35f, 2, 0.3f, 0f); Osc(b, 1.3, 750f, 350f, 1.3f, 0.35f, 2, 0.01f, 0.4f);
                    Osc(b, 0.65, 260f, 560f, 1.3f, 0.35f, 2, 0.3f, 0f); Osc(b, 1.95, 560f, 260f, 1.3f, 0.35f, 2, 0.01f, 0.4f); break;
                case "Siren rise":
                    Osc(b, 0.0, 250f, 1100f, 2.6f, 0.5f, 2, 0.3f, 0f); Osc(b, 0.0, 253f, 1110f, 2.6f, 0.4f, 2, 0.3f, 0f); Osc(b, 0.0, 125f, 550f, 2.6f, 0.3f, 0, 0.3f, 0f); break;
                case "Siren pulse":
                    for (int i = 0; i < 4; i++) { Osc(b, i * 0.65, 300f, 700f, 0.5f, 0.5f, 2, 0.04f, 1.2f); Osc(b, i * 0.65, 303f, 706f, 0.5f, 0.4f, 2, 0.04f, 1.2f); } break;
                case "Wartime siren":
                    Osc(b, 0.0, 350f, 700f, 1.0f, 0.5f, 2, 0.25f, 0f); Osc(b, 0.0, 353f, 706f, 1.0f, 0.4f, 2, 0.25f, 0f);
                    Osc(b, 1.0, 700f, 350f, 1.0f, 0.5f, 2, 0.01f, 0f); Osc(b, 1.0, 706f, 353f, 1.0f, 0.4f, 2, 0.01f, 0f);
                    Osc(b, 2.0, 350f, 700f, 0.9f, 0.5f, 2, 0.01f, 0.8f); Osc(b, 2.0, 353f, 706f, 0.9f, 0.4f, 2, 0.01f, 0.8f); break;
                case "Siren far":
                    for (int k = 0; k < 3; k++) { double d = k * 0.35; float a = k == 0 ? 0.4f : (k == 1 ? 0.22f : 0.12f);
                        Osc(b, d, 350f, 750f, 1.3f, a, 2, 0.3f, 0f); Osc(b, d + 1.3, 750f, 350f, 1.3f, a, 2, 0.01f, 0.4f); } break;
                case "Black market deep":
                    Osc(b, 0.0, 41f, 41f, 1.8f, 0.7f, 0, 0.15f, 1.2f); Ping(b, 0.4, 554.4f, 659.3f, 0.5f); Ping(b, 0.9, 415.3f, 554.4f, 0.4f); break;
                case "Black market door":
                    Osc(b, 0.0, 100f, 40f, 0.2f, 1.0f, 0, 0.002f, 14f); Noise(b, 0.0, 0.03f, 0.6f, 100f, 0.6f);
                    Note(b, 0.0, 400f, 0.8f, 0.8f, new[] { 1f, 2.76f, 5.4f }, new[] { 1f, 0.5f, 0.25f }, 8f); Ping(b, 0.5, 1108.7f, 1318.5f, 0.5f); break;
                case "Black market radio":
                    Noise(b, 0.0, 0.07f, 0.55f, 20f, 0.7f); Osc(b, 0.0, 55f, 55f, 1.2f, 0.5f, 0, 0.1f, 1.5f); Ping(b, 0.12, 1108.7f, 1318.5f, 0.45f); Noise(b, 0.5, 0.4f, 0.25f, 7f, 0.6f); break;
                case "Shadow deal":
                    Osc(b, 0.0, 48f, 48f, 1.6f, 0.6f, 0, 0.2f, 1.2f); Osc(b, 0.35, 90f, 40f, 0.15f, 0.8f, 0, 0.002f, 18f); Ping(b, 0.35, 932.3f, 1108.7f, 0.45f); Ping(b, 0.8, 698.5f, 830.6f, 0.4f); break;
                case "Dark ping":
                    Osc(b, 0.0, 60f, 30f, 0.4f, 0.8f, 0, 0.002f, 6f); Note(b, 0.05, 1108.7f, 1.2f, 0.5f, BellR, BellA, 3f); break;
                case "Underground":
                    Osc(b, 0.0, 38f, 38f, 2.0f, 0.6f, 0, 0.3f, 0.8f); Osc(b, 0.3, 1800f, 700f, 0.1f, 0.5f, 0, 0.003f, 12f); Osc(b, 0.9, 1700f, 650f, 0.1f, 0.4f, 0, 0.003f, 12f); Ping(b, 1.3, 932.3f, 1108.7f, 0.45f); break;
                case "Decrypting":
                    { double t = 0, gap = 0.2; while (gap > 0.03) { float f = 800f + (float)rng.NextDouble() * 2200f; Osc(b, t, f, f, 0.03f, 0.35f, 1, 0.002f, 0f); t += gap; gap *= 0.85; }
                      Osc(b, t + 0.05, 1200f, 1200f, 0.08f, 0.5f, 1, 0.003f, 2f); Osc(b, t + 0.15, 1600f, 1600f, 0.08f, 0.5f, 1, 0.003f, 2f); Osc(b, t + 0.25, 2000f, 2000f, 0.25f, 0.5f, 1, 0.003f, 4f); break; }
                case "Encrypted burst":
                    Noise(b, 0.0, 0.15f, 0.5f, 10f, 0.7f); for (int i = 0; i < 12; i++) { float f = 600f + (float)rng.NextDouble() * 2800f; Osc(b, i * 0.015, f, f, 0.012f, 0.3f, 1, 0.001f, 0f); }
                    Ping(b, 0.3, 1200f, 1800f, 0.55f); break;
                case "Data transfer":
                    { double t = 0; while (t < 0.7) { float f = 600f + (float)rng.NextDouble() * 2900f; Osc(b, t, f, f, 0.02f, 0.3f, 1, 0.001f, 0f); t += 0.022; } Noise(b, 0.0, 0.7f, 0.06f, 0f, 0.8f); Osc(b, 0.8, 1800f, 1800f, 0.12f, 0.5f, 1, 0.003f, 2f); break; }
                case "Access granted":
                    for (int i = 0; i < 4; i++) { float f = 700f + (float)rng.NextDouble() * 1500f; Osc(b, i * 0.04, f, f, 0.03f, 0.3f, 1, 0.002f, 0f); }
                    Osc(b, 0.25, 1000f, 1000f, 0.1f, 0.5f, 1, 0.003f, 2f); Osc(b, 0.37, 1333f, 1333f, 0.1f, 0.5f, 1, 0.003f, 2f); Osc(b, 0.49, 1777f, 1777f, 0.4f, 0.5f, 1, 0.003f, 4f); break;
                case "Signal lock":
                    Osc(b, 0.0, 400f, 2400f, 0.5f, 0.35f, 0, 0.02f, 0f); Osc(b, 0.6, 1800f, 1800f, 0.08f, 0.5f, 1, 0.003f, 2f); Osc(b, 0.75, 1800f, 1800f, 0.08f, 0.5f, 1, 0.003f, 2f); Osc(b, 0.9, 1800f, 1800f, 0.35f, 0.5f, 1, 0.003f, 4f); break;
                case "Cipher":
                    { float[] pen = { 1318.5f, 1568f, 1760f, 2093f, 2349f, 3136f }; for (int i = 0; i < 12; i++) { float nf = pen[rng.Next(pen.Length)]; Osc(b, i * 0.045, nf, nf, 0.05f, 0.4f, 3, 0.002f, 6f); } Ping(b, 0.62, 1200f, 1800f, 0.55f); break; }
                case "Blowout siren":
                    Noise(b, 0.0, 3.2f, 0.25f, 0f, 0.1f);
                    Osc(b, 0.0, 150f, 520f, 2.2f, 0.45f, 2, 0.5f, 0f, 0.6f, 0.02f); Osc(b, 0.0, 152f, 526f, 2.2f, 0.35f, 2, 0.5f, 0f, 0.6f, 0.02f);
                    Osc(b, 2.2, 520f, 140f, 1.2f, 0.45f, 2, 0.01f, 0.8f); Osc(b, 2.2, 526f, 142f, 1.2f, 0.35f, 2, 0.01f, 0.8f); break;
                case "Psi storm":
                    Osc(b, 0.0, 200f, 800f, 2.1f, 0.4f, 0, 0.4f, 0.3f, 8f, 0.25f); Osc(b, 0.0, 205f, 810f, 2.1f, 0.3f, 0, 0.4f, 0.3f, 7f, 0.3f); Noise(b, 0.0, 2.1f, 0.12f, 0f, 0.8f); break;
                case "Anomaly detector":
                    { double t = 0, gap = 0.4; float f = 800f; while (gap > 0.06) { Osc(b, t, f, f, 0.05f, 0.5f, 0, 0.003f, 0f); t += gap; gap *= 0.8; f += 40f; } Osc(b, t, f, f, 0.5f, 0.5f, 0, 0.003f, 1f); break; }
                case "Artifact found":
                    { float[] n = { 880f, 1108.7f, 1318.5f, 1760f }; for (int i = 0; i < n.Length; i++) Bell(b, i * 0.14, n[i], 1.3f, 0.5f);
                      float[] pen = { 2637f, 3136f, 3520f, 4186f }; for (int i = 0; i < 8; i++) Bell(b, 0.6 + i * 0.07, pen[rng.Next(pen.Length)], 0.5f, 0.2f); break; }
                case "Electro anomaly":
                    Osc(b, 0.0, 3000f, 300f, 0.1f, 0.5f, 2, 0.001f, 8f);
                    { double t = 0.05; while (t < 0.9) { Noise(b, t, 0.003f, 0.8f, 400f, 0.95f); t += 0.004 + rng.NextDouble() * 0.04; } }
                    Osc(b, 0.0, 50f, 50f, 0.9f, 0.35f, 2, 0.01f, 3f); break;
                case "PDA message":
                    Osc(b, 0.0, 1400f, 1400f, 0.06f, 0.45f, 3, 0.003f, 6f); Osc(b, 0.08, 1000f, 1000f, 0.06f, 0.45f, 3, 0.003f, 6f); Osc(b, 0.16, 1700f, 1700f, 0.14f, 0.45f, 3, 0.003f, 6f); break;
                case "PDA ping": Noise(b, 0.0, 0.008f, 0.3f, 150f, 0.9f); Osc(b, 0.0, 1500f, 1500f, 0.12f, 0.5f, 3, 0.003f, 7f); break;
                case "Radio static":
                    Noise(b, 0.0, 0.06f, 0.5f, 20f, 0.7f);
                    { double[] t = { 0.1, 0.55, 0.9, 1.25 }; double[] d = { 0.4, 0.3, 0.3, 0.3 }; for (int i = 0; i < 4; i++) Noise(b, t[i], (float)d[i], 0.3f, 2f, 0.65f); }
                    Noise(b, 1.5, 0.2f, 0.45f, 12f, 0.6f); break;
                case "Numbers station":
                    Noise(b, 0.0, 2.6f, 0.08f, 0f, 0.7f);
                    { float[] n = { 800f, 1000f, 600f, 1200f, 900f, 700f, 1100f, 850f }; for (int i = 0; i < n.Length; i++) Osc(b, 0.2 + i * 0.26, n[i], n[i], 0.15f, 0.45f, 0, 0.005f, 1f); }
                    Osc(b, 2.4, 440f, 440f, 0.5f, 0.4f, 0, 0.01f, 2f); break;
                case "Bolt throw":
                    Note(b, 0.0, 1800f, 0.2f, 0.7f, new[] { 1f, 2.5f, 4.4f }, new[] { 1f, 0.5f, 0.25f }, 30f);
                    Note(b, 0.4, 1400f, 0.25f, 0.6f, new[] { 1f, 2.5f, 4.4f }, new[] { 1f, 0.5f, 0.25f }, 25f); Note(b, 0.62, 1500f, 0.2f, 0.35f, new[] { 1f, 2.5f }, new[] { 1f, 0.5f }, 30f); break;
                case "Campfire":
                    Noise(b, 0.0, 2.5f, 0.06f, 0f, 0.3f);
                    { double t = 0; while (t < 2.5) { Noise(b, t, 0.006f + (float)rng.NextDouble() * 0.015f, 0.2f + (float)rng.NextDouble() * 0.6f, 150f, 0.6f); t += 0.01 + rng.NextDouble() * 0.15; } break; }
                case "Guitar strum":
                    { float[] n = { 82.4f, 123.5f, 164.8f, 196f, 246.9f, 329.6f }; for (int i = 0; i < n.Length; i++) Pluck(b, i * 0.035, n[i], 1.8f, 0.6f); break; }
                case "Vodka clink":
                    Note(b, 0.0, 3200f, 0.7f, 0.7f, new[] { 1f, 2.32f, 4.25f, 6.63f }, new[] { 1f, 0.6f, 0.35f, 0.2f }, 9f);
                    Note(b, 0.05, 3700f, 0.6f, 0.5f, new[] { 1f, 2.32f, 4.25f }, new[] { 1f, 0.5f, 0.3f }, 11f); break;
                case "Gas mask":
                    { for (int c = 0; c < 2; c++) { double o = c * 1.6; int n1 = (int)(SR * 0.7); float lp = 0f; for (int i = 0; i < n1; i++) { int k = (int)(o * SR) + i; if (k >= b.Length) break; float t = (float)i / n1; float w = (float)(rng.NextDouble() * 2.0 - 1.0); lp += (w - lp) * 0.12f; b[k] += lp * 1.6f * (float)Math.Sin(Math.PI * t) * 0.5f; }
                        int n2 = (int)(SR * 0.8); lp = 0f; double o2 = o + 0.8; for (int i = 0; i < n2; i++) { int k = (int)(o2 * SR) + i; if (k >= b.Length) break; float t = (float)i / n2; float w = (float)(rng.NextDouble() * 2.0 - 1.0); lp += (w - lp) * 0.1f; b[k] += lp * 1.4f * (1f - t) * 0.5f; }
                        Noise(b, o2, 0.01f, 0.4f, 150f, 0.8f); }
                      break; }
                case "Energy drink":
                    Noise(b, 0.0, 0.012f, 0.7f, 140f, 0.9f); Osc(b, 0.0, 1200f, 300f, 0.04f, 0.5f, 0, 0.001f, 30f); Noise(b, 0.06, 1.3f, 0.25f, 2.5f, 0.9f); break;
                case "Singing bowl":
                    Note(b, 0.0, 330f, 3.6f, 0.6f, new[] { 1f, 2.71f, 5.15f }, new[] { 1f, 0.5f, 0.2f }, 0.9f); Note(b, 0.0, 331.5f, 3.6f, 0.5f, new[] { 1f, 2.71f }, new[] { 1f, 0.4f }, 0.9f); break;
                case "Zen bell": Note(b, 0.0, 523.3f, 2.6f, 0.6f, BellR, BellA, 1.2f); break;
                case "Cotton ping": Osc(b, 0.0, 880f, 880f, 0.5f, 0.5f, 0, 0.015f, 5f); Osc(b, 0.12, 1318.5f, 1318.5f, 0.5f, 0.3f, 0, 0.015f, 5f); break;
                case "Soft piano":
                    { float[] n = { 523.3f, 659.3f, 784f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.25, n[i], 2.4f, 0.5f, PiaR, new[] { 1f, 0.35f, 0.12f, 0.06f, 0.02f }, 2.4f); break; }
                case "Dream pad":
                    { float[] n = { 261.6f, 329.6f, 392f, 587.3f }; for (int i = 0; i < n.Length; i++) { Osc(b, 0.0, n[i], n[i], 2.9f, 0.3f, 0, 0.7f, 0.5f, 0.4f, 0.004f); Osc(b, 0.0, n[i] * 1.003f, n[i] * 1.003f, 2.9f, 0.2f, 0, 0.7f, 0.5f, 0.3f, 0.004f); } break; }
                case "Lullaby":
                    { float[] n = { 1046.5f, 1174.7f, 1318.5f, 1568f, 1318.5f, 1174.7f, 1046.5f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.3, n[i], 1.1f, 0.4f, BoxR, BoxA, 3.5f); break; }
                case "Rain drops":
                    { double t = 0; while (t < 2.3) { float f = 1200f + (float)rng.NextDouble() * 1400f; Osc(b, t, f, f * 0.85f, 0.12f, 0.2f + (float)rng.NextDouble() * 0.25f, 0, 0.003f, 16f); t += 0.08 + rng.NextDouble() * 0.28; } break; }
                case "Bubbles":
                    { double t = 0; for (int i = 0; i < 6; i++) { float f = 400f + (float)rng.NextDouble() * 500f; Osc(b, t, f, f * 2.2f, 0.07f, 0.4f, 0, 0.004f, 14f); t += 0.1 + rng.NextDouble() * 0.15; } break; }
                case "Wind bells soft":
                    { float[] pen = { 1568f, 1760f, 2093f, 2349f, 2637f }; double t = 0; for (int i = 0; i < 7; i++) { Note(b, t, pen[rng.Next(pen.Length)], 1.5f, 0.2f, BellR, BellA, 2.2f); t += 0.2 + rng.NextDouble() * 0.2; } break; }
                case "Harp lullaby":
                    { float[] n = { 392f, 523.3f, 659.3f, 784f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.22, n[i], 1.8f, 0.35f, HarpR, HarpA, 2.8f); break; }
                case "Celesta":
                    { float[] n = { 1046.5f, 1318.5f, 1568f, 2093f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.15, n[i], 1.0f, 0.3f, BoxR, BoxA, 3.5f); break; }
                case "Feather chime":
                    Osc(b, 0.0, 1046.5f, 1046.5f, 1.4f, 0.4f, 3, 0.02f, 3.5f); Osc(b, 0.0, 1568f, 1568f, 1.4f, 0.25f, 0, 0.02f, 4f); break;
                case "Soft heartbeat":
                    for (int r = 0; r < 2; r++) { double o = r * 1.1; Osc(b, o, 80f, 50f, 0.3f, 0.45f, 0, 0.03f, 8f); Osc(b, o + 0.28, 72f, 46f, 0.3f, 0.35f, 0, 0.03f, 8f); } break;
                case "Ocean wave":
                    { int n = (int)(SR * 2.8); float lp = 0f; for (int i = 0; i < n; i++) { float t = (float)i / SR; float w = (float)(rng.NextDouble() * 2.0 - 1.0); lp += (w - lp) * 0.04f; float sw = 0.5f - 0.5f * (float)Math.Cos(2.0 * Math.PI * t / 2.8); b[i] += lp * 9f * sw * 0.5f; } break; }
                case "Birdsong":
                    { double[] t = { 0.0, 0.2, 0.4, 1.1, 1.3 }; for (int i = 0; i < t.Length; i++) { float f = 2600f + (float)rng.NextDouble() * 900f; Osc(b, t[i], f, f * 1.35f, 0.1f, 0.3f, 0, 0.01f, 8f, 25f, 0.02f); } break; }
                case "Soft keys":
                    { float[] n = { 261.6f, 329.6f, 392f, 493.9f }; for (int i = 0; i < n.Length; i++) Note(b, i * 0.04, n[i], 2.8f, 0.4f, new[] { 1f, 2f, 5f }, new[] { 1f, 0.25f, 0.1f }, 1.6f); break; }
                case "Cat sleepy mew": CatVoice(b, 0.0, 1.1f, 0.8f, 400f, 620f, 340f, 0f, 0f, 5f); break;
                case "Cat angry mew": CatVoice(b, 0.0, 0.7f, 0.9f, 650f, 1050f, 480f, 0f, 0f, 9f); Noise(b, 0.0, 0.7f, 0.12f, 2f, 0.5f); break;
                case "Cat call": CatVoice(b, 0.0, 1.6f, 0.9f, 450f, 850f, 480f, 0f, 0f, 6f); break;
                case "Kitten chirps": for (int i = 0; i < 3; i++) CatVoice(b, i * 0.25, 0.14f, 0.8f, 950f + i * 80f, 1350f, 1500f, 0f, 0f); break;
                case "Purr and mew": Purr(b, 0.0, 2.2f, 0.7f, 25f, 0.12f); CatVoice(b, 2.0, 0.8f, 0.8f, 480f, 900f, 560f, 0f, 0f, 6f); break;
                case "Dog bark": CatVoice(b, 0.0, 0.2f, 1.0f, 420f, 330f, 220f, 0f, 0f); Noise(b, 0.0, 0.12f, 0.35f, 18f, 0.3f); break;
                case "Dog double bark": CatVoice(b, 0.0, 0.2f, 1.0f, 420f, 330f, 220f, 0f, 0f); Noise(b, 0.0, 0.12f, 0.35f, 18f, 0.3f); CatVoice(b, 0.35, 0.22f, 1.0f, 450f, 350f, 230f, 0f, 0f); Noise(b, 0.35, 0.12f, 0.35f, 18f, 0.3f); break;
                case "Puppy yip": CatVoice(b, 0.0, 0.12f, 0.8f, 900f, 1300f, 1000f, 0f, 0f); CatVoice(b, 0.2, 0.12f, 0.8f, 950f, 1400f, 1050f, 0f, 0f); break;
                case "Wolf howl": CatVoice(b, 0.0, 2.1f, 0.8f, 330f, 620f, 450f, 0f, 0f, 5f); break;
                case "Owl hoot": Osc(b, 0.0, 430f, 380f, 0.38f, 0.7f, 0, 0.06f, 2f, 5f, 0.01f); Osc(b, 0.55, 430f, 360f, 0.5f, 0.7f, 0, 0.06f, 2f, 5f, 0.01f); break;
                case "Frog ribbit": CatVoice(b, 0.0, 0.25f, 0.9f, 170f, 260f, 190f, 38f, 0.9f); CatVoice(b, 0.3, 0.3f, 0.9f, 180f, 280f, 200f, 38f, 0.9f); break;
                case "Rooster": CatVoice(b, 0.0, 0.25f, 0.9f, 500f, 800f, 750f, 0f, 0f); CatVoice(b, 0.3, 0.2f, 0.9f, 650f, 1000f, 900f, 0f, 0f); CatVoice(b, 0.55, 0.9f, 0.9f, 800f, 1100f, 500f, 0f, 0f, 9f); break;
                case "Sheep": CatVoice(b, 0.0, 0.9f, 0.9f, 350f, 430f, 300f, 35f, 0.6f); break;
                case "Cricket": for (int i = 0; i < 6; i++) Osc(b, i * 0.08, 4300f, 4300f, 0.045f, 0.3f, 0, 0.005f, 20f); break;
                case "Crow caw": CatVoice(b, 0.0, 0.3f, 0.9f, 600f, 450f, 300f, 0f, 0f); Noise(b, 0.0, 0.3f, 0.15f, 5f, 0.4f); CatVoice(b, 0.45, 0.3f, 0.9f, 580f, 440f, 290f, 0f, 0f); Noise(b, 0.45, 0.3f, 0.15f, 5f, 0.4f); break;
                case "Dolphin": for (int i = 0; i < 3; i++) Osc(b, i * 0.25, 1800f, 4200f, 0.18f, 0.35f, 0, 0.01f, 6f); break;
                case "Owl double hoot": Osc(b, 0.0, 440f, 390f, 0.25f, 0.7f, 0, 0.05f, 3f, 5f, 0.01f); Osc(b, 0.3, 440f, 390f, 0.25f, 0.7f, 0, 0.05f, 3f, 5f, 0.01f); Osc(b, 0.8, 420f, 350f, 0.6f, 0.7f, 0, 0.06f, 2f, 5f, 0.01f); break;
                case "Owl deep": Osc(b, 0.0, 280f, 250f, 0.5f, 0.8f, 0, 0.08f, 1.5f, 4f, 0.01f); Osc(b, 0.7, 280f, 230f, 0.7f, 0.8f, 0, 0.08f, 1.5f, 4f, 0.01f); break;
                case "Owl night": Osc(b, 0.0, 430f, 380f, 0.4f, 0.6f, 0, 0.06f, 2f, 5f, 0.01f); Osc(b, 0.6, 430f, 370f, 0.5f, 0.6f, 0, 0.06f, 2f, 5f, 0.01f); Osc(b, 1.9, 380f, 330f, 0.4f, 0.35f, 0, 0.06f, 2f, 5f, 0.01f); Osc(b, 2.4, 380f, 320f, 0.5f, 0.35f, 0, 0.06f, 2f, 5f, 0.01f); break;
                case "Owl screech": CatVoice(b, 0.0, 0.9f, 0.8f, 1400f, 2400f, 1700f, 0f, 0f, 14f); Noise(b, 0.0, 0.9f, 0.1f, 2f, 0.6f); break;
                case "Frog croak deep": CatVoice(b, 0.0, 0.45f, 1.0f, 110f, 170f, 120f, 32f, 0.9f); CatVoice(b, 0.6, 0.5f, 1.0f, 100f, 160f, 110f, 32f, 0.9f); break;
                case "Frog chorus": for (int i = 0; i < 8; i++) { float f = 150f + (float)rng.NextDouble() * 120f; CatVoice(b, i * 0.28 + rng.NextDouble() * 0.1, 0.2f, 0.7f, f, f * 1.5f, f * 1.1f, 36f, 0.9f); } break;
                case "Tree frog": for (int i = 0; i < 4; i++) Osc(b, i * 0.16, 2200f, 2500f, 0.1f, 0.35f, 0, 0.01f, 12f, 40f, 0.04f); break;
                case "Frog bubble": CatVoice(b, 0.0, 0.18f, 0.9f, 200f, 400f, 300f, 40f, 0.8f); CatVoice(b, 0.25, 0.18f, 0.9f, 220f, 450f, 330f, 40f, 0.8f); CatVoice(b, 0.5, 0.25f, 0.9f, 240f, 500f, 350f, 40f, 0.8f); break;
                case "Wolf howl long": CatVoice(b, 0.0, 3.5f, 0.8f, 300f, 700f, 400f, 0f, 0f, 5.5f); break;
                case "Wolf short howl": CatVoice(b, 0.0, 1.2f, 0.8f, 380f, 640f, 500f, 0f, 0f, 6f); break;
                case "Wolf pack": CatVoice(b, 0.0, 3.2f, 0.7f, 320f, 650f, 420f, 0f, 0f, 5f); CatVoice(b, 0.5, 3.0f, 0.55f, 420f, 780f, 520f, 0f, 0f, 6f); CatVoice(b, 1.0, 2.6f, 0.5f, 250f, 540f, 350f, 0f, 0f, 4.5f); break;
                case "Wolf distant": CatVoice(b, 0.0, 3.0f, 0.5f, 330f, 680f, 420f, 0f, 0f, 5.5f); break;
                case "Wolf growl": CatVoice(b, 0.0, 1.2f, 0.8f, 90f, 110f, 80f, 28f, 0.7f); Noise(b, 0.0, 1.2f, 0.1f, 1.5f, 0.2f); break;
                case "Wolf and owl": CatVoice(b, 0.0, 2.2f, 0.7f, 330f, 660f, 430f, 0f, 0f, 5.5f); Osc(b, 2.6, 430f, 380f, 0.35f, 0.5f, 0, 0.06f, 2f, 5f, 0.01f); Osc(b, 3.1, 430f, 360f, 0.5f, 0.5f, 0, 0.06f, 2f, 5f, 0.01f); break;
                case "Beer bottle opening": Noise(b, 0.0, 0.006f, 0.6f, 300f, 0.3f); MetalPing(b, 0.0, 3300f, 0.5f); Osc(b, 0.03, 230f, 140f, 0.14f, 0.9f, 0, 0.002f, 22f); Noise(b, 0.03, 0.04f, 0.5f, 70f, 0.4f); Noise(b, 0.04, 1.4f, 0.28f, 3.2f, 0.07f); Noise(b, 0.04, 0.35f, 0.25f, 9f, 0.15f); Fizz(b, 0.12, 1.7, 90, 0.2f, 1.6f); MetalPing(b, 0.32, 4300f, 0.35f); MetalPing(b, 0.47, 4200f, 0.22f); MetalPing(b, 0.56, 4100f, 0.14f); MetalPing(b, 0.62, 4000f, 0.08f); break;
                case "Beer cap drop": MetalPing(b, 0.0, 4300f, 0.6f); MetalPing(b, 0.15, 4200f, 0.4f); MetalPing(b, 0.26, 4100f, 0.25f); MetalPing(b, 0.33, 4000f, 0.15f); MetalPing(b, 0.37, 3900f, 0.08f); break;
                case "Can opening": Noise(b, 0.0, 0.004f, 0.5f, 400f, 0.3f); Osc(b, 0.0, 1900f, 1300f, 0.015f, 0.35f, 0, 0.001f, 120f); Noise(b, 0.07, 0.004f, 0.4f, 400f, 0.3f); Osc(b, 0.07, 1500f, 1000f, 0.015f, 0.3f, 0, 0.001f, 120f); Noise(b, 0.2, 0.012f, 0.8f, 160f, 0.25f); Osc(b, 0.2, 340f, 200f, 0.09f, 0.7f, 0, 0.002f, 35f); MetalPing(b, 0.2, 1900f, 0.25f); Noise(b, 0.21, 0.5f, 0.55f, 5.5f, 0.06f); Noise(b, 0.21, 1.4f, 0.2f, 2.4f, 0.1f); Fizz(b, 0.3, 1.9, 120, 0.22f, 1.5f); break;
                case "Beer cheers": Gl(b, 0.0, 2350f, 1.1f, 0.7f); Gl(b, 0.0, 3320f, 0.9f, 0.35f); Noise(b, 0.0, 0.004f, 0.6f, 350f, 0.3f); Gl(b, 0.0, 2480f, 1.0f, 0.4f); break;
                case "Cat prrt": CatVoice(b, 0.0, 0.3f, 0.9f, 520f, 700f, 820f, 28f, 0.8f); break;
                case "Cat double prrt": CatVoice(b, 0.0, 0.28f, 0.9f, 500f, 680f, 790f, 28f, 0.8f); CatVoice(b, 0.4, 0.34f, 0.9f, 560f, 760f, 900f, 28f, 0.8f); break;
                case "Cat chirp": CatVoice(b, 0.0, 0.14f, 0.8f, 700f, 1000f, 1150f, 0f, 0f); CatVoice(b, 0.2, 0.18f, 0.8f, 760f, 1100f, 1300f, 0f, 0f); break;
                case "Cat mew": CatVoice(b, 0.0, 0.8f, 0.9f, 480f, 900f, 560f, 0f, 0f, 6f); break;
                case "Kitten mew": CatVoice(b, 0.0, 0.55f, 0.8f, 800f, 1350f, 900f, 0f, 0f, 7f); break;
                case "Cat trill up": CatVoice(b, 0.0, 0.5f, 0.9f, 450f, 800f, 1100f, 24f, 0.75f); break;
                case "Cat greeting": CatVoice(b, 0.0, 0.3f, 0.9f, 520f, 720f, 860f, 28f, 0.8f); CatVoice(b, 0.45, 0.8f, 0.8f, 560f, 850f, 540f, 0f, 0f, 6f); break;
                case "Purr": Purr(b, 0.0, 2.8f, 0.9f, 25f, 0.12f); break;
                case "Purr deep": Purr(b, 0.0, 3.0f, 1.0f, 20f, 0.06f); break;
                case "Purr soft": Purr(b, 0.0, 2.8f, 0.7f, 26f, 0.2f); break;
                case "Purr and prrt": Purr(b, 0.0, 2.2f, 0.7f, 25f, 0.12f); CatVoice(b, 2.3, 0.3f, 0.9f, 520f, 700f, 820f, 28f, 0.8f); break;
                case "Kitten purr": Purr(b, 0.0, 2.4f, 0.6f, 32f, 0.25f); break;
                case "Triple chime": Gl(b, 0.0, 1108.7f, 0.9f, 0.6f); Gl(b, 0.16, 1479.98f, 0.9f, 0.6f); Gl(b, 0.32, 1760f, 1.0f, 0.6f); break;
                case "Glass tap": Gl(b, 0.0, 1568f, 0.8f, 0.7f); break;
                case "Swoosh":
                    { int n = (int)(SR * 0.4); float lp = 0f; for (int i = 0; i < n; i++) { float t = (float)i / n; float w = (float)(rng.NextDouble() * 2.0 - 1.0); float a = 0.05f + 0.6f * t; lp += (w - lp) * a; b[i] += (w - lp) * 0.4f * (float)Math.Sin(Math.PI * t) * (0.3f + t); } }
                    Osc(b, 0.0, 600f, 1300f, 0.3f, 0.25f, 0, 0.03f, 4f); break;
                case "Key tock": Osc(b, 0.0, 900f, 450f, 0.025f, 0.6f, 0, 0.001f, 60f); Noise(b, 0.0, 0.006f, 0.25f, 200f, 0.8f); break;
                case "Plug-in chime": Gl(b, 0.0, 1318.5f, 0.8f, 0.6f); Gl(b, 0.14, 1975.5f, 1.0f, 0.6f); Bell(b, 0.3, 2637f, 0.7f, 0.2f); break;
                case "Unlock click": Noise(b, 0.0, 0.01f, 0.5f, 150f, 0.7f); Osc(b, 0.0, 220f, 120f, 0.04f, 0.5f, 0, 0.001f, 40f); Osc(b, 0.08, 1800f, 2200f, 0.05f, 0.25f, 0, 0.005f, 20f); break;
                case "Success tick": Osc(b, 0.0, 1500f, 1500f, 0.06f, 0.5f, 0, 0.004f, 14f); Osc(b, 0.07, 2250f, 2250f, 0.2f, 0.5f, 0, 0.004f, 10f); break;
                case "Beam":
                    Osc(b, 0.0, 500f, 1800f, 0.3f, 0.3f, 0, 0.05f, 1f); Gl(b, 0.3, 2093f, 0.7f, 0.5f); break;
                case "Listening blip": Osc(b, 0.0, 700f, 1100f, 0.1f, 0.5f, 0, 0.01f, 5f); Osc(b, 0.12, 1100f, 1100f, 0.1f, 0.45f, 0, 0.005f, 8f); break;
                case "Bloom":
                    Osc(b, 0.0, 880f, 880f, 1.6f, 0.4f, 0, 0.08f, 2f); Osc(b, 0.1, 1318.5f, 1318.5f, 1.5f, 0.3f, 0, 0.08f, 2.2f); Osc(b, 0.2, 1760f, 1760f, 1.4f, 0.25f, 0, 0.08f, 2.5f); break;
                case "Low battery":
                    for (int i = 0; i < 2; i++) { Osc(b, i * 0.8, 880f, 880f, 0.18f, 0.5f, 0, 0.01f, 5f); Osc(b, i * 0.8 + 0.2, 660f, 660f, 0.4f, 0.5f, 0, 0.01f, 4f); } break;
                case "Radar pulse":
                    for (int i = 0; i < 3; i++) { Osc(b, i * 0.7, 1400f, 1400f, 0.5f, 0.5f - i * 0.1f, 0, 0.005f, 6f); } break;
                case "Glass chord":
                    { float[] n = { 1046.5f, 1318.5f, 1568f }; for (int i = 0; i < n.Length; i++) Gl(b, 0.0, n[i], 1.3f, 0.5f); break; }
                case "Ding": Osc(b, 0.0, 1318.5f, 1318.5f, 0.9f, 0.6f, 0, 0.003f, 6f); Osc(b, 0.0, 2637f, 2637f, 0.5f, 0.2f, 0, 0.003f, 9f); break;
                case "Old phone":
                    for (int r = 0; r < 2; r++)
                    {
                        double o = r * 1.1; int n = (int)(SR * 0.8); int s0 = (int)(o * SR);
                        for (int i = 0; i < n; i++) { int k = s0 + i; if (k >= b.Length) break; float t = (float)i / SR; float am = Math.Sin(2.0 * Math.PI * 22.0 * t) > 0 ? 1f : 0.15f; b[k] += (float)(Math.Sin(2.0 * Math.PI * 440.0 * t) + Math.Sin(2.0 * Math.PI * 480.0 * t)) * 0.3f * am * Math.Min(1f, Math.Min(i / (SR * 0.01f), (n - i) / (SR * 0.02f))); }
                    }
                    break;

            }
            float[] fin = Finish(b);
            float gain = 1f;
            switch (name)
            {
                case "Tinnitus": gain = 0.35f; break;
                case "Epic boom": case "Explosion": case "Bass drop": case "Cinema braam": case "Lightning": case "Boss warning": gain = 0.75f; break;
                case "Sniper shot": case "Machine gun": case "Taiko drums": case "Sad tuba": gain = 0.8f; break;
                case "Wrong buzzer": case "Car alarm": case "Burglar alarm": case "Lockdown": case "Smoke detector": gain = 0.65f; break;
                case "Siren deep": case "Double siren": case "Siren rise": case "Siren pulse": case "Wartime siren": case "Siren far": case "Black market deep": case "Underground": gain = 0.75f; break;
                case "Blowout siren": case "Psi storm": case "Gas mask": case "Electro anomaly": gain = 0.75f; break;
                case "Singing bowl": case "Zen bell": case "Cotton ping": case "Soft piano": case "Dream pad": case "Lullaby": case "Rain drops": case "Bubbles": case "Wind bells soft": case "Harp lullaby": case "Celesta": case "Feather chime": case "Soft heartbeat": case "Ocean wave": case "Birdsong": case "Soft keys": gain = 0.4f; break;
                case "Triple chime": case "Glass tap": case "Swoosh": case "Key tock": case "Plug-in chime": case "Unlock click": case "Success tick": case "Beam": case "Listening blip": case "Bloom": case "Low battery": case "Radar pulse": case "Glass chord": case "Ding": gain = 0.6f; break;
                case "Air raid siren": case "Evacuation horn": case "Lab alarm": case "Submarine dive": case "Fire bell": gain = 0.75f; break;
                case "Bullet on armor": case "Armor ricochet": case "Ceramic plate": case "Armor tank": gain = 0.8f; break;
                case "Raid horn": case "Flashbang": case "Klaxon": case "Keycard denied": gain = 0.7f; break;
                case "Suppressed shot": gain = 0.8f; break;
                case "Pistol shot": gain = 0.7f; break;
                case "Geiger": case "Radio squelch": case "Drum roll": case "Helicopter": gain = 0.75f; break;
            }
            switch (name)
            {
                case "Purr": case "Purr deep": case "Purr soft": case "Kitten purr": gain = 0.6f; break;
                case "Purr and prrt": gain = 0.7f; break;
                case "Beer bottle opening": case "Beer cap drop": case "Can opening": case "Beer cheers": gain = 0.8f; break;
                case "Owl double hoot": case "Owl deep": case "Owl night": case "Owl screech": case "Frog croak deep": case "Frog chorus": case "Tree frog": case "Frog bubble": case "Wolf howl long": case "Wolf short howl": case "Wolf pack": case "Wolf distant": case "Wolf growl": case "Wolf and owl": gain = 0.7f; break;
                case "Cat sleepy mew": case "Cat angry mew": case "Cat call": case "Kitten chirps": case "Purr and mew": case "Dog bark": case "Dog double bark": case "Puppy yip": case "Wolf howl": case "Owl hoot": case "Frog ribbit": case "Rooster": case "Sheep": case "Cricket": case "Crow caw": case "Dolphin": gain = 0.7f; break;
                case "Cat prrt": case "Cat double prrt": case "Cat chirp": case "Cat mew": case "Kitten mew": case "Cat trill up": case "Cat greeting": gain = 0.7f; break;
            }
            if (gain != 1f) for (int i = 0; i < fin.Length; i++) fin[i] *= gain;
            return fin;
        }
    }
}
