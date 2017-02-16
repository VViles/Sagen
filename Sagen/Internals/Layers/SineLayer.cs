﻿using System;

namespace Sagen.Internals.Layers
{
    internal class SineLayer : Layer
    {
        private double stateA, stateB;
        private const double FullPhase = Math.PI * 2.0;

        /// <summary>
        /// Frequency A in Hertz.
        /// </summary>
        public double FrequencyA { get; set; } = 697.0;

		/// <summary>
		/// Frequency B in Hertz.
		/// </summary>
		public double FrequencyB { get; set; } = 1209.0;

        /// <summary>
        /// The amplitude of the tone.
        /// </summary>
        public double Amplitude { get; set; } = 0.5;

        /// <summary>
        /// The phase offset of the tone.
        /// </summary>
        public double Phase { get; } = 0.0f;

        /// <summary>
        /// The DC (vertical) offset of the wave.
        /// </summary>
        public double DCOffset { get; set; } = 0.0f;

        public SineLayer(Synthesizer synth) : base(synth)
        {
            
        }

        public SineLayer(Synthesizer synth, double fa, double fb, double amplitude, double phase = 0.0f, double dcOffset = 0.0f) : base(synth)
        {
            FrequencyA = fa;
			FrequencyB = fb;
            Amplitude = amplitude;
            Phase = phase;
            stateA = phase;
			stateB = phase;
            DCOffset = dcOffset;
        }

        public override void Update(ref double sample)
        {
            stateA = (stateA + synth.TimeStep * FrequencyA) % 1.0f;
			stateB = (stateB + synth.TimeStep * FrequencyB) % 1.0f;
            sample += (FrequencyA > 0 ? (Math.Sin(stateA * FullPhase) * Amplitude) : 0) + (FrequencyB > 0 ? (Math.Sin(stateB * FullPhase) * Amplitude) : 0) + DCOffset;
        }
    }
}