using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace MusicManager.Core.ViewModules
{
    [XmlRoot(ElementName = "AudioMetadata")]
    public class XMLMetadata
    {
        [XmlElement(ElementName = "SourceFile")]
        public SourceFile SourceFile { get; set; }
        [XmlElement(ElementName = "ConvertedFile")]
        public ConvertedFile ConvertedFile { get; set; }
    }

	[XmlRoot(ElementName = "AudioFormat")]
	public class AudioFormat
	{
		[XmlElement(ElementName = "Channels")]
		public string Channels { get; set; }
		[XmlElement(ElementName = "SamplesPerSec")]
		public string SamplesPerSec { get; set; }
		[XmlElement(ElementName = "BitsPerSample")]
		public string BitsPerSample { get; set; }
		[XmlElement(ElementName = "AvgBytesPerSec")]
		public string AvgBytesPerSec { get; set; }
	}

	[XmlRoot(ElementName = "SourceFile")]
	public class SourceFile
	{
		[XmlElement(ElementName = "FileName")]
		public string FileName { get; set; }
		[XmlElement(ElementName = "FileNameNoExt")]
		public string FileNameNoExt { get; set; }
		[XmlElement(ElementName = "FileNameExt")]
		public string FileNameExt { get; set; }
		[XmlElement(ElementName = "FileNamePath")]
		public string FileNamePath { get; set; }
		[XmlElement(ElementName = "FileSize")]
		public string FileSize { get; set; }
		[XmlElement(ElementName = "FileLength")]
		public string FileLength { get; set; }
		[XmlElement(ElementName = "FileBitrate")]
		public string FileBitrate { get; set; }
		[XmlElement(ElementName = "FileCreated")]
		public string FileCreated { get; set; }
		[XmlElement(ElementName = "FileModified")]
		public string FileModified { get; set; }
		[XmlElement(ElementName = "FileAccessed")]
		public string FileAccessed { get; set; }
		[XmlElement(ElementName = "AudioProperties")]
		public AudioProperties AudioProperties { get; set; } // Datatype was String
		[XmlElement(ElementName = "AudioFormat")]
		public AudioFormat AudioFormat { get; set; }
		[XmlElement(ElementName = "IDTags")]
		public IDTags IDTags { get; set; } // Datatype was String
	}

	[XmlRoot(ElementName = "AudioProperties")]
	public class AudioProperties
	{
		[XmlElement(ElementName = "AudioQuality")]
		public string AudioQuality { get; set; }
		[XmlElement(ElementName = "Encoder")]
		public string Encoder { get; set; }
		[XmlElement(ElementName = "SampleCount")]
		public string SampleCount { get; set; }
		[XmlElement(ElementName = "IDTag")]
		public string IDTag { get; set; }
		[XmlElement(ElementName = "Contains")]
		public string Contains { get; set; }
		[XmlElement(ElementName = "Gapless")]
		public string Gapless { get; set; }
	}

	[XmlRoot(ElementName = "IDTags")]
	public class IDTags
	{
		[XmlElement(ElementName = "AccurateRipResult")]
		public string AccurateRipResult { get; set; }
		[XmlElement(ElementName = "AccurateRipDiscID")]
		public string AccurateRipDiscID { get; set; }
		[XmlElement(ElementName = "Title")]
		public string Title { get; set; }
		[XmlElement(ElementName = "Album")]
		public string Album { get; set; }
		[XmlElement(ElementName = "Year")]
		public string Year { get; set; }
		[XmlElement(ElementName = "Disc")]
		public string Disc { get; set; }
		[XmlElement(ElementName = "Label")]
		public string Label { get; set; }
		[XmlElement(ElementName = "AlbumArtist")]
		public string AlbumArtist { get; set; }
		[XmlElement(ElementName = "Genre")]
		public string Genre { get; set; }
		[XmlElement(ElementName = "Style")]
		public string Style { get; set; }
		[XmlElement(ElementName = "Artist")]
		public string Artist { get; set; }		
		[XmlElement(ElementName = "Composer")]
		public string Composer { get; set; }
		[XmlElement(ElementName = "Publisher")]
		public string Publisher { get; set; }
		[XmlElement(ElementName = "ArtistSort")]
		public string ArtistSort { get; set; }
		[XmlElement(ElementName = "AlbumArtistSort")]
		public string AlbumArtistSort { get; set; }
		[XmlElement(ElementName = "ISRC")]
		public string ISRC { get; set; }
		[XmlElement(ElementName = "Catalog")]
		public string Catalog { get; set; }
		[XmlElement(ElementName = "MBID")]
		public string MBID { get; set; }
		[XmlElement(ElementName = "BatchName")]
		public string BatchName { get; set; }
		[XmlElement(ElementName = "BatchID")]
		public string BatchID { get; set; }
		[XmlElement(ElementName = "BatchDiscNumber")]
		public string BatchDiscNumber { get; set; }
		[XmlElement(ElementName = "MetaProvider")]
		public string MetaProvider { get; set; }
		[XmlElement(ElementName = "Track")]
		public string Track { get; set; }
		[XmlElement(ElementName = "Profile")]
		public string Profile { get; set; }
		[XmlElement(ElementName = "Compilation")]
		public string Compilation { get; set; }
		[XmlElement(ElementName = "Source")]
		public string Source { get; set; }
		[XmlElement(ElementName = "Length")]
		public string Length { get; set; }
		[XmlElement(ElementName = "CDDBDiscID")]
		public string CDDBDiscID { get; set; }
		[XmlElement(ElementName = "EncodedBy")]
		public string EncodedBy { get; set; }
		[XmlElement(ElementName = "Encoder")]
		public string Encoder { get; set; }
		[XmlElement(ElementName = "EncoderSettings")]
		public string EncoderSettings { get; set; }
		[XmlElement(ElementName = "UPC")]
		public string UPC { get; set; }
		[XmlElement(ElementName = "BBCBarcode")]
		public string BBCBarcode { get; set; }
	}

	[XmlRoot(ElementName = "ConvertedFile")]
	public class ConvertedFile
	{
		[XmlElement(ElementName = "FileName")]
		public string FileName { get; set; }
		[XmlElement(ElementName = "FileNameNoExt")]
		public string FileNameNoExt { get; set; }
		[XmlElement(ElementName = "FileNameExt")]
		public string FileNameExt { get; set; }
		[XmlElement(ElementName = "FileNamePath")]
		public string FileNamePath { get; set; }
		[XmlElement(ElementName = "FileSize")]
		public string FileSize { get; set; }
		[XmlElement(ElementName = "FileLength")]
		public string FileLength { get; set; }
		[XmlElement(ElementName = "FileBitrate")]
		public string FileBitrate { get; set; }
		[XmlElement(ElementName = "FileCreated")]
		public string FileCreated { get; set; }
		[XmlElement(ElementName = "FileModified")]
		public string FileModified { get; set; }
		[XmlElement(ElementName = "FileAccessed")]
		public string FileAccessed { get; set; }
		[XmlElement(ElementName = "AudioProperties")]
		public AudioProperties AudioProperties { get; set; }
		[XmlElement(ElementName = "AudioFormat")]
		public AudioFormat AudioFormat { get; set; }
		[XmlElement(ElementName = "IDTags")]
		public IDTags IDTags { get; set; }
	}
}
