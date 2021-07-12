using Orchard.ContentManagement;
using Orchard.ContentManagement.FieldStorage;
using Orchard.ContentManagement.Utilities;
using System;

namespace CloudConstruct.SecureFileField.Fields
{
    public class SecureFileField : ContentField
    {
        internal LazyField<string> _secureUrl = new LazyField<string>();

        public string Url {
            get { return Storage.Get<string>(); }
            set { Storage.Set(value); }
        }

        public string AlternateText {
            get { return Storage.Get<string>("AlternateText"); }
            set { Storage.Set("AlternateText", value); }
        }

        public string Class {
            get { return Storage.Get<string>("Class"); }
            set { Storage.Set("Class", value); }
        }

        public string Style {
            get { return Storage.Get<string>("Style"); }
            set { Storage.Set("Style", value); }
        }

        public string Alignment {
            get { return Storage.Get<string>("Alignment"); }
            set { Storage.Set("Alignment", value); }
        }

        public int Width {
            get { return Storage.Get<int>("Width"); }
            set { Storage.Set("Width", value); }
        }

        public int Height {
            get { return Storage.Get<int>("Height"); }
            set { Storage.Set("Height", value); }
        }
        //public string GetSecureUrl(int id) {
        //    return "/CloudConstruct.SecureFileField/SecureFileField/GetSecureFile/" + id + "?fieldName=" + this.Name;
        //}

        public string SecureUrl {
            get {
                return _secureUrl.Value;
            }
        }
        public string SharedAccessUrl { get; set; }

        /// <summary>
        /// Upload date is used in case url of the file is in a subfolder like "Root/YYYYMMDD/file".
        /// This behaviour is defined in <see cref="Settings.SecureFileFieldSettings"/>.
        /// </summary>
        public DateTime Upload {
            get { return Storage.Get<DateTime>(); }
            set { Storage.Set(value); }
        }
    }
}
