using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicManager.Core.ViewModules
{

    public class RequestMessage
    {
        public Guid buildId { get; set; }
        public List<PlayoutChannel> channels { get; set; }
        public List<PlayoutTrack> tracks { get; set; }
    }

    public class PlayoutChannel
    {
        public string name { get; set; }
        public string deliveryLocation { get; set; }
    }

    public class PlayoutTrack
    {
        public long id { get; set; }
        public Guid trackId { get; set; }
        public Guid mlTrackId { get; set; }
        public string type { get; set; }
        public List<string> prsPublishers { get; set; }
    }


    public class ResponseMessage
    {
        public Guid buildId { get; set; }
        public Guid requestId { get; set; }
        public string channel { get; set; }
        public string status { get; set; }
        public string id { get; set; }
        public string sentBy { get; set; }
    }

    #region --- Job Delivery Request -----
    public class PlayoutDeliveryRequest
    {
        public PlayoutDelivery request { get; set; }      
        public PlayoutDeliveryCallbackOptions callbackOptions { get; set; }
    }

    public class PlayoutDelivery
    {
        public Guid id { get; set; }
        public string deliveryLocation { get; set; }
        public List<PlayoutDeliveryTrackFiles> trackFiles { get; set; }      
    }

    public class PlayoutDeliveryTrackFiles
    {
        public long id { get; set; }
        public Guid trackId { get; set; }
        public string wav { get; set; }
        public string signedWav { get; set; }
        public string xml { get; set; }
        public Stream xmlStream { get; set; }
    }

    public class PlayoutDeliveryCallbackOptions
    {        
        public string mode { get; set; }
        public string location { get; set; }
    }
    #endregion

    #region --- Job Delivery Response -----

    public class PlayoutDeliveryResponseBody
    {
        public Guid id { get; set; }
        public string sentBy { get; set; }
        public PlayoutDeliveryResponse job { get; set; }
    }

    public class PlayoutDeliveryResponse
    {
        public Guid id { get; set; }
        public DateTime dateModified { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public DeliveryResponse response { get; set; }
        public PlayoutDelivery request { get; set; }
        public PlayoutDeliveryCallbackOptions callbackOptions { get; set; }
        public bool callbackSent { get; set; }
        public string uri { get; set; }
    }

    public class DeliveryResponse
    {
        public Guid id  { get; set; }
        public string status { get; set; }
    }
    #endregion
}
