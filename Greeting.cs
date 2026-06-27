using System;
using System.IO;
using System.Media;

namespace Part_2
{
    public class Greeting
    {
        // Play voice greeting when the program starts
        public void PlayVoiceGreeting()
        {
            //Get the folder where the app is running
            string paths = AppDomain.CurrentDomain.BaseDirectory;
            string soundPath = paths;

            // Look for the bin folder
            int binIndex = paths.IndexOf(@"\bin\", StringComparison.OrdinalIgnoreCase);
            if (binIndex >= 0)
            {
                soundPath = paths.Substring(0, binIndex) + @"\voice_greeting.wav";
            }
            else
            {
                soundPath = Path.Combine(paths, "voice_greeting.wav");
            }

            try
            {
                //create sound player and play audio
                using (SoundPlayer player = new SoundPlayer(soundPath))
                {
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                // show error message
                System.Diagnostics.Debug.WriteLine($"Voice greeting could not be played: {ex.Message}. Path checked: {soundPath}");
            }
        }
    }
}