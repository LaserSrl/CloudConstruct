namespace CloudConstruct.SecureFileField.Settings {
    /// <summary>
    /// Standard: no subfolders are created into Secure Directory.
    /// UploadDate: subfolder in the format "YYYYMMDD", representing the upload date of the file, is created into Secure Directory.
    /// Token: 
    /// Custom: custom subfolder is created into Secure Directory.
    /// </summary>
    public enum UrlType {
        Standard = 0,
        UploadDate,
        Custom
    }

    public class SecureFileFieldSettings {
        public string Hint { get; set; }
        public string AllowedExtensions { get; set; }
        public bool Required { get; set; }
        public string SecureDirectoryName { get; set; }
        public string SecureBlobAccountName { get; set; }
        public string SecureSharedKey { get; set; }
        public string SecureBlobEndpoint { get; set; }
        public int SharedAccessExpirationMinutes { get; set; }
        public bool GenerateFileName { get; set; }
        public UrlType UrlType { get; set; }
        public string CustomSubfolder { get; set; }
        public string Custom1 { get; set; }
        public string Custom2 { get; set; }
        public string Custom3 { get; set; }
    }
}
