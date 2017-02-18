﻿using System.Collections.Generic;
using System.IO;
using System.Text;

using Sagen.Extensibility;
using Sagen.Internals.Layers;
using Sagen.Internals.Playback;
using System;

namespace Sagen.Internals
{
	internal class Synthesizer : ISynthesizer
	{
		public const int MaxSamplers = 128;
		public const SampleFormat PlaybackFormat = SampleFormat.Signed16;
		public const int PlaybackFormatBytes = (int)PlaybackFormat / 8;
		private const double StreamChunkDurationSeconds = 0.1;

		private readonly HashSet<Layer> samplers = new HashSet<Layer>();
		private readonly List<Layer> samplerSequence = new List<Layer>();
		private readonly int _sampleRate = (int)TTS.Quality;
		private readonly VoiceQuality _quality = TTS.Quality;
		private readonly Voice _voice;
		private readonly TTS _tts;
		private AudioStream _audioStream;
		private int _position = 0;

		/// <summary>
		/// Pitch, measured in relative octaves.
		/// </summary>
		public double Pitch { get; set; } = 0.0f;

		/// <summary>
		/// Fundamental frequency calculated from gender, pitch, and intonation.
		/// </summary>
		public double Fundamental { get; set; } = 100.0f;

		/// <summary>
		/// The time, in seconds, already elapsed before the current sample
		/// </summary>
		public double TimePosition => (double)_position / _sampleRate;

		/// <summary>
		/// The amount of time, in seconds, elapsed per sample
		/// </summary>
		public double TimeStep { get; }

		public Voice Voice => _tts.Voice;

		internal TTS TTS => _tts;

		public AudioStream AudioStream => _audioStream;

		public Synthesizer(TTS engine)
		{
			TimeStep = 1.0f / _sampleRate;
			_tts = engine;
			_voice = engine.Voice;
		}

		/// <summary>
		/// This method is used to create the audio stream and its accompanying ManualResetEventSlim ahead of time so that TTS.Sync works properly with multithreading.
		/// </summary>
		public void CreateAudioStream()
		{
			if (_audioStream == null)
			{
				_audioStream = new AudioStreamAL(PlaybackFormat, this);
			}
		}

		public int SampleRate => _sampleRate;

		public VoiceQuality Quality => _quality;

		public int Position
		{
			get { return _position; }
			set { _position = value; }
		}

		public void AddSampler(Layer sampler)
		{
			if (sampler != null && samplers.Add(sampler))
			{
				samplerSequence.Add(sampler);
			}
		}

		public void Generate(double lengthSeconds, Stream output, SampleFormat format, bool includeWavHeader = true)
		{
			using (var writer = new BinaryWriter(output, Encoding.Default, true))
			{
				int sampleCount = (int)(SampleRate * lengthSeconds);

				if (includeWavHeader)
					Wav.GenerateWavHeader(this, output, sampleCount, format);

				for (_position = 0; _position < sampleCount; _position++)
				{
					double currentSample = 0f;
					foreach (var sampler in samplerSequence)
					{
						if (!sampler.Enabled) continue;
						sampler.Update(ref currentSample);
					}
					switch (format)
					{
						case SampleFormat.Float64:
						writer.Write(currentSample);
						break;
						case SampleFormat.Float32:
						writer.Write((float)currentSample);
						break;
						case SampleFormat.Signed16:
						writer.Write((short)(currentSample * short.MaxValue));
						break;
						case SampleFormat.Unsigned8:
						writer.Write((byte)((currentSample + 1.0f) / 2.0f * byte.MaxValue));
						break;
					}
				}
				writer.Flush();
			}
		}

		public void Play(double lengthSeconds)
		{
			CreateAudioStream();
			int blockSize = (int)(SampleRate * StreamChunkDurationSeconds) * PlaybackFormatBytes;
			int sampleCount = (int)(SampleRate * lengthSeconds);
			short[] data = new short[blockSize];
			int len = 0;

			for (_position = 0; _position < sampleCount; _position++)
			{
				double currentSample = 0f;
				foreach (var sampler in samplerSequence)
				{
					if (!sampler.Enabled) continue;
					sampler.Update(ref currentSample);
				}

				data[len++] = (short)(currentSample * short.MaxValue);

				if (len >= blockSize)
				{
					_audioStream.QueueDataBlock(data, len);
					len = 0;
				}
			}
			
			if (len > 0) _audioStream.QueueDataBlock(data, len);
			_audioStream.Cleanup();
		}
	}
}