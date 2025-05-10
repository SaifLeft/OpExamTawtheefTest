namespace TawtheefTest.DTOs.Common
{
  public class FileRespoesDTO
  {
    public string FileName { get; set; }
    public string Path { get; set; }
    public string URL { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public byte[] FileBytes { get; set; }
    public string FileExtension { get; set; }
    public string Text { get; set; }
    public string FileContentType { get; internal set; }
  }
}
