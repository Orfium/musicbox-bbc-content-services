using Soundmouse.Messaging.Model;

namespace MusicManager.Application.WebService
{
    public interface IProduct
    {
        Track[] GetProductByTitle(string title);
        Track[] GetProductByCatNo(string catNo);
        Track[] GetProductByTuneCode(string tuneCode);
        Track[] GetProductByRecordingId(int recordingId);
    }
}
