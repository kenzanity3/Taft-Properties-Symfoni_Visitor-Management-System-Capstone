using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

public class LocalImageService
{
    //Function to provide location of folder wwwroot when running application
    private readonly IWebHostEnvironment _environment;
    //Set Location of Image upload to wwwroot/uploads/profile-pictures
    private const string UploadsFolder = "uploads/profile-pictures";
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    //Constructor 
    public LocalImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string?> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            throw new Exception($"File size exceeds maximum limit of {MaxFileSize} bytes.");
        }

        // Validate file extension
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".jfif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!validExtensions.Contains(fileExtension))
        {
            throw new Exception("Invalid file type. Only JPG, JPEG, JFIF and PNG are allowed.");
        }

        try
        {
            // Create uploads directory if it doesn't exist
            if (string.IsNullOrEmpty(_environment.WebRootPath))
                throw new Exception("WebRootPath is not configured. Make sure wwwroot folder exists.");

            var uploadsPath = Path.Combine(_environment.WebRootPath, UploadsFolder);
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            // Save the file
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return relative path
            return $"/{UploadsFolder}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to upload image.", ex);
        }
    }

    public void DeleteImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !imagePath.StartsWith($"/{UploadsFolder}/"))
            return;

        var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}