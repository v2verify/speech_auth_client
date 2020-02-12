using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic.Speech.Authorization
{
    public enum SpeechAudioChannel
    {
        [Description("Mono")]
        Mono,

        [Description("Stereo Left Channel")]
        StereoLeft,

        [Description("Stereo Right Channel")]
        StereoRight
    }
}
