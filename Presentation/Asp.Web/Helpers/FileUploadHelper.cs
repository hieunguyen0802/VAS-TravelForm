using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace src.Web.Helpers
{
    public class FileUploadHelper
    {
        public async Task<string> SaveFileAsync(IFormFile file, string pathToUpload, string prefixName)
        {
            string imageUrl = string.Empty;
            if (!Directory.Exists(pathToUpload))
                System.IO.Directory.CreateDirectory(pathToUpload); //Create Path of not exists
            string pathwithfileName = pathToUpload + "\\" + GetFileName(file, true, prefixName);
            using (var fileStream = new FileStream(pathwithfileName, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            imageUrl = pathwithfileName;
            return imageUrl;
        }
        public string SaveFile(IFormFile file, string pathToUplaod,string prefixName)
        {
            string imageUrl = string.Empty;
            string pathwithfileName = pathToUplaod + "\\" + GetFileName(file, true, prefixName);
            using (var fileStream = new FileStream(pathwithfileName, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            return imageUrl;
        }
        public string GetFileName(IFormFile file, bool BuildUniqeName,string prefixName)
        {
            string fileName = string.Empty;
            //string strFileName = file.FileName.Substring(
            //  file.FileName.LastIndexOf("\\"))
            // .Replace("\\", string.Empty);
            string fileExtension = GetFileExtension(file);
            if (BuildUniqeName)
            {
                string strUniqName = GetUniqueName(prefixName);
                fileName = strUniqName + fileExtension;
            }
            //else
            //{
            //    fileName = strFileName;
            //}
            return fileName;
        }
        public string GetUniqueName(string preFix)
        {
            string uName = preFix + DateTime.Now.ToString()
             .Replace("/", "-")
             .Replace(":", "-")
             .Replace(" ", string.Empty)
             .Replace("PM", string.Empty)
             .Replace("AM", string.Empty);
            return uName;
        }
        public string GetFileExtension(IFormFile file)
        {
            string fileExtension;
            fileExtension = (file != null) ?
             file.FileName.Substring(file.FileName.LastIndexOf('.')).ToLower() :
             string.Empty;
            return fileExtension;
        }
    }
}
