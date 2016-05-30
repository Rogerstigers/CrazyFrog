using Microsoft.Maker.Media.UniversalMediaEngine;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Xaml.Controls;

namespace CrazyFrog
{
    public sealed class AudioMonitor
    {
        private const int LED_PIN = 5;
        private GpioPin pin;
        private MCP3008 _adc = new MCP3008();
        private bool isConnected = false;
        private ThreadPoolTimer timer;
        private GpioPinValue LEDOffValue = GpioPinValue.Low;
        private GpioPinValue LEDOnValue = GpioPinValue.High;
        private bool AudioPlaying = false;
        private MediaEngine mediaEngine;

        public string AudioFileUrl { get; set; }

        public bool ShouldReactToAudio { get; set; } = true;

        public int TriggerLevel { get; set; } = 70;

        private async void Timer_Tick(ThreadPoolTimer timer)
        {
            timer.Cancel();

            await ReactToAudio();

            ResetTimer();
        }

        private void MediaEngine_MediaStateChanged(MediaState state)
        {
            if (state == MediaState.Ended || state == MediaState.Stopped || state == MediaState.Error)
            {
                AudioPlaying = false;
            }
            else
            {
                AudioPlaying = true;
            }
        }

        private async Task ReactToAudio()
        {
            if (!AudioPlaying && ShouldReactToAudio)
            {
                int volume = await _adc.SampleAsync(1000, 0, 100);
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(volume);
                }

                if (volume >= TriggerLevel)
                {
                    pin.Write(LEDOnValue);
                    PlayAudio();
                }
                else
                {
                    pin.Write(LEDOffValue);
                }
            }
        }

        private void ResetTimer()
        {
            timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(250));
        }

        private void PlayAudio()
        {
            mediaEngine.Play(AudioFileUrl);
        }

        private async void ToggleLED(int blinkLength = 250)
        {
            pin.Write(LEDOnValue);
            await Task.Delay(blinkLength);
            pin.Write(LEDOffValue);
            await Task.Delay(blinkLength);
        }

        public async void Init()
        {
            //Initialize the MediaEngine
            mediaEngine = new MediaEngine();
            mediaEngine.MediaStateChanged += MediaEngine_MediaStateChanged;
            var result = await this.mediaEngine.InitializeAsync();
            if (result == MediaEngineInitializationResult.Fail)
            {
                // Your error logic           
            }

            //Initialize the LED Circuits
            pin = GpioController.GetDefault().OpenPin(LED_PIN);
            pin.Write(LEDOffValue);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            //Initialize the ADC
            isConnected = await _adc.ConnectAsync();

            ResetTimer();
        }
    }
}
