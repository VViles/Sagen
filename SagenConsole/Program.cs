﻿using System;

using Sagen;
using Sagen.Playback.OpenAL;

namespace SagenConsole
{
	class Program
	{
		private const string TestString = "playground";

		static void Main(string[] args)
		{
			var tts = new TTS(Voice.Jimmy);

			Console.WriteLine("Playing...");
#if DEBUG
			tts.SpeakToFile("speech.wav", TestString);
#endif
			tts.Speak<ALPlaybackEngine>(TestString);

			tts.Sync();

			Console.WriteLine("Done. Press any key to exit.");
			Console.ReadKey();
		}
	}
}
