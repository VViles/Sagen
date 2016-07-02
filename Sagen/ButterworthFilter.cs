﻿using System;

namespace Sagen
{
	// This algorithm is an adaptation of the one found here: http://www.musicdsp.org/archive.php?classid=3#38
	public class ButterworthFilter
	{
		private double _resonance;
		private double _frequency;
		private int _sampleRate;
		private PassFilterType _filterType;
		private bool _changed = false;

		/// <summary>
		/// The resonance amount. Range: [0.1, sqrt(2)]
		/// </summary>
		public double Resonance
		{
			get { return _resonance; }
			set
			{
				_resonance = value;
				_changed = true;
			}
		}

		/// <summary>
		/// The cutoff frequency.
		/// </summary>
		public double Frequency
		{
			get { return _frequency; }

			set
			{
				_frequency = value;
				_changed = true;
			}
		}

		/// <summary>
		/// The sampling rate.
		/// </summary>
		public int SampleRate
		{
			get { return _sampleRate; }
			set
			{
				_sampleRate = value;
				_changed = true;
			}
		}

		/// <summary>
		/// The filter type.
		/// </summary>
		public PassFilterType FilterType
		{
			get { return _filterType; }
			set
			{
				_filterType = value;
				_changed = true;
			}
		}

		private double c, cc, a1, a2, b1, b2;

		private double i0, i1;
		private double o0, o1;

		public ButterworthFilter(double frequency, int sampleRate, PassFilterType filterType, double resonance)
		{
			_resonance = resonance;
			_frequency = frequency;
			_sampleRate = sampleRate;
			_filterType = filterType;
			RecalculateConstants();
		}

		private void RecalculateConstants()
		{
			switch (_filterType)
			{
				case PassFilterType.LowPass:
					c = 1.0f / Math.Tan(Math.PI * _frequency / _sampleRate);
					cc = c * c;
					a1 = 1.0f / (1.0f + _resonance * c + cc);
					a2 = 2f * a1;
					b1 = 2.0f * (1.0f - cc) * a1;
					b2 = (1.0f - _resonance * c + cc) * a1;
					break;
				case PassFilterType.HighPass:
					c = Math.Tan(Math.PI * _frequency / _sampleRate);
					cc = c * c;
					a1 = 1.0f / (1.0f + _resonance * c + cc);
					a2 = -2f * a1;
					b1 = 2.0f * (cc - 1.0f) * a1;
					b2 = (1.0f - _resonance * c + cc) * a1;
					break;
			}
		}

		public void Update(double newInput)
		{
			if (_changed)
			{
				RecalculateConstants();
				_changed = false;
			}

			var output = a1 * newInput + a2 * i0 + a1 * i1 - b1 * o0 - b2 * o1;

			i1 = i0;
			i0 = newInput;
			o1 = o0;
			o0 = output;
		}

		public double Value => o0;
	}

	public enum PassFilterType
	{
		HighPass,
		LowPass,
	}
}