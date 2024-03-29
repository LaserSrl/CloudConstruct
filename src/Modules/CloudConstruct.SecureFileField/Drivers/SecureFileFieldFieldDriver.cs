﻿using CloudConstruct.SecureFileField.Providers;
using CloudConstruct.SecureFileField.Settings;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Tokens;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloudConstruct.SecureFileField.Drivers {

    public class SecureFileFieldDriver : ContentFieldDriver<Fields.SecureFileField> {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IEncryptionService _encryptionService;
        public Localizer T { get; set; }
        private readonly ITokenizer _tokenizer;

        public SecureFileFieldDriver(IWorkContextAccessor workContextAccessor, IEncryptionService encryptionService, ITokenizer tokenizer) {
            T = NullLocalizer.Instance;
            _workContextAccessor = workContextAccessor;
            _encryptionService = encryptionService;
            _tokenizer = tokenizer;
        }

        private static string GetPrefix(Fields.SecureFileField field, ContentPart part) {
            return part.PartDefinition.Name + "." + field.Name;
        }

        private static string GetDifferentiator(Fields.SecureFileField field, ContentPart part) {
            return field.Name;
        }

        protected override DriverResult Display(ContentPart part, Fields.SecureFileField field, string displayType, dynamic shapeHelper) {
            return ContentShape("Fields_SecureFile", GetDifferentiator(field, part),
                () => {
                    //does the user want to use shared access sigs
                    var settings = field.PartFieldDefinition.Settings.GetModel<SecureFileFieldSettings>();

                    if (settings.SharedAccessExpirationMinutes > 0) {
                        if (!string.IsNullOrEmpty(settings.SecureBlobAccountName)) {
                            SecureAzureBlobStorageProvider provider = new SecureAzureBlobStorageProvider(settings.SecureBlobAccountName, settings.SecureSharedKey,
                                                                                                         settings.SecureBlobEndpoint, true, settings.SecureDirectoryName);
                            field.SharedAccessUrl = provider.GetSharedAccessSignature(field.Url, settings.SharedAccessExpirationMinutes);
                        }
                    }

                    return shapeHelper.Fields_SecureFile();
                });
        }

        protected override DriverResult Editor(ContentPart part, Fields.SecureFileField field, dynamic shapeHelper) {
            return ContentShape("Fields_SecureFile_Edit", GetDifferentiator(field, part),
                () => shapeHelper.EditorTemplate(TemplateName: "Fields/SecureFile.Edit", Model: field, Prefix: GetPrefix(field, part)));
        }

        protected override DriverResult Editor(ContentPart part, Fields.SecureFileField field, IUpdateModel updater, dynamic shapeHelper) {

            WorkContext wc = _workContextAccessor.GetContext();
            var file = wc.HttpContext.Request.Files["FileField-" + field.Name];

            // if the model could not be bound, don't try to validate its properties
            if (updater.TryUpdateModel(field, GetPrefix(field, part), null, null)) {
                var settings = field.PartFieldDefinition.Settings.GetModel<SecureFileFieldSettings>();

                var extensions = String.IsNullOrWhiteSpace(settings.AllowedExtensions)
                        ? new string[0]
                        : settings.AllowedExtensions.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                try {
                    if (file != null && file.ContentLength > 0) {
                        string fname = Path.GetFileName(file.FileName);
                        if (settings.GenerateFileName) {
                            var extension = Path.GetExtension(file.FileName);
                            fname = Guid.NewGuid().ToString("n") + extension;
                        }
                        if (extensions.Any() && fname != null && !extensions.Any(x => fname.EndsWith(x, StringComparison.OrdinalIgnoreCase))) {
                            updater.AddModelError("Url", T("The field {0} must have one of these extensions: {1}", field.DisplayName.CamelFriendly(), settings.AllowedExtensions));
                            return Editor(part, field, shapeHelper);
                        }

                        if (settings.Required && String.IsNullOrWhiteSpace(fname)) {
                            updater.AddModelError("Url", T("The field {0} is mandatory", field.DisplayName.CamelFriendly()));
                            return Editor(part, field, shapeHelper);
                        }

                        DateTime upload = DateTime.UtcNow;
                        field.Upload = upload;
                        field.Url = fname;
                        IStorageProvider provider;

                        if (!string.IsNullOrEmpty(settings.SecureBlobAccountName)) {

                            provider = new SecureAzureBlobStorageProvider(settings.SecureBlobAccountName, settings.SecureSharedKey,
                                                                          settings.SecureBlobEndpoint, true, settings.SecureDirectoryName);

                        } else {
                            string url = settings.SecureDirectoryName;
                            string subfolder = _tokenizer.Replace(settings.CustomSubfolder, new Dictionary<string, object> { { "Content", part.ContentItem } });

                            if (!string.IsNullOrWhiteSpace(subfolder)) {
                                field.Subfolder = subfolder;
                                url = Path.Combine(url, subfolder);
                                if (!Directory.Exists(url))
                                    Directory.CreateDirectory(url);
                            }

                            provider = new SecureFileStorageProvider(url);
                        }

                        int length = (int)file.ContentLength;
                        byte[] buffer = new byte[length];
                        using (Stream stream = file.InputStream) {
                            stream.Read(buffer, 0, length);
                        }

                        if (settings.EncryptFile) {
                            buffer = _encryptionService.Encode(buffer);
                        }

                        provider.Insert(fname, buffer, file.ContentType, length, true);
                    }

                } catch (Exception) {

                    throw;
                }

            }

            return Editor(part, field, shapeHelper);
        }

        protected override void Importing(ContentPart part, Fields.SecureFileField field, ImportContentContext context) {
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Url", value => field.Url = value);
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "AlternateText", value => field.AlternateText = value);
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Class", value => field.Class = value);
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Style", value => field.Style = value);
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Alignment", value => field.Alignment = value);
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Width", value => field.Width = Int32.Parse(value));
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Height", value => field.Height = Int32.Parse(value));
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Upload", value => field.Upload = DateTime.Parse(value));
            context.ImportAttribute(field.FieldDefinition.Name + "." + field.Name, "Subfolder", value => field.Subfolder = value);
        }

        protected override void Exporting(ContentPart part, Fields.SecureFileField field, ExportContentContext context) {
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Url", field.Url);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("AlternateText", field.AlternateText);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Class", field.Class);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Style", field.Style);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Alignment", field.Alignment);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Width", field.Width);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Height", field.Height);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Upload", field.Upload);
            context.Element(field.FieldDefinition.Name + "." + field.Name).SetAttributeValue("Subfolder", field.Subfolder);
        }

        protected override void Describe(DescribeMembersContext context) {
            context
                .Member(null, typeof(string), T("Url"), T("The url of the media."));
        }
    }
}