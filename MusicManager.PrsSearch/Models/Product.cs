using ProductsServiceReference;
using System.Collections.Generic;


namespace MusicManager.PrsSearch.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string[] CatalogueNumbers { get; set; }
        public string Label { get; set; }
        public string RecordCompany { get; set; }
        public bool? IsCompilation { get; set; }
        public WebServiceProductSummaryBO arg { get; set; }


        public Soundmouse.Messaging.Model.Product ToSoundmouseProduct()
        {
            var product = new Soundmouse.Messaging.Model.Product
            {
                Artist = Artist,
                Identifiers = new Dictionary<string, string> {{"prs", ProductId.ToString()}},
                Name = Title
            };

            var catNoCount = 0;

            foreach (var c in CatalogueNumbers)
            {
                catNoCount++;

                var key = catNoCount == 1 ? "catalogue_number" : $"catalogue_number_{catNoCount}";
                product.Identifiers[key] = c;
            }

            return product;
        }

        public override string ToString()
        {
            return Title;
        }

        public Soundmouse.Messaging.Model.Track ToTrack(Soundmouse.Messaging.Model.Track track)
        {
            track.TrackData.Product = ToSoundmouseProduct();

            if (!string.IsNullOrEmpty(Label))
            {
                track.TrackData.InterestedParties = new List<Soundmouse.Messaging.Model.InterestedParty>();
                var label = new Soundmouse.Messaging.Model.InterestedParty { FullName = Label, Role = "record_label"};
                track.TrackData.InterestedParties.Add(label);
            }

            return track;
        }
    }
}