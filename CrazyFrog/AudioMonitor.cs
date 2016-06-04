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
        private const int PV_PIN = 26;
        private GpioPin ledPin;
        private GpioPin pvPin;
        private MCP3008 _adc = new MCP3008();
        private bool isConnected = false;
        private ThreadPoolTimer timer;
        private GpioPinValue LEDOffValue = GpioPinValue.Low;
        private GpioPinValue LEDOnValue = GpioPinValue.High;
        private bool AudioPlaying = false;
        private MediaEngine mediaEngine;
        private bool PVTriggered = false;
        private int PVTriggerThreshold = 1000;


        public string AudioFileUrl { get; set; }

        public bool ShouldReactToAudio { get; set; } = true;

        public int TriggerLevel { get; set; } = 70;

        public bool EnablePV { get; set; } = false;
        
        private async void Timer_Tick(ThreadPoolTimer timer)
        {
            timer.Cancel();

            await ReadPV();

            await ReactToAudio();

            ResetTimer();
        }

        private void MediaEngine_MediaStateChanged(MediaState state)
        {
            if (state == MediaState.Ended || state == MediaState.Stopped || state == MediaState.Error)
            {
                TurnOffLED();
                AudioPlaying = false;
            }
            else
            {
                TurnOnLED();
                AudioPlaying = true;
            }
        }

        private async Task ReadPV() {
            if (!EnablePV)
                PVTriggered = false;
            else
            {
                pvPin.SetDriveMode(GpioPinDriveMode.Output);
                pvPin.Write(GpioPinValue.Low);
                await Task.Delay(100);
                pvPin.SetDriveMode(GpioPinDriveMode.Input);

                int loopCount = 0;
                while (pvPin.Read() == GpioPinValue.Low)
                {
                    loopCount++;
                }

                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(loopCount);
                }
                PVTriggered = (loopCount < PVTriggerThreshold);
            }

        }

        private async Task ReactToAudio()
        {
            if (!AudioPlaying && ShouldReactToAudio && !PVTriggered)
            {
                int volume = await _adc.SampleAsync(1000, 0, 100);

                if (volume >= TriggerLevel)
                {
                    PlayAudio();
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

        private void TurnOnLED() { if (ledPin.Read() != LEDOnValue) ledPin.Write(LEDOnValue); }
        private void TurnOffLED() { if (ledPin.Read() != LEDOffValue) ledPin.Write(LEDOffValue); }

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
            ledPin = GpioController.GetDefault().OpenPin(LED_PIN);
            TurnOffLED();
            ledPin.SetDriveMode(GpioPinDriveMode.Output);

            //Initialize the PV Circuit
            pvPin = GpioController.GetDefault().OpenPin(PV_PIN);

            //Initialize the ADC
            isConnected = await _adc.ConnectAsync();

            ResetTimer();
        }
    }
}
