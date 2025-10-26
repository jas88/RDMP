/*
 * From http://www.techmikael.com/2009/07/removing-exif-data-continued.html
 * */

using System;
using System.IO;

namespace DrsPlugin.Attachers;

/// <summary>
/// Interface for image patching operations
/// </summary>
public interface IImagePatcher
{
    Stream PatchAwayExif(Stream inStream, Stream outStream);
    byte[] ReadPixelData(Stream stream);
}