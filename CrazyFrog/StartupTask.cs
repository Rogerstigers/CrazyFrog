using Catnap.Server;
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
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        AudioMonitor audioMonitor;
        HttpServer webServer;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            audioMonitor = new AudioMonitor();
            audioMonitor.AudioFileUrl = "http://soundbible.com/grab.php?id=1864&type=mp3";
            audioMonitor.Init();

            webServer = new HttpServer(80);
            webServer.restHandler.RegisterController(new ControlPanelController() { audioMonitor = audioMonitor });
            webServer.StartServer();
        }
    }
}
