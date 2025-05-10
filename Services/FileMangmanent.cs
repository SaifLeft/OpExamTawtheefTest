using TawtheefTest.DTOs.Common;
using TawtheefTest.Enums;

namespace ITAM.Service
{
  public interface IFileMangmanent
  {
    FileRespoesDTO GetFileByName(string fileName, ContentSourceType type);
    FileRespoesDTO SaveFile(IFormFile file, ContentSourceType type);
  }
  public class FileMangmanent : IFileMangmanent
  {
    private readonly IConfiguration _configuration;
    private readonly string? _FilesLocationPath = string.Empty;
    private readonly IWebHostEnvironment _env;
    private ILogger<FileMangmanent> _logger;


    public FileMangmanent(IConfiguration configuration, IWebHostEnvironment env, ILogger<FileMangmanent> logger)
    {
      _configuration = configuration;
      _FilesLocationPath = _configuration.GetSection("AppSettings:FilesLocationPath").Value;
      _env = env;
      _logger = logger;
    }
    public FileRespoesDTO SaveFile(IFormFile file, ContentSourceType type)
    {
      try
      {
        // Validate if file is provided
        if (file == null || file.Length == 0)
        {
          return new FileRespoesDTO
          {
            IsSuccess = false,
            Message = "File is required"
          };
        }

        // Determine base path - use project location if null
        var basePath = string.IsNullOrEmpty(_FilesLocationPath) ?
            Path.Combine(_env.ContentRootPath, "Files") : _FilesLocationPath;

        // Construct directory path based on type
        var directoryPath = Path.Combine(basePath, type.ToString());

        // Ensure directory exists
        if (!Directory.Exists(directoryPath))
        {
          Directory.CreateDirectory(directoryPath);
        }

        // Generate a unique file name using GUID
        var fileExtension = Path.GetExtension(file.FileName);
        var newFileName = Guid.NewGuid().ToString() + fileExtension;
        var filePath = Path.Combine(directoryPath, newFileName);

        // Save file to specified path
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          file.CopyTo(stream);
        }

        // Generate URL for accessing the file
        var baseUrl = _configuration.GetSection("AppSettings:FilesLocationURL").Value ?? "http://localhost/files/";
        var fileUrl = Path.Combine(baseUrl, type.ToString(), newFileName).Replace("\\", "/");

        return new FileRespoesDTO
        {
          IsSuccess = true,
          Message = "File saved successfully",
          FileName = newFileName,
          FileExtension = fileExtension,
          Path = directoryPath,
          URL = fileUrl,
          FileBytes = File.ReadAllBytes(filePath)
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, ex.Message);
        return new FileRespoesDTO
        {
          IsSuccess = false,
          Message = "An error occurred while saving the file: " + ex.Message
        };
      }
    }


    public FileRespoesDTO GetFileByName(string fileName, ContentSourceType type)
    {
      try
      {
        // Determine base path - use project location if null
        var basePath = string.IsNullOrEmpty(_FilesLocationPath) ?
            Path.Combine(_env.ContentRootPath, "Files") : _FilesLocationPath;

        // Construct directory path based on type
        var directoryPath = Path.Combine(basePath, type.ToString());

        // Construct full file path
        var filePath = Path.Combine(directoryPath, fileName);

        // Check if file exists
        if (!File.Exists(filePath))
        {
          return new FileRespoesDTO
          {
            IsSuccess = false,
            Message = "لم يتم العثور على الملف"
          };
        }

        // Read file bytes
        var fileBytes = File.ReadAllBytes(filePath);

        // TODO: Add file content type to the response like image/png, image/jpeg, application/pdf, etc.
        string FileContentType = "application/pdf";
        if (Path.GetExtension(fileName) == ".png" || Path.GetExtension(fileName) == ".jpg" || Path.GetExtension(fileName) == ".jpeg")
        {
          FileContentType = "image/" + Path.GetExtension(fileName);
        }
        else if (Path.GetExtension(fileName) == ".pdf")
        {
          FileContentType = "application/pdf";
        }
        else if (Path.GetExtension(fileName) == ".docx")
        {
          FileContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        }
        else if (Path.GetExtension(fileName) == ".xlsx")
        {
          FileContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }
        else if (Path.GetExtension(fileName) == ".pptx")
        {
          FileContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        }
        else if (Path.GetExtension(fileName) == ".txt")
        {
          FileContentType = "text/plain";
        }
        else if (Path.GetExtension(fileName) == ".csv")
        {
          FileContentType = "text/csv";
        }
        else if (Path.GetExtension(fileName) == ".json")
        {
          FileContentType = "application/json";
        }
        else if (Path.GetExtension(fileName) == ".zip")
        {

          FileContentType = "application/zip";
        }
        else if (Path.GetExtension(fileName) == ".rar")
        {
          FileContentType = "application/x-rar-compressed";
        }
        else
        {
          FileContentType = "application/octet-stream";
        }

        var res = new FileRespoesDTO
        {
          IsSuccess = true,
          Message = "تم استرجاع الملف بنجاح",
          FileName = fileName,
          Path = directoryPath,
          FileBytes = fileBytes,
          FileExtension = Path.GetExtension(fileName),
          FileContentType = FileContentType
        };

        if (type == ContentSourceType.Document)
        {
          res.Text = File.ReadAllText(filePath);
        }

        return res;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, ex.Message);
        return null;
      }
    }






  }
}
