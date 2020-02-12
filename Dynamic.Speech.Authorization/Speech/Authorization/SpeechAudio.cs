
using System.ComponentModel;
using System.IO;

using NAudio.Wave;

namespace Dynamic.Speech.Authorization
{
    public sealed class SpeechAudio : ISpeechElement
    {
        public WaveStream Stream { get; set; }
        public SpeechAudioChannel Channel { get; set; }

        private WaveFormat Format { get; set; }
        private IWaveProvider Provider { get; set; }
        
        public string FileName { get; internal set; }

        public SpeechAudio()
        {
            Stream = null;
            FileName = "";
            Channel = SpeechAudioChannel.Mono;
        }

        public SpeechAudio(WaveStream stream, SpeechAudioChannel channel = SpeechAudioChannel.Mono)
            : this(stream, "unknown.wav", channel)
        {
        }

        public SpeechAudio(WaveStream stream, WaveFormat format, SpeechAudioChannel channel = SpeechAudioChannel.Mono)
            : this(stream, "unknown.wav", channel)
        {
            if (!Stream.WaveFormat.Equals(format))
            {
                Format = format;
                Provider = new MediaFoundationResampler(Stream, format);
            }
        }

        public SpeechAudio(WaveStream stream, string filename, SpeechAudioChannel channel = SpeechAudioChannel.Mono)
        {
            Stream = stream;
            FileName = filename;
            Channel = channel;
        }

        public SpeechAudio(string filename, SpeechAudioChannel channel = SpeechAudioChannel.Mono)
        {
            FileName = Path.GetFileName(filename);
            Stream = new WaveFileReader(filename);
            Channel = channel;
        }

        public SpeechAudio(string filename, WaveFormat format, SpeechAudioChannel channel = SpeechAudioChannel.Mono)
        {
            FileName = Path.GetFileName(filename);
            Stream = new WaveFileReader(filename);
            Channel = channel;

            if (!Stream.WaveFormat.Equals(format))
            {
                Format = format;
                Provider = new MediaFoundationResampler(Stream, format);
            }
        }
        
        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
