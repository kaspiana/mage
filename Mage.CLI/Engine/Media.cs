using System.Reflection;
using SkiaSharp;

namespace Mage.Engine;

public enum MediaType {
    Binary,
    Text,
    Image,
    Animation,
    Audio,
    Video
}

public class MediaMetadata {
    public virtual MediaType mediaType { get => MediaType.Binary; }
}

public class MediaMetadataBinary : MediaMetadata {
    override public MediaType mediaType { get => MediaType.Binary; }
}

public class MediaMetadataText : MediaMetadata {
    override public MediaType mediaType { get => MediaType.Text; }
}

public class MediaMetadataImage : MediaMetadata {
    override public MediaType mediaType { get => MediaType.Image; }
    public int width;
    public int height;
}

public class MediaMetadataAudio : MediaMetadata {
    override public MediaType mediaType { get => MediaType.Audio; }
    public int duration;
}

public class MediaMetadataVideo : MediaMetadata {
    override public MediaType mediaType { get => MediaType.Video; }
    public int width;
    public int height;
    public int duration;
}

public class MediaMetadataAnimation : MediaMetadataVideo {
    override public MediaType mediaType { get => MediaType.Animation; }
}

public class Media {

    public static MediaMetadata GetMediaType(string filePath){
        var fileExt = Path.GetExtension(filePath)[1..];
        switch(fileExt){
            default:
                return GetBinaryMetadata(filePath);
            break;

            case "txt" or "ini" or "log":
                return GetTextMetadata(filePath);
            break;

            case "png" or "jpg" or "jpeg" or "avif":
                return GetImageMetadata(filePath);
            break;

            case "webp" or "gif":
                return GetAnimatedImageMetadata(filePath);
            break;
            
            case "mp3" or "wav" or "ogg" or "m4a":
                return GetAudioMetadata(filePath);
            break;

            case "mp4" or "webm" or "mkv" or "mov" or "avi":
                return GetVideoMetadata(filePath);
            break;
        }
    }

    public static MediaMetadataBinary GetBinaryMetadata(string filePath){
        return new MediaMetadataBinary();
    }

    public static MediaMetadataText GetTextMetadata(string filePath){
        return new MediaMetadataText();
    }

    public static MediaMetadataImage GetImageMetadata(string filePath){

        using var buf = File.OpenRead(filePath);
        var bmHeader = SKBitmap.DecodeBounds(buf);

        return new MediaMetadataImage(){
            width = bmHeader.Width,
            height = bmHeader.Height
        };
    }

    public static MediaMetadata GetAnimatedImageMetadata(string filePath){

        using var buf = File.OpenRead(filePath);
        using var codec = SKCodec.Create(buf);

        if(codec.FrameCount == 0){
            return new MediaMetadataImage(){
                width = codec.Info.Width,
                height = codec.Info.Height
            };
        }

        return new MediaMetadataAnimation(){
            width = codec.Info.Width,
            height = codec.Info.Height,
            duration = Math.Max(1, codec.FrameInfo.Sum(fi => fi.Duration) / 1000)
        };
    }

    public static MediaMetadataAudio GetAudioMetadata(string filePath){
        return new MediaMetadataAudio(){
            duration = 0
        };
    }

    public static MediaMetadataVideo GetVideoMetadata(string filePath){
        return new MediaMetadataAnimation(){
            width = 0,
            height = 0,
            duration = 0
        };
    }
    
}