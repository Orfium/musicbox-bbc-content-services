using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace MusicManager.Core.ViewModules
{
	[XmlRoot(ElementName = "netmix")]
	public class BBCXmlMetadata
    {
		[XmlElement(ElementName = "FileName")]
		public string FileName { get; set; }
		[XmlElement(ElementName = "Album_ID")]
		public string AlbumID { get; set; }

		[XmlElement(ElementName = "Track_No")]
		public string TrackNo { get; set; }

		[XmlElement(ElementName = "Song_Title")]
		public string SongTitle { get; set; }

		[XmlElement(ElementName = "Artist_Name")]
		public string ArtistName { get; set; }

		[XmlElement(ElementName = "Album_Title")]
		public string AlbumTitle { get; set; }

		[XmlElement(ElementName = "Release_Year")]
		public string ReleaseYear { get; set; }

		[XmlElement(ElementName = "Comment")]
		public string Comment { get; set; }

		[XmlElement(ElementName = "ID3Genre")]
		public string ID3Genre { get; set; }

		[XmlElement(ElementName = "Product_Artist")]
		public string ProductArtist { get; set; }

		[XmlElement(ElementName = "Product_UPC")]
		public string ProductUPC { get; set; }

		[XmlElement(ElementName = "Product_Release_Year")]
		public string ProductReleaseYear { get; set; }

		[XmlElement(ElementName = "Track_ISRC")]
		public string TrackISRC { get; set; }

		[XmlElement(ElementName = "Source")]
		public string Source { get; set; }

		[XmlElement(ElementName = "Composer")]
		public string Composer { get; set; }

		[XmlElement(ElementName = "Publisher")]
		public string Publisher { get; set; }

		[XmlElement(ElementName = "Catalogue_No")]
		public string CatalogueNo { get; set; }

		[XmlElement(ElementName = "Style")]
		public string Style { get; set; }

		[XmlElement(ElementName = "Music_Styles")]
		public string MusicStyles { get; set; }

		[XmlElement(ElementName = "Subjects")]
		public string Subjects { get; set; }

		[XmlElement(ElementName = "Musical_Forms")]
		public string MusicalForms { get; set; }

		[XmlElement(ElementName = "Musical_Groups")]
		public string MusicalGroups { get; set; }

		[XmlElement(ElementName = "Instrument_Or_Voice")]
		public string InstrumentOrVoice { get; set; }

		[XmlElement(ElementName = "Category")]
		public string Category { get; set; }

		[XmlElement(ElementName = "Disc")]
		public int Disc { get; set; }

		[XmlElement(ElementName = "Product_Label")]
		public string ProductLabel { get; set; }

		[XmlElement(ElementName = "Music_For")]
		public string MusicFor { get; set; }

		[XmlElement(ElementName = "PG")]
		public string PG { get; set; }

		[XmlElement(ElementName = "TempoBPM")]
		public string TempoBPM { get; set; }
	}



		
	
}
