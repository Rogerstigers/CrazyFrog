using Catnap.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json;

namespace CrazyFrog
{
    internal class ControlPanelController : Controller
    {
        public AudioMonitor audioMonitor { get; set; }

        [HttpGet]
        [Route]
        public HttpResponse Get()
        {
            return new HttpResponse(HttpStatusCode.Ok, File.ReadAllText("Assets\\ControlPanel.html"));
        }

        [HttpGet]
        [Route("/status")]
        public HttpResponse GetStatus()
        {
            var retval = new UpdateRequest()
            {
                Enabled = audioMonitor.ShouldReactToAudio,
                EnablePhotoSwitch = audioMonitor.EnablePV,
                UrlToAudio = audioMonitor.AudioFileUrl,
                Volume = audioMonitor.TriggerLevel
            };
            return new HttpResponse(HttpStatusCode.Ok, JsonConvert.SerializeObject(retval));
        }

        [HttpGet]
        [Route("{filename}")]
        public HttpResponse Get(string filename)
        {
            try
            {
                return new HttpResponse(HttpStatusCode.Ok, File.ReadAllText(string.Format("Assets\\{0}", filename)));
            }
            catch (FileNotFoundException)
            {
                return new HttpResponse(HttpStatusCode.NotFound);
            }            
        }

        [HttpPost]
        [Route]
        public HttpResponse JsonPost([JsonBody]string body)
        {
            var request = JsonConvert.DeserializeObject<UpdateRequest>(body);
            audioMonitor.AudioFileUrl = request.UrlToAudio;
            audioMonitor.ShouldReactToAudio = request.Enabled;
            audioMonitor.TriggerLevel = request.Volume;
            audioMonitor.EnablePV = request.EnablePhotoSwitch;

            return new JsonResponse(body);
        }
    }
}
