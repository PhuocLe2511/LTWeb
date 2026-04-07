namespace SV22T1020330.Admin
{
    /// <summary>
    /// Helper class for resolving media file paths
    /// </summary>
    public static class MediaPaths
    {
        /// <summary>
        /// Resolve the root path for media files
        /// </summary>
        /// <param name="env">Web host environment</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Root path for media files</returns>
        public static string ResolveRoot(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            // Default to wwwroot/images
            string mediaRoot = Path.Combine(env.WebRootPath, "images");
            
            // Check if there's a custom media path in configuration
            string? customPath = configuration["Media:RootPath"];
            if (!string.IsNullOrWhiteSpace(customPath))
            {
                mediaRoot = customPath;
                // If relative path, combine with web root
                if (!Path.IsPathRooted(customPath))
                {
                    mediaRoot = Path.Combine(env.WebRootPath, customPath);
                }
            }
            
            return mediaRoot;
        }
        
        /// <summary>
        /// Get the URL for a media file
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="subfolder">Subfolder (e.g., "products", "employees")</param>
        /// <returns>URL to access the file</returns>
        public static string GetFileUrl(string fileName, string subfolder = "")
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "/images/no-image.png";
                
            string path = "/images";
            if (!string.IsNullOrWhiteSpace(subfolder))
                path += $"/{subfolder}";
            path += $"/{fileName}";
            
            return path;
        }
    }
}
